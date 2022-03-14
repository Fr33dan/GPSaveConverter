using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GPSaveConverter.Library
{
    internal class GameLibrary
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();
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
                            gameInfo.Name = name == string.Empty? null: name;
                            gameInfo.IconLocation = appInfo[1];
                            gameInfo.PackageName = appInfo[2];
                            if (!result.ContainsKey(gameInfo.PackageName))
                            {
                                result.Add(gameInfo.PackageName, gameInfo);
                            }
                        }
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

        public static string GetNonXboxProfileLocation(string baseSaveFileLocation)
        {
            return ExpandSaveFileLocation(baseSaveFileLocation.Substring(0, baseSaveFileLocation.IndexOf(Library.GameLibrary.NonSteamProfileMarker)));
        }

        public static GameInfo getGameInfo(string gamePassID)
        {
            GameInfo uwpInfo;

            if (!uwpLibrary.TryGetValue(gamePassID, out uwpInfo))
            {
                throw new Exception("Game info no found in UWP library");
            }
            GameInfo psvInfo;

            if(psvLibrary.TryGetValue(gamePassID, out psvInfo))
            {
                uwpInfo.BaseNonXboxSaveLocation = psvInfo.BaseNonXboxSaveLocation;
                uwpInfo.FileTranslations = psvInfo.FileTranslations;
                uwpInfo.WGSProfileSuffix = psvInfo.WGSProfileSuffix;
            }

            return uwpInfo;
        }
    }
}
