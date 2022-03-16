using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GPSaveConverter.Library
{
    internal class GameLibrary
    {
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();
        public static readonly FileTranslation DefaultTranslation;
        internal const string NonSteamProfileMarker = "<user-id>";
        internal const string SteamInstallMarker = "<Steam-folder>";
        private static Dictionary<string, GameInfo> psvLibrary;
        private static Dictionary<string, GameInfo> uwpLibrary;

        public static string ProfileID;

        static GameLibrary()
        {
            LoadPSV();
            uwpLibrary = GetInstalledApps();

            DefaultTranslation = new FileTranslation();
        }

        /// <summary>
        /// Gets a list of installed UWP Apps on the system, containing each app name + AUMID, separated by '|' 
        /// </summary>
        /// <returns>List of installed UWP Apps</returns>
        public static Dictionary<string, GameInfo> GetInstalledApps()
        {
            Dictionary<string, GameInfo> result = new Dictionary<string, GameInfo>();
            try
            {
                using (Stream stream = new MemoryStream(GPSaveConverter.Properties.Resources.GetAUMIDScript))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string scriptText = reader.ReadToEnd();
                        logger.Debug("Starting Powershell script to retreive UWP package manifests.");
                        string scriptOutput = ScriptManager.RunScript(scriptText).Trim();
                        logger.Trace("Powershell script results:\n{0}", scriptOutput);

                        StringReader sr = new StringReader(scriptOutput);

                        string appInfoLine = null;
                        while ((appInfoLine = sr.ReadLine()) != null)
                        {
                            string[] appInfo = appInfoLine.Split('|');
                            GameInfo gameInfo = new GameInfo();
                            string name = appInfo[0].Trim();
                            gameInfo.IconLocation = appInfo[1];
                            gameInfo.PackageName = appInfo[2];
                            gameInfo.Name = name == string.Empty? gameInfo.PackageName : name;
                            if (!result.ContainsKey(gameInfo.PackageName))
                            {
                                result.Add(gameInfo.PackageName, gameInfo);
                            }
                        }

                        logger.Debug("Found {0} UWP Packages.", result.Count);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error trying to get installed apps on your PC " + Environment.NewLine + e.Message, e.InnerException);
            }

            return result;
        }

        public static void LoadPSV()
        {
            StreamReader stream = new StreamReader(new MemoryStream(GPSaveConverter.Properties.Resources.GameLibrary));

            IList<GameInfo> jsonLibrary = JsonSerializer.Deserialize<IList<GameInfo>>(stream.ReadToEnd());

            psvLibrary = new Dictionary<string, GameInfo>();
            foreach (GameInfo newGame in jsonLibrary)
            {
                newGame.nonUWPFetched = true;
                newGame.BaseNonXboxSaveLocation = newGame.BaseNonXboxSaveLocation;
                psvLibrary.Add(newGame.PackageName, newGame);
            }

            stream.Close();
        }

        public static string ExpandSaveFileLocation(string unexpanedSaveFileLocation)
        {
            string returnVal = Environment.ExpandEnvironmentVariables(unexpanedSaveFileLocation);

            if (returnVal.Contains(SteamInstallMarker))
            {
                string steamLocation = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null);
                if(steamLocation == null)
                {
                    returnVal = null;
                }
                else
                {
                    returnVal = returnVal.Replace(SteamInstallMarker, steamLocation);
                }
            }
            if (returnVal.Contains(NonSteamProfileMarker))
            {
                returnVal = returnVal.Replace(NonSteamProfileMarker, ProfileID);
            }


            return returnVal;
        }


        public static async Task FetchNonUWPInformation(GameInfo i)
        {
            GameInfo psvInfo;
            i.nonUWPFetched = true;

            if (psvLibrary.TryGetValue(i.PackageName, out psvInfo))
            {
                RegisterSerializedInfo(psvInfo);
            }

            if (i.BaseNonXboxSaveLocation == null && GPSaveConverter.Properties.Settings.Default.AllowWebDataFetch)
            {
                await PCGameWiki.FetchSaveLocation(i);
            }
        }

        public static void RegisterSerializedInfo(GameInfo deserializedInfo)
        {
            GameInfo i = getGameInfo(deserializedInfo.PackageName);
            i.BaseNonXboxSaveLocation = deserializedInfo.BaseNonXboxSaveLocation;
            i.FileTranslations.AddRange(deserializedInfo.FileTranslations);
            i.WGSProfileSuffix = deserializedInfo.WGSProfileSuffix;
        }
        

        public static string GetNonXboxProfileLocation(string baseSaveFileLocation)
        {
            return ExpandSaveFileLocation(baseSaveFileLocation.Substring(0, baseSaveFileLocation.IndexOf(Library.GameLibrary.NonSteamProfileMarker)));
        }

        public static GameInfo getGameInfo(string packageName)
        {
            GameInfo uwpInfo;

            if (!uwpLibrary.TryGetValue(packageName, out uwpInfo))
            {
                throw new Exception("Game info not found in UWP library");
            }
            return uwpInfo;
        }
    }
}
