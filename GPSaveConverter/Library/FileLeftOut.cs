using System;
using System.Text.RegularExpressions;

namespace GPSaveConverter.Library
{
    /// <summary>
    /// A file the game library says not to copy, with the reason. A transfer leaves it out in either
    /// direction, even if a translation covers it: copying it is known to do harm, or it belongs in a
    /// folder other than the save folder.
    /// </summary>
    /// <remarks>
    /// Each name is a pattern for the whole name. A file in the Xbox save matches if at least one of
    /// the three Xbox names is given and every one that is given matches. A file on the other side
    /// matches if <see cref="NonXboxFilename"/> is given and matches its path, in any capitals.
    /// </remarks>
    internal class FileLeftOut
    {
        public string ContainerName1 { get; set; }

        public string ContainerName2 { get; set; }

        public string XboxFileID { get; set; }

        public string NonXboxFilename { get; set; }

        /// <summary>Why the file is left out, in words for the user, with the issue it comes from.</summary>
        public string Reason { get; set; }

        internal bool MatchesXbox(string containerName1, string containerName2, string fileID)
        {
            if (ContainerName1 == null && ContainerName2 == null && XboxFileID == null)
            {
                return false;
            }
            return matches(ContainerName1, containerName1, RegexOptions.None)
                && matches(ContainerName2, containerName2, RegexOptions.None)
                && matches(XboxFileID, fileID, RegexOptions.None);
        }

        internal bool MatchesNonXbox(string relativePath)
        {
            return NonXboxFilename != null && matches(NonXboxFilename, relativePath, RegexOptions.IgnoreCase);
        }

        private static bool matches(string pattern, string name, RegexOptions options)
        {
            return pattern == null || Regex.IsMatch(name ?? String.Empty, FileTranslation.ExactRegex(pattern), options);
        }

        /// <summary>
        /// Checks the entry for mistakes that would stop it being used.
        /// </summary>
        /// <returns>A description of the first problem found, or null if there is none.</returns>
        internal string FindProblem()
        {
            if (String.IsNullOrWhiteSpace(Reason)) return "A file left out has no Reason.";
            if (ContainerName1 == null && ContainerName2 == null && XboxFileID == null && NonXboxFilename == null)
            {
                return "A file left out names no file.";
            }

            foreach (string pattern in new[] { ContainerName1, ContainerName2, XboxFileID, NonXboxFilename })
            {
                if (pattern == null) continue;
                try
                {
                    new Regex(pattern);
                }
                catch (ArgumentException e)
                {
                    return "\"" + pattern + "\" in a file left out is not a valid pattern: " + e.Message;
                }
            }
            return null;
        }
    }
}
