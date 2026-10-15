using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Library
{
    internal class StoredGameLibrary
    {
        public IList<GameInfo> GameInfo { get; set; }

        public string Version { get; set; }

        /// <summary>
        /// Checks the library for mistakes that would stop the application loading or using it.
        /// </summary>
        /// <param name="mayBeEmpty">
        /// True for the preview library, which lists no games when no translation is being tested.
        /// </param>
        /// <returns>A description of the first problem found, or null if there is none.</returns>
        internal string FindProblem(bool mayBeEmpty = false)
        {
            // The date of the change. Another change on the same day adds a capital letter, as in
            // 2023-01-28A. Versions are compared as text, and that keeps both kinds in order.
            DateTime versionDate;
            if (Version == null
                || !System.Text.RegularExpressions.Regex.IsMatch(Version, @"^[0-9]{4}-[0-9]{2}-[0-9]{2}[A-Z]?\z")
                || !DateTime.TryParseExact(Version.Substring(0, 10), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out versionDate))
            {
                return "Version must be a date in the form yyyy-MM-dd, with a capital letter after it for another change on the same day.";
            }

            if (GameInfo == null || (GameInfo.Count == 0 && !mayBeEmpty)) return "The library contains no games.";

            HashSet<string> packageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (GameInfo game in GameInfo)
            {
                if (game == null || string.IsNullOrEmpty(game.PackageName)) return "A game is missing its PackageName.";
                if (!packageNames.Add(game.PackageName)) return "PackageName '" + game.PackageName + "' is listed more than once.";
                if (game.FileTranslations == null) return game.PackageName + ": FileTranslations is missing.";

                foreach (FileTranslation translation in game.FileTranslations)
                {
                    string problem = translation == null ? "A file translation is empty." : translation.FindProblem();
                    if (problem != null) return game.PackageName + ": " + problem;
                }

                if (game.FilesLeftOut != null)
                {
                    foreach (FileLeftOut leftOut in game.FilesLeftOut)
                    {
                        string problem = leftOut == null ? "A file left out is empty." : leftOut.FindProblem();
                        if (problem != null) return game.PackageName + ": " + problem;
                    }
                }
            }

            return null;
        }
    }
}
