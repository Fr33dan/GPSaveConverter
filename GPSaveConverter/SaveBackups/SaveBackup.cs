using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GPSaveConverter.SaveBackups
{
    /// <summary>
    /// Which side of a transfer a backup holds.
    /// </summary>
    internal enum BackupSide
    {
        /// <summary>A whole Xbox profile folder: containers.index and every container below it.</summary>
        Xbox,

        /// <summary>Files from the non-Xbox save folder, one for each path a transfer was about to write.</summary>
        NonXbox
    }

    /// <summary>
    /// One file a backup accounts for.
    /// </summary>
    internal class BackupEntry
    {
        /// <summary>Where the file is, relative to the folder the backup was taken from.</summary>
        public string RelativePath { get; set; }

        /// <summary>False if there was no such file when the backup was taken. Restoring then removes it.</summary>
        public bool Existed { get; set; }

        public long Length { get; set; }

        /// <summary>
        /// The outermost folder above the file that was not there either, relative like the file.
        /// Null if the file's folder existed.
        /// </summary>
        public string NewFolder { get; set; }
    }

    /// <summary>
    /// A copy of save files as they were before the application changed them. On disk it is a folder
    /// holding backup.json, which is this object, and a "files" folder with the copies.
    /// </summary>
    internal class SaveBackup
    {
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();

        internal const string ManifestName = "backup.json";
        internal const string JournalName = "journal.jsonl";
        internal const string FilesFolderName = "files";
        internal const string XboxIndexName = "containers.index";
        internal const int CurrentFormatVersion = 1;

        private static readonly JsonSerializerOptions ManifestOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions JournalOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public int FormatVersion { get; set; } = CurrentFormatVersion;

        public DateTime Created { get; set; }

        public string PackageName { get; set; }

        public string GameName { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BackupSide Side { get; set; }

        /// <summary>
        /// What the application was about to do, worded to follow "before": "copying 3 files to Xbox".
        /// </summary>
        public string MadeBefore { get; set; }

        /// <summary>
        /// The folder the files came from, and where restoring puts them back.
        /// </summary>
        public string OriginalFolder { get; set; }

        public List<BackupEntry> Files { get; set; } = new List<BackupEntry>();

        /// <summary>
        /// Xbox backups only: every folder below the profile folder, relative to it.
        /// </summary>
        public List<string> Folders { get; set; } = new List<string>();

        /// <summary>
        /// The folder this backup is stored in.
        /// </summary>
        internal string Folder { get; set; }

        internal string FilesFolder { get { return Path.Combine(Folder, FilesFolderName); } }

        /// <summary>
        /// The bytes this backup holds.
        /// </summary>
        internal long Size { get { return Files.Sum(f => f.Length); } }

        /// <summary>
        /// Reads the backup stored in a folder.
        /// </summary>
        /// <returns>The backup, or null if the folder does not hold one this version can use.</returns>
        internal static SaveBackup Load(string folder)
        {
            try
            {
                string manifest = Path.Combine(folder, ManifestName);
                if (!File.Exists(manifest))
                {
                    return null;
                }

                SaveBackup backup = JsonSerializer.Deserialize<SaveBackup>(File.ReadAllText(manifest));
                if (backup == null || backup.FormatVersion != CurrentFormatVersion || backup.OriginalFolder == null || backup.Files == null)
                {
                    return null;
                }
                if (backup.Folders == null)
                {
                    backup.Folders = new List<string>();
                }
                backup.Folder = folder;

                // A journal is left behind when the application is closed in the middle of a transfer.
                // The files it lists were kept before anything was written, so they still count.
                string journal = Path.Combine(folder, JournalName);
                if (File.Exists(journal))
                {
                    foreach (string line in File.ReadAllLines(journal))
                    {
                        BackupEntry entry = ReadJournalLine(line);
                        if (entry != null && !backup.Holds(entry.RelativePath))
                        {
                            backup.Add(entry);
                        }
                    }
                }

                // Started, but stopped before its first file was recorded. There is nothing in it to restore.
                if (backup.Side == BackupSide.NonXbox && backup.Files.Count == 0)
                {
                    return null;
                }
                return backup;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
            {
                logger.Debug(e, "Backup folder skipped: {0}", folder);
                return null;
            }
        }

        private static BackupEntry ReadJournalLine(string line)
        {
            if (line.Trim().Length == 0)
            {
                return null;
            }
            try
            {
                BackupEntry entry = JsonSerializer.Deserialize<BackupEntry>(line);
                return entry == null || entry.RelativePath == null ? null : entry;
            }
            catch (JsonException)
            {
                // The last line, cut short. Its file was not written to: the line comes first.
                return null;
            }
        }

        /// <summary>
        /// Writes backup.json.
        /// </summary>
        internal void Save()
        {
            Directory.CreateDirectory(Folder);
            string manifest = Path.Combine(Folder, ManifestName);
            string unfinished = manifest + ".tmp";
            File.WriteAllText(unfinished, JsonSerializer.Serialize(this, ManifestOptions));

            // Swapped in whole, so there is never half a manifest on disk.
            if (File.Exists(manifest))
            {
                File.Replace(unfinished, manifest, null);
            }
            else
            {
                File.Move(unfinished, manifest);
            }
        }

        /// <summary>
        /// Keeps a file of the non-Xbox save folder as it is now. Call it before writing to that path.
        /// If there is no file yet, that is what gets recorded, and restoring removes the file again.
        /// Only the first call for a path does anything: after that the path no longer holds what was
        /// there to begin with.
        /// </summary>
        internal void Preserve(string path)
        {
            string relativePath = RelativeTo(OriginalFolder, path);
            if (relativePath == null)
            {
                throw new InvalidOperationException("The file is outside the save folder, so it cannot be backed up: " + path
                    + Environment.NewLine + "To copy it anyway, turn off backups in the preferences.");
            }
            if (Holds(relativePath))
            {
                return;
            }

            BackupEntry entry = new BackupEntry { RelativePath = relativePath };
            try
            {
                if (File.Exists(path))
                {
                    string copy = Path.Combine(FilesFolder, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(copy));
                    CopyOver(path, copy);
                    entry.Existed = true;
                    entry.Length = new FileInfo(copy).Length;
                }
                else
                {
                    string root = FullPath(OriginalFolder);
                    string newFolder = null;
                    for (string folder = Path.GetDirectoryName(Path.GetFullPath(path)); folder != null && folder.Length > root.Length && !Directory.Exists(folder); folder = Path.GetDirectoryName(folder))
                    {
                        newFolder = folder;
                    }
                    entry.NewFolder = newFolder == null ? null : RelativeTo(root, newFolder);
                }

                if (Files.Count == 0)
                {
                    Save();
                }
                // One line per file, written before the caller touches the file. Rewriting backup.json each
                // time would get slow for a transfer of many files.
                File.AppendAllText(Path.Combine(Folder, JournalName), JsonSerializer.Serialize(entry, JournalOptions) + Environment.NewLine);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Said in full, because the person at the screen is shown this as the reason a file was not copied.
                throw new IOException("The file could not be backed up, so it was left as it is. " + e.Message, e);
            }
            Add(entry);
        }

        /// <summary>
        /// Call when the transfer that used <see cref="Preserve"/> is over. Moves what the journal
        /// recorded into backup.json.
        /// </summary>
        internal void Complete()
        {
            if (Files.Count == 0)
            {
                return;
            }
            Save();
            File.Delete(Path.Combine(Folder, JournalName));
        }

        /// <summary>
        /// Copies everything in the Xbox profile folder into this backup.
        /// </summary>
        internal void CopyFolder()
        {
            string root = FullPath(OriginalFolder);
            foreach (string folder in FoldersBelow(root))
            {
                Folders.Add(RelativeTo(root, folder));
            }
            foreach (string file in FilesBelow(root))
            {
                string relativePath = RelativeTo(root, file);
                string copy = Path.Combine(FilesFolder, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(copy));
                File.Copy(file, copy);
                Files.Add(new BackupEntry { RelativePath = relativePath, Existed = true, Length = new FileInfo(copy).Length });
            }
            Save();
        }

        /// <summary>
        /// Checks that every file this backup should hold is here and that it names nothing outside the
        /// folder it was taken from. Throws if not.
        /// </summary>
        internal void Verify()
        {
            // Restoring an Xbox backup deletes whatever else is in the folder. That must never be
            // pointed at anything but a profile's save folder, whatever backup.json says.
            if (Side == BackupSide.Xbox && !IsXboxProfileFolder(OriginalFolder, PackageName))
            {
                throw new InvalidDataException("The backup does not name an Xbox save folder of the game: " + OriginalFolder);
            }

            foreach (BackupEntry file in Files)
            {
                if (!IsInside(file.RelativePath) || (file.NewFolder != null && !IsInside(file.NewFolder)))
                {
                    throw new InvalidDataException("The backup names a file outside the folder it was taken from: " + file.RelativePath);
                }
                if (file.Existed)
                {
                    FileInfo copy = new FileInfo(Path.Combine(FilesFolder, file.RelativePath));
                    if (!copy.Exists || copy.Length != file.Length)
                    {
                        throw new InvalidDataException("The backup is incomplete. This file is missing from it or has changed: " + file.RelativePath);
                    }
                }
            }
            foreach (string folder in Folders)
            {
                if (!IsInside(folder))
                {
                    throw new InvalidDataException("The backup names a folder outside the folder it was taken from: " + folder);
                }
            }
        }

        private bool IsInside(string relativePath)
        {
            return !string.IsNullOrEmpty(relativePath) && RelativeTo(OriginalFolder, Path.Combine(FullPath(OriginalFolder), relativePath)) != null;
        }

        /// <summary>
        /// Puts the files back where they came from, and removes the ones that were not there.
        /// </summary>
        internal void RestoreFiles()
        {
            Verify();
            string root = FullPath(OriginalFolder);

            foreach (string folder in Folders)
            {
                Directory.CreateDirectory(Path.Combine(root, folder));
            }

            // containers.index names every container, so it goes back after them. If the restore is cut
            // short, the index still on disk is one whose containers are all still there.
            foreach (BackupEntry file in Files.Where(f => f.Existed).OrderBy(f => IsXboxIndex(f) ? 1 : 0))
            {
                string target = Path.Combine(root, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                CopyOver(Path.Combine(FilesFolder, file.RelativePath), target);
            }

            foreach (BackupEntry file in Files.Where(f => !f.Existed))
            {
                string target = Path.GetFullPath(Path.Combine(root, file.RelativePath));
                RemoveFile(target);

                if (file.NewFolder != null)
                {
                    // The folders that were made for the file go too, innermost first, unless something
                    // else has been put in them since.
                    string outermost = Path.GetFullPath(Path.Combine(root, file.NewFolder));
                    for (string folder = Path.GetDirectoryName(target); folder != null && folder.Length >= outermost.Length; folder = Path.GetDirectoryName(folder))
                    {
                        if (!RemoveFolderIfEmpty(folder))
                        {
                            break;
                        }
                    }
                }
            }

            if (Side == BackupSide.Xbox)
            {
                // The backup is the whole profile, so anything it does not hold was added afterwards.
                HashSet<string> files = new HashSet<string>(Files.Select(f => f.RelativePath), StringComparer.OrdinalIgnoreCase);
                foreach (string file in FilesBelow(root))
                {
                    if (!files.Contains(RelativeTo(root, file)))
                    {
                        RemoveFile(file);
                    }
                }

                HashSet<string> folders = new HashSet<string>(Folders, StringComparer.OrdinalIgnoreCase);
                foreach (string folder in FoldersBelow(root).OrderByDescending(f => f.Length))
                {
                    if (!folders.Contains(RelativeTo(root, folder)))
                    {
                        RemoveFolderIfEmpty(folder);
                    }
                }
            }
        }

        private bool IsXboxIndex(BackupEntry file)
        {
            return Side == BackupSide.Xbox && string.Equals(file.RelativePath, XboxIndexName, StringComparison.OrdinalIgnoreCase);
        }

        private HashSet<string> pathsHeld;

        /// <summary>
        /// Tells whether this backup already accounts for a path.
        /// </summary>
        private bool Holds(string relativePath)
        {
            // Looked up once per file of a transfer, so not by going through the list each time.
            if (pathsHeld == null || pathsHeld.Count != Files.Count)
            {
                pathsHeld = new HashSet<string>(Files.Select(f => f.RelativePath), StringComparer.OrdinalIgnoreCase);
            }
            return pathsHeld.Contains(relativePath);
        }

        private void Add(BackupEntry entry)
        {
            Files.Add(entry);
            if (pathsHeld != null)
            {
                pathsHeld.Add(entry.RelativePath);
            }
        }

        private static void CopyOver(string source, string destination)
        {
            if (File.Exists(destination))
            {
                // A read-only or hidden file cannot be overwritten as it is.
                File.SetAttributes(destination, FileAttributes.Normal);
            }
            File.Copy(source, destination, true);
        }

        private static void RemoveFile(string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        /// <returns>True if the folder is gone afterwards.</returns>
        private static bool RemoveFolderIfEmpty(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return true;
            }
            if (Directory.EnumerateFileSystemEntries(folder).Any())
            {
                return false;
            }
            Directory.Delete(folder);
            return true;
        }

        /// <summary>
        /// Lists the folders below a folder, each one before the folders inside it. A link to another
        /// place is left out along with whatever it leads to: nothing reached through a link is copied,
        /// and nothing reached through a link is ever deleted.
        /// </summary>
        internal static List<string> FoldersBelow(string folder)
        {
            List<string> found = new List<string>();
            foreach (string child in Directory.GetDirectories(folder))
            {
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }
                found.Add(child);
                found.AddRange(FoldersBelow(child));
            }
            return found;
        }

        /// <summary>
        /// Lists the files in a folder and in the folders <see cref="FoldersBelow"/> finds.
        /// </summary>
        internal static List<string> FilesBelow(string folder)
        {
            List<string> found = new List<string>(Directory.GetFiles(folder));
            foreach (string child in FoldersBelow(folder))
            {
                found.AddRange(Directory.GetFiles(child));
            }
            return found;
        }

        /// <summary>
        /// The path of a file or folder relative to a folder that contains it.
        /// </summary>
        /// <returns>The relative path, or null if <paramref name="path"/> is not inside <paramref name="folder"/>.</returns>
        internal static string RelativeTo(string folder, string path)
        {
            string root = FullPath(folder);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                root += Path.DirectorySeparatorChar;
            }
            string full = Path.GetFullPath(path);

            return full.Length > root.Length && full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : null;
        }

        /// <summary>
        /// Tells whether a folder is where the Xbox app keeps one profile's saves of a game:
        /// ...\Packages\{package}\SystemAppData\wgs\{profile}
        /// </summary>
        internal static bool IsXboxProfileFolder(string folder, string packageName)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(packageName))
            {
                return false;
            }

            string[] parts = FullPath(folder).Split(Path.DirectorySeparatorChar);
            int profile = parts.Length - 1;
            return profile >= 4
                && string.Equals(parts[profile - 1], "wgs", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parts[profile - 2], "SystemAppData", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parts[profile - 3], packageName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(parts[profile - 4], "Packages", StringComparison.OrdinalIgnoreCase);
        }

        private static string FullPath(string folder)
        {
            string full = Path.GetFullPath(folder);

            // "C:\" keeps its separator. Without it the path would mean the current folder on that drive.
            return full.Length > 3 ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
        }

        internal static string FormatSize(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + (bytes == 1 ? " byte" : " bytes");
            }

            string[] units = { "KB", "MB", "GB", "TB" };
            double size = bytes / 1024.0;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return size.ToString("0.#") + " " + units[unit];
        }
    }
}
