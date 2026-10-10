using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Xbox
{
    internal static class XboxHelper
    {
        internal const int GuidLength = 16;
        internal const int FileIDByteLength = 128;
        internal const int EntryByteLength = FileIDByteLength + GuidLength + GuidLength;

        /// <summary>
        /// Tells from the folders in a package's "wgs" folder whether there is a save to work with.
        /// </summary>
        internal static bool HoldsSaves(string[] wgsSubfolders)
        {
            // Two folders or more has always counted. One folder counts as well if it is a profile's:
            // after a game is uninstalled that can be all that is left, and the save in it is as
            // good as it was.
            return wgsSubfolders.Length >= 2 || wgsSubfolders.Any(IsProfileFolder);
        }

        /// <summary>
        /// A profile's folder is named with the profile's ID, an underscore and a second ID.
        /// </summary>
        internal static bool IsProfileFolder(string folder)
        {
            return System.IO.Path.GetFileName(folder.TrimEnd('\\', '/')).Contains("_");
        }
    }
}
