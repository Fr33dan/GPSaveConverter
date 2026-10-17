using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter.Xbox
{
    /// <summary>
    /// An Xbox save in a folder picked by hand: a copy of one, someone else's, or a backup the tool
    /// made. Files are copied from it, never to it (#138).
    /// </summary>
    internal class PickedXboxSave
    {
        internal static IFileSystem FileSystem { get; set; } = new DefaultFileSystem();

        internal const string IndexName = "containers.index";

        /// <summary>
        /// How many folders below the picked one a save is looked for. Three reaches it in a copy of a
        /// game's whole package folder, where it is in SystemAppData\wgs\(profile).
        /// </summary>
        internal const int DepthSearched = 3;

        /// <summary>
        /// How many folders are looked in at most. A folder as big as a whole drive would take minutes.
        /// </summary>
        internal const int MostFoldersSearched = 5000;

        /// <summary>
        /// Stands for the profile when nothing says which one the save belongs to. A translation that
        /// uses the profile's ID matches no file with it, which is safer than matching the wrong ones.
        /// </summary>
        internal const string UnknownProfileID = "0";

        /// <summary>The folder holding the save's containers.index.</summary>
        internal string ProfileFolder { get; private set; }

        /// <summary>The Xbox profile the save belongs to, or <see cref="UnknownProfileID"/>.</summary>
        internal string XboxProfileID { get; private set; }

        internal bool ProfileIDKnown { get { return XboxProfileID != UnknownProfileID; } }

        private PickedXboxSave() { }

        /// <summary>
        /// Looks for the save in the folder, and in the folders up to <see cref="DepthSearched"/> below it.
        /// </summary>
        /// <param name="problem">Why no save is given, worded for the user.</param>
        /// <returns>Null if there is no save there, or more than one.</returns>
        internal static PickedXboxSave Find(string folder, out string problem)
        {
            List<string> found = new List<string>();
            int searched = 0;
            if (!collectSaves(folder, 0, found, ref searched))
            {
                problem = "There are too many folders in " + folder + " to look through. Pick the folder the Xbox save is in, or one nearer to it.";
                return null;
            }

            if (found.Count == 0)
            {
                problem = "There is no Xbox save in " + folder + " or in the folders in it." + Environment.NewLine + Environment.NewLine
                    + "An Xbox save is a folder that holds a file named " + IndexName + ". The Xbox app keeps it in %LOCALAPPDATA%\\Packages\\(the game's package)\\SystemAppData\\wgs, in a folder named after the Xbox profile.";
                return null;
            }

            // The Xbox app can leave a second folder for a profile, holding an index and nothing else (#113).
            List<string> withContainers = found.Where(f => FileSystem.GetDirectories(f).Length > 0).ToList();
            if (withContainers.Count > 0)
            {
                found = withContainers;
            }

            if (found.Count > 1)
            {
                const int namesShown = 5;
                StringBuilder text = new StringBuilder();
                text.AppendLine("There is more than one Xbox save in " + folder + ". Pick the folder of one of them:");
                text.AppendLine();
                foreach (string save in found.Take(namesShown))
                {
                    text.AppendLine("    " + save);
                }
                if (found.Count > namesShown)
                {
                    text.AppendLine("    and " + (found.Count - namesShown) + " more");
                }
                problem = text.ToString().TrimEnd();
                return null;
            }

            problem = null;
            return new PickedXboxSave { ProfileFolder = found[0], XboxProfileID = profileIDOf(found[0]) };
        }

        /// <summary>Reads the save's index. The game it belongs to is the one the index names.</summary>
        internal XboxContainerIndex Open()
        {
            return XboxContainerIndex.OpenPickedFolder(ProfileFolder, XboxProfileID);
        }

        /// <returns>False if it stopped at <see cref="MostFoldersSearched"/>.</returns>
        private static bool collectSaves(string folder, int depth, List<string> found, ref int searched)
        {
            if (++searched > MostFoldersSearched)
            {
                return false;
            }

            if (FileSystem.FileExists(Path.Combine(folder, IndexName)))
            {
                // The folders in a save are its containers. There is no other save below them.
                found.Add(folder);
                return true;
            }
            if (depth == DepthSearched)
            {
                return true;
            }

            string[] subfolders;
            try
            {
                subfolders = FileSystem.GetDirectories(folder);
            }
            catch (Exception e) when (e is UnauthorizedAccessException || e is IOException)
            {
                // A folder that cannot be read holds no save the tool could open.
                return true;
            }

            foreach (string subfolder in subfolders)
            {
                if (!collectSaves(subfolder, depth + 1, found, ref searched))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The profile, from the folder's name as the Xbox app gives it. A backup the tool made keeps
        /// the save in a folder named "files", and its record says which folder the save came from.
        /// </summary>
        private static string profileIDOf(string profileFolder)
        {
            string id = profileIDInName(profileFolder);
            string folderName = Path.GetFileName(profileFolder.TrimEnd('\\', '/'));
            if (id == null && string.Equals(folderName, SaveBackups.SaveBackup.FilesFolderName, StringComparison.OrdinalIgnoreCase))
            {
                SaveBackups.SaveBackup backup = SaveBackups.SaveBackup.Load(Path.GetDirectoryName(profileFolder.TrimEnd('\\', '/')));
                if (backup != null && backup.Side == SaveBackups.BackupSide.Xbox)
                {
                    id = profileIDInName(backup.OriginalFolder);
                }
            }
            return id ?? UnknownProfileID;
        }

        /// <summary>
        /// The profile's ID from a folder named as the Xbox app names a profile's: the ID in hex, an
        /// underscore and a second ID.
        /// </summary>
        /// <returns>Null if the name is not of that form.</returns>
        internal static string profileIDInName(string folder)
        {
            Match name = Regex.Match(Path.GetFileName(folder.TrimEnd('\\', '/')), "^([0-9A-Fa-f]{1,16})_[0-9A-Fa-f]+$");
            return name.Success ? name.Groups[1].Value : null;
        }
    }
}
