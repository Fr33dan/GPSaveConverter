using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Net;
using System.Text.RegularExpressions;

namespace GPSaveConverter.Library
{
    internal static class PCGameWiki
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();
        public static readonly string[,] FolderNameSubstitutions = new string[,] { { "{{p|userprofile}}", "	%USERPROFILE%" }
                                                                                 , { "{{p|appdata}}", "	%APPDATA%" }
                                                                                 , { "{{p|localappdata}}", "%LOCALAPPDATA%" }
                                                                                 , { "{{p|programdata}}", "	%PROGRAMDATA%" }
                                                                                 , { "{{p|uid}}", "<user-id>"}
                                                                                 , { "{{p|steam}}", "<Steam-folder>"} };
        public static async Task FetchSaveLocation(GameInfo i)
        {
            logger.Info("Fetching save data from pcgamingwiki.com");
            string wikiTable = null;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    string url = String.Format(@"https://www.pcgamingwiki.com/w/api.php?action=query&prop=revisions&titles={0}&formatversion=2&format=json", i.Name);
                    string queryJson = await wc.DownloadStringTaskAsync(url);

                    JsonNode queryRoot = JsonValue.Parse(queryJson);

                    if (queryRoot == null)
                    {
                        return;
                    }
                    if (queryRoot["query"]["pages"][0]["missing"] != null && queryRoot["query"]["pages"][0]["missing"].AsValue().GetValue<bool>())
                    {
                        return;
                    }
                    int pageID = queryRoot["query"]["pages"][0]["pageid"].AsValue().GetValue<int>();


                    url = String.Format(@"https://www.pcgamingwiki.com/w/api.php?action=parse&pageid={0}&formatversion=2&format=json&prop=sections", pageID);
                    string sectionsJson = await wc.DownloadStringTaskAsync(url);
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
                    string saveFileSectionJson = await wc.DownloadStringTaskAsync(url);
                    JsonNode saveFileSectionRoot = JsonValue.Parse(saveFileSectionJson);
                    wikiTable = saveFileSectionRoot["parse"]["wikitext"].GetValue<string>();

                }
                catch (Exception e)
                {
                    logger.Info(e, "Unable to fetch save data location");
                }
            }

            if (wikiTable != null)
            {
                string foundLocation;
                Dictionary<string, string> saveLocationTable = parseWikiTable(wikiTable);
                if (saveLocationTable.TryGetValue("Windows", out foundLocation) && foundLocation != String.Empty)
                {
                    i.BaseNonXboxSaveLocation = foundLocation;
                    logger.Info("Save Data location loaded");
                }
                else
                {
                    if (saveLocationTable.TryGetValue("Steam", out foundLocation))
                    {
                        i.BaseNonXboxSaveLocation = foundLocation;
                        i.TargetProfileTypes = new NonXboxProfile.ProfileType[] { NonXboxProfile.ProfileType.Steam };
                        i.TargetProfiles = new NonXboxProfile[] { new NonXboxProfile(0, NonXboxProfile.ProfileType.Steam) };
                        logger.Info("Save Data location loaded");
                    }
                }
            }
        }


        private static Dictionary<string, string> parseWikiTable(string unparsedWikiTable)
        {
            int entryStart = unparsedWikiTable.IndexOf("{{Game data/saves|");
            int entryEnd = entryStart;
            Dictionary<string, string> result = new Dictionary<string, string>();
            while (entryStart != -1)
            {
                int subEntryEnd = entryStart;
                int subEntryStart = entryStart;
                do
                {
                    entryEnd = unparsedWikiTable.IndexOf("}}", subEntryEnd + 2);
                    subEntryStart = unparsedWikiTable.IndexOf("{{", subEntryEnd + 2, entryEnd - subEntryEnd - 2);
                    subEntryEnd = entryEnd;
                } while (subEntryStart != -1);

                string entryLine = unparsedWikiTable.Substring(entryStart, entryEnd - entryStart);
                entryLine = NameSubsitution(entryLine);
                string[] lineInfo = entryLine.Split('|');

                result.Add(lineInfo[1], lineInfo[2]);

                entryStart = unparsedWikiTable.IndexOf("{{Game data/saves|", entryEnd);
            }

            return result;
        }

        private static string NameSubsitution(string path)
        {
            for (int i = 0; i < FolderNameSubstitutions.GetLength(0); i++)
            {
                path = Regex.Replace(path, Regex.Escape(FolderNameSubstitutions[i, 0]), FolderNameSubstitutions[i, 1].Replace("$", "$$"),RegexOptions.IgnoreCase);

                //path.Replace(FolderNameSubstitutions[i,0], FolderNameSubstitutions[i,1]);
            }
            return path;
        }
    }
}
