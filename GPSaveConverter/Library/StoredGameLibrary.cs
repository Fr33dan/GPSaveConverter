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
        /// <returns>A description of the first problem found, or null if there is none.</returns>
        internal string FindProblem()
        {
            DateTime versionDate;
            if (Version == null || !DateTime.TryParseExact(Version, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out versionDate))
            {
                return "Version must be a date in the form yyyy-MM-dd.";
            }

            if (GameInfo == null || GameInfo.Count == 0) return "The library contains no games.";

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
            }

            return null;
        }
    }
}
