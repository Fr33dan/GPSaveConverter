using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GPSaveConverter.SaveBackups
{
    /// <summary>
    /// The folder the backups are kept in: one folder per game, and in it one folder per backup.
    /// </summary>
    internal class SaveBackupStore
    {
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();

        private readonly HashSet<string> foldersHandedOut = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal string Root { get; private set; }

        /// <summary>
        /// Gives the free space on the drive that holds a folder, or null if that cannot be told.
        /// </summary>
        internal Func<string, long?> FreeSpace { get; set; } = DriveFreeSpace;

        /// <summary>
        /// Gives the time a new backup is dated with.
        /// </summary>
        internal Func<DateTime> Now { get; set; } = () => DateTime.Now;

        internal SaveBackupStore(string root)
        {
            this.Root = root;
        }

        /// <summary>
        /// Next to the folder the settings are in.
        /// </summary>
        internal static string DefaultRoot
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GPSaveConverter", "Backups"); }
        }

        /// <summary>
        /// Copies a whole Xbox profile folder. The backup is complete when this returns, so call it
        /// before the first file is written.
        /// </summary>
        /// <param name="madeBefore">What is about to happen, worded to follow "before".</param>
        internal SaveBackup BackUpXboxSave(string packageName, string gameName, string profileFolder, string madeBefore)
        {
            // The same check a restore makes. A backup that could not be restored is not worth taking.
            if (!SaveBackup.IsXboxProfileFolder(profileFolder, packageName))
            {
                throw new ArgumentException("Not an Xbox save folder of the game: " + profileFolder);
            }
            if (!Directory.Exists(profileFolder))
            {
                throw new DirectoryNotFoundException("The Xbox save folder does not exist: " + profileFolder);
            }

            long needed = SaveBackup.FilesBelow(profileFolder).Sum(f => new FileInfo(f).Length);
            long? free = FreeSpace(Root);
            if (free != null && needed > free)
            {
                throw new IOException(string.Format("There is not enough free disk space for a backup. It needs {0} and {1} is free.", SaveBackup.FormatSize(needed), SaveBackup.FormatSize(free.Value)));
            }

            SaveBackup backup = NewBackup(packageName, gameName, BackupSide.Xbox, profileFolder, madeBefore);
            try
            {
                backup.CopyFolder();
            }
            catch
            {
                // Half a backup must not be left where it could be mistaken for a whole one.
                try
                {
                    DeleteFolder(backup.Folder);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    logger.Debug(e, "Unfinished backup could not be removed: {0}", backup.Folder);
                }
                throw;
            }

            logger.Debug("Backed up Xbox save {0} to {1} ({2} files, {3})", profileFolder, backup.Folder, backup.Files.Count, SaveBackup.FormatSize(backup.Size));
            return backup;
        }

        /// <summary>
        /// Starts a backup of files in the non-Xbox save folder. It holds nothing, and is not on disk,
        /// until <see cref="SaveBackup.Preserve"/> is called for the first file. Call
        /// <see cref="SaveBackup.Complete"/> when the transfer is over.
        /// </summary>
        /// <param name="madeBefore">What is about to happen, worded to follow "before".</param>
        internal SaveBackup StartNonXboxBackup(string packageName, string gameName, string saveFolder, string madeBefore)
        {
            return NewBackup(packageName, gameName, BackupSide.NonXbox, saveFolder, madeBefore);
        }

        /// <summary>
        /// The backups of a game, newest first.
        /// </summary>
        internal List<SaveBackup> List(string packageName)
        {
            List<SaveBackup> backups = new List<SaveBackup>();
            string gameFolder = GameFolder(packageName);
            if (Directory.Exists(gameFolder))
            {
                foreach (string folder in Directory.GetDirectories(gameFolder))
                {
                    SaveBackup backup = SaveBackup.Load(folder);
                    if (backup != null)
                    {
                        backups.Add(backup);
                    }
                }
            }
            return backups.OrderByDescending(b => b.Created).ToList();
        }

        /// <summary>
        /// Puts the files of a backup back where they came from. What is there now is backed up first,
        /// so that a restore can be undone like a transfer.
        /// </summary>
        /// <returns>The backup of what the restore replaced.</returns>
        internal SaveBackup Restore(SaveBackup backup)
        {
            // Before anything else: a damaged backup must not get as far as replacing files.
            backup.Verify();

            string madeBefore = "restoring the backup from " + backup.Created.ToString("g");
            SaveBackup replaced;
            if (backup.Side == BackupSide.Xbox)
            {
                replaced = BackUpXboxSave(backup.PackageName, backup.GameName, backup.OriginalFolder, madeBefore);
            }
            else
            {
                replaced = StartNonXboxBackup(backup.PackageName, backup.GameName, backup.OriginalFolder, madeBefore);
                foreach (BackupEntry file in backup.Files)
                {
                    replaced.Preserve(Path.Combine(backup.OriginalFolder, file.RelativePath));
                }
                replaced.Complete();
            }

            backup.RestoreFiles();
            logger.Debug("Restored backup {0} to {1}", backup.Folder, backup.OriginalFolder);
            return replaced;
        }

        internal void Delete(SaveBackup backup)
        {
            // Only ever a folder this store holds, and only one that is a backup.
            if (SaveBackup.RelativeTo(Root, backup.Folder) == null || !File.Exists(Path.Combine(backup.Folder, SaveBackup.ManifestName)))
            {
                throw new InvalidOperationException("Not a backup folder: " + backup.Folder);
            }
            DeleteFolder(backup.Folder);
        }

        /// <summary>
        /// Deletes the oldest backups of a game until no more than <paramref name="keep"/> are left.
        /// </summary>
        /// <returns>How many were deleted.</returns>
        internal int Prune(string packageName, int keep)
        {
            int deleted = 0;
            foreach (SaveBackup old in List(packageName).Skip(Math.Max(keep, 1)))
            {
                Delete(old);
                deleted++;
            }
            return deleted;
        }

        private SaveBackup NewBackup(string packageName, string gameName, BackupSide side, string originalFolder, string madeBefore)
        {
            DateTime now = Now();
            string name = now.ToString("yyyyMMdd-HHmmss");
            string folder = Path.Combine(GameFolder(packageName), name);

            // A backup of non-Xbox files is not on disk until its first file, so names given out are remembered.
            for (int number = 2; Directory.Exists(folder) || !foldersHandedOut.Add(folder); number++)
            {
                folder = Path.Combine(GameFolder(packageName), name + "-" + number);
            }

            return new SaveBackup
            {
                Created = now,
                PackageName = packageName,
                GameName = gameName,
                Side = side,
                OriginalFolder = originalFolder,
                MadeBefore = madeBefore,
                Folder = folder
            };
        }

        private string GameFolder(string packageName)
        {
            return Path.Combine(Root, string.Join("_", packageName.Split(Path.GetInvalidFileNameChars())));
        }

        private static void DeleteFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }
            // A copy of a read-only file is read-only too, and would stop the delete.
            foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(folder, true);
        }

        private static long? DriveFreeSpace(string folder)
        {
            try
            {
                return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(folder))).AvailableFreeSpace;
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException)
            {
                // A network share, for one, has no drive to ask.
                return null;
            }
        }
    }
}
