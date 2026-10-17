using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GPSaveConverter
{
    /// <summary>
    /// Builds the GitHub page that asks for a file translation, filled in with what the tool knows.
    /// The form it opens is .github/ISSUE_TEMPLATE/translation-request.yml in the repository.
    /// </summary>
    internal static class TranslationRequest
    {
        internal const string NewIssuePage = "https://github.com/Fr33dan/GPSaveConverter/issues/new";
        internal const string Template = "translation-request.yml";

        /// <summary>
        /// The longest address that is sure to open. Browsers and GitHub turn longer ones away, so a
        /// long list of files goes on the clipboard instead of into the address.
        /// </summary>
        internal const int LongestAddress = 7000;

        /// <summary>
        /// The address of a new issue on the translation request form, with its fields filled in.
        /// </summary>
        /// <param name="tablesIncluded">False if the tables were too long for the address and were left out of it.</param>
        internal static string IssueAddress(string gameName, string version, string tables, out bool tablesIncluded)
        {
            string start = NewIssuePage + "?template=" + Template
                + "&title=" + Uri.EscapeDataString("Translation request: " + gameName)
                + "&game=" + Uri.EscapeDataString(gameName)
                + "&version=" + Uri.EscapeDataString(version);
            string withTables = start + "&tables=" + Uri.EscapeDataString(tables);

            tablesIncluded = withTables.Length <= LongestAddress;
            return tablesIncluded ? withTables : start;
        }

        /// <summary>
        /// Writes the user's own folders as the variables that stand for them, as in
        /// %USERPROFILE%\Documents, so that file tables posted in public do not carry the user's name.
        /// </summary>
        /// <param name="folders">Each variable with the folder it stands for. The longest folder is put back first.</param>
        internal static string HideUserFolders(string text, IEnumerable<KeyValuePair<string, string>> folders)
        {
            foreach (KeyValuePair<string, string> folder in folders.Where(f => !String.IsNullOrEmpty(f.Value)).OrderByDescending(f => f.Value.Length))
            {
                string variable = folder.Key;
                // Only where the folder's name ends: C:\Users\Jo is not the start of C:\Users\Joe.
                string pattern = Regex.Escape(folder.Value.TrimEnd('\\')) + @"(?=[\\\s|""']|$)";
                text = Regex.Replace(text, pattern, match => variable, RegexOptions.IgnoreCase);
            }
            return text;
        }

        /// <summary>The user's folders that a save location can be under, with the variables for them.</summary>
        internal static IEnumerable<KeyValuePair<string, string>> UserFolders()
        {
            return new[]
            {
                new KeyValuePair<string, string>("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                new KeyValuePair<string, string>("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)),
                new KeyValuePair<string, string>("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            };
        }
    }
}
