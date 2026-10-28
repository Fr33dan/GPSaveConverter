using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

        /// <summary>A row of a file table: the file's names, then its size.</summary>
        internal sealed class TableRow
        {
            /// <param name="size">The size in bytes, or null if the file could not be read.</param>
            internal TableRow(long? size, params string[] names)
            {
                this.Size = size;
                this.Names = names;
            }

            internal long? Size { get; }
            internal string[] Names { get; }
        }

        /// <summary>
        /// A game's save files on both sides, as tables for a GitHub issue, each file with its size
        /// in bytes.
        /// </summary>
        /// <remarks>
        /// Sizes say most within one side: a file kept in parts on one store shows as parts of one
        /// size and a shorter last one. Across the two sides they say less, as a save that has been
        /// played on is often larger than one made only so that files exist to list.
        /// </remarks>
        /// <param name="xboxFiles">Container Name 1, Container Name 2 and Blob ID of each file, or null for no Xbox table.</param>
        /// <param name="nonXboxFiles">Each file's path within the save folder, or null for no non-Xbox table.</param>
        internal static string SaveFileTables(string gameName, string packageName, IEnumerable<TableRow> xboxFiles, string nonXboxLocation, IEnumerable<TableRow> nonXboxFiles)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Game Name:");
            sb.AppendLine(gameName);

            sb.Append("Game Package ID:");
            sb.AppendLine(packageName);
            sb.AppendLine();

            if (xboxFiles != null)
            {
                sb.AppendLine("## Xbox Files:");
                sb.AppendLine("| Container Name 1 | Container Name 2 | Blob ID | Size (bytes) |");
                sb.AppendLine("| ---------------- | ---------------- | ------- | ------------ |");
                appendRows(sb, xboxFiles);
            }

            if (nonXboxFiles != null)
            {
                sb.AppendLine("## Non-Xbox Files:");
                sb.Append("Non-Xbox save location: ");
                sb.AppendLine(nonXboxLocation);
                sb.AppendLine("| File Path | Size (bytes) |");
                sb.AppendLine("| --------- | ------------ |");
                appendRows(sb, nonXboxFiles);
            }

            return sb.ToString();
        }

        private static void appendRows(StringBuilder sb, IEnumerable<TableRow> rows)
        {
            foreach (TableRow row in rows)
            {
                sb.Append("| ");
                foreach (string name in row.Names)
                {
                    sb.Append(name);
                    sb.Append(" | ");
                }
                sb.Append(row.Size.HasValue ? row.Size.Value.ToString(CultureInfo.InvariantCulture) : String.Empty);
                sb.AppendLine(" |");
            }
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
