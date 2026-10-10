using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter.Library
{
    internal class PCGameWiki
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();
        public static readonly string[,] FolderNameSubstitutions = new string[,] { { "{{p|userprofile}}",   "%USERPROFILE%"     }
                                                                                  , { "{{p|appdata}}",       "%APPDATA%"         }
                                                                                  , { "{{p|localappdata}}",  "%LOCALAPPDATA%"    }
                                                                                  , { "{{p|programdata}}",   "%PROGRAMDATA%"     }
                                                                                  , { "{{p|uid}}",           "<user-id>"         }
                                                                                  , { "{{p|steam}}",         "<Steam-folder>"    } };

        private const string SaveRowStart = "{{Game data/saves|";

        private readonly IHttpClient httpClient;

        internal PCGameWiki(IHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task FetchSaveLocation(GameInfo i)
        {
            logger.Info("Fetching save data from pcgamingwiki.com");
            try
            {
                int? foundPage = await findPage(i.Name);
                if (foundPage == null)
                {
                    return;
                }
                int pageID = foundPage.Value;

                string url = String.Format(@"https://www.pcgamingwiki.com/w/api.php?action=parse&pageid={0}&formatversion=2&format=json&prop=sections", pageID);
                string sectionsJson = await httpClient.DownloadStringAsync(url);
                JsonNode sectionsRoot = JsonValue.Parse(sectionsJson);

                if (sectionsRoot == null)
                {
                    return;
                }
                string sectionIndex = "-1";
                foreach (JsonNode n in sectionsRoot["parse"]["sections"].AsArray())
                {
                    if (n["line"].GetValue<string>() == "Save game data location")
                    {
                        sectionIndex = n["index"].GetValue<string>();
                    }
                }
                if (sectionIndex == null || sectionIndex == "-1")
                {
                    return;
                }

                url = String.Format(@"https://www.pcgamingwiki.com/w/api.php?action=parse&pageid={0}&formatversion=2&format=json&prop=wikitext&section={1}", pageID, sectionIndex);
                string saveFileSectionJson = await httpClient.DownloadStringAsync(url);
                JsonNode saveFileSectionRoot = JsonValue.Parse(saveFileSectionJson);
                string wikiTable = saveFileSectionRoot["parse"]["wikitext"].GetValue<string>();

                // Inside the try with the rest. Anyone can edit a wiki page, and whatever is on it must
                // not stop a game from being selected.
                bool usesProfile;
                string foundLocation = chooseSaveLocation(wikiTable, out usesProfile);
                if (foundLocation != null)
                {
                    i.BaseNonXboxSaveLocation = foundLocation;
                    if (usesProfile)
                    {
                        i.TargetProfileTypes = new NonXboxProfile.ProfileType[] { NonXboxProfile.ProfileType.Steam };
                        i.TargetProfiles = new NonXboxProfile[] { new NonXboxProfile(0, NonXboxProfile.ProfileType.Steam) };
                    }
                    logger.Info("Save Data location loaded");
                }
            }
            catch (Exception e)
            {
                logger.Info(e, "Unable to fetch save data location");
            }
        }

        /// <summary>
        /// Finds the wiki's page for a game from the name the Xbox app gives the game.
        /// </summary>
        /// <returns>The page's ID, or null if the wiki has no page that is plainly this game's.</returns>
        private async Task<int?> findPage(string gameName)
        {
            string url = String.Format(@"https://www.pcgamingwiki.com/w/api.php?action=query&prop=revisions&titles={0}&formatversion=2&format=json", Uri.EscapeDataString(gameName));
            JsonNode queryRoot = JsonValue.Parse(await httpClient.DownloadStringAsync(url));
            if (queryRoot == null)
            {
                return null;
            }

            JsonNode page = queryRoot["query"]["pages"][0];
            if (page["missing"] == null || !page["missing"].GetValue<bool>())
            {
                return page["pageid"].GetValue<int>();
            }

            // A title is matched letter for letter, and the Xbox app often writes a name another way
            // than the wiki does: "FINAL FANTASY IX" for "Final Fantasy IX", "The Angler™" for
            // "The Angler", "All-Star" for "All Star". So search for the name, and take a page only if
            // its title is the same name once case, symbols and punctuation are left out. A page that
            // is merely similar could be another game, and its save folder would be the wrong one.
            string wanted = lettersAndDigits(gameName);
            if (wanted == String.Empty)
            {
                return null;
            }

            url = String.Format(@"https://www.pcgamingwiki.com/w/api.php?action=query&list=search&srsearch={0}&srnamespace=0&srlimit=5&formatversion=2&format=json", Uri.EscapeDataString(wordsOf(gameName)));
            JsonNode searchRoot = JsonValue.Parse(await httpClient.DownloadStringAsync(url));
            JsonArray hits = searchRoot?["query"]?["search"] as JsonArray;
            if (hits == null)
            {
                return null;
            }

            foreach (JsonNode hit in hits)
            {
                if (lettersAndDigits(hit["title"].GetValue<string>()) == wanted)
                {
                    return hit["pageid"].GetValue<int>();
                }
            }
            return null;
        }

        /// <summary>
        /// A name as words with one space between them. The wiki's search finds nothing for a word
        /// with a "™" stuck to it, and reads ":" and "-" as instructions.
        /// </summary>
        private static string wordsOf(string name)
        {
            return Regex.Replace(name, @"[^\p{L}\p{N}]+", " ").Trim();
        }

        private static string lettersAndDigits(string name)
        {
            return Regex.Replace(name, @"[^\p{L}\p{N}]+", String.Empty).ToLowerInvariant();
        }

        /// <summary>
        /// Picks the non-Xbox save folder out of a page's "Save game data location" section.
        /// </summary>
        /// <param name="usesProfile">True if the folder has to be completed with a profile the user picks.</param>
        /// <returns>The folder as the game library writes one, or null if the page gives none that can be used.</returns>
        internal static string chooseSaveLocation(string unparsedWikiTable, out bool usesProfile)
        {
            Dictionary<string, string> saveLocationTable = parseWikiTable(unparsedWikiTable);
            usesProfile = false;

            // The row for every Windows version if there is one, otherwise the Steam version's.
            string foundLocation;
            if (!saveLocationTable.TryGetValue("Windows", out foundLocation) && !saveLocationTable.TryGetValue("Steam", out foundLocation))
            {
                return null;
            }

            foundLocation = folderOf(foundLocation);
            if (foundLocation == String.Empty)
            {
                return null;
            }

            // Only then. A profile that the location has no place for could never be picked, and a
            // transfer is refused for as long as one has not been.
            usesProfile = foundLocation.Contains(GameLibrary.NonSteamProfileMarker);
            return foundLocation;
        }

        /// <summary>
        /// Turns a path from the wiki into a folder written the way the application expects one.
        /// </summary>
        private static string folderOf(string path)
        {
            // Pages often name the files, as in "Saves\*.sav". The folder is what is wanted.
            int lastSeparator = path.LastIndexOf('\\');
            if (path.IndexOf('*', lastSeparator + 1) != -1)
            {
                path = path.Substring(0, lastSeparator + 1);
            }

            // Files are listed relative to this folder. Without the separator at its end each of their
            // paths would start with one, and no translation written for the game would match.
            return path.EndsWith("\\") || path == String.Empty ? path : path + "\\";
        }

        /// <summary>
        /// Reads the rows of a "Save game data location" section.
        /// </summary>
        /// <returns>
        /// For each platform, the first path in its row that could be read in full. A row without one
        /// is left out, and so is anything the text does not finish.
        /// </returns>
        internal static Dictionary<string, string> parseWikiTable(string unparsedWikiTable)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            int rowStart = unparsedWikiTable.IndexOf(SaveRowStart);
            while (rowStart != -1)
            {
                // Null for a row that is never closed. The rows after it can still be read.
                List<string> fields = readTemplateFields(unparsedWikiTable, rowStart);

                // Field 0 is the template's name, field 1 the platform, and the paths follow.
                if (fields != null && fields.Count > 2)
                {
                    string platform = NameSubstitution(fields[1]).Trim();
                    string path = fields.Skip(2).Select(field => NameSubstitution(field).Trim()).FirstOrDefault(field => field != String.Empty && !field.Contains("{{"));

                    // A path that still has a template in it stands for a place only the wiki knows how
                    // to describe, such as the folder the game is installed in.
                    if (path != null && !result.ContainsKey(platform))
                    {
                        result.Add(platform, path);
                    }
                }

                rowStart = unparsedWikiTable.IndexOf(SaveRowStart, rowStart + SaveRowStart.Length);
            }

            return result;
        }

        /// <summary>
        /// Splits the template that starts at a position into its fields. A "|" inside a template
        /// within it belongs to that template and does not split.
        /// </summary>
        /// <returns>The fields, or null if the template is not closed.</returns>
        private static List<string> readTemplateFields(string text, int start)
        {
            List<string> fields = new List<string>();
            StringBuilder field = new StringBuilder();
            int depth = 0;

            for (int position = start; position < text.Length; position++)
            {
                bool pair = position + 1 < text.Length && text[position] == text[position + 1];
                if (pair && text[position] == '{')
                {
                    depth++;
                    if (depth > 1)
                    {
                        field.Append("{{");
                    }
                    position++;
                }
                else if (pair && text[position] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        fields.Add(field.ToString());
                        return fields;
                    }
                    field.Append("}}");
                    position++;
                }
                else if (text[position] == '|' && depth == 1)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(text[position]);
                }
            }
            return null;
        }

        internal static string NameSubstitution(string path)
        {
            for (int i = 0; i < FolderNameSubstitutions.GetLength(0); i++)
            {
                // "{{p|userprofile}}", and also "{{p|userprofile\Documents}}": the wiki puts a folder below
                // a known one inside the same template.
                string template = FolderNameSubstitutions[i, 0];
                string pattern = Regex.Escape(template.Substring(0, template.Length - 2)) + @"(\\[^{}|]*)?\}\}";
                path = Regex.Replace(path, pattern, FolderNameSubstitutions[i, 1].Replace("$", "$$") + "$1", RegexOptions.IgnoreCase);
            }
            return path;
        }
    }
}
