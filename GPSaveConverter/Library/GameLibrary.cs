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
        internal const string NonSteamProfileMarker = "<user-id>";
        internal const string SteamInstallMarker = "<Steam-folder>";
        private static Dictionary<string, GameInfo> psvLibrary;
        private static Dictionary<string, GameInfo> uwpLibrary;

        static GameLibrary()
        {
            LoadPSV();
            uwpLibrary = GetInstalledApps();
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
                        string scriptOutput = ScriptManager.RunScript(scriptText).Trim();

                        foreach(string app in scriptOutput.Split(';'))
                        {
                            if (app != String.Empty)
                            {
                                string[] appInfo = app.Split('|');
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
                newGame.NonXboxSaveLocation = ExpandSaveFileLocation(newGame.NonXboxSaveLocation);
                psvLibrary.Add(newGame.PackageName, newGame);
            }

            stream.Close();

            /*StreamReader stream = new StreamReader(new MemoryStream(GPSaveConverter.Properties.Resources.GameLibrary));
            psvLibrary = new Dictionary<string, GameInfo>();
            while (!stream.EndOfStream)
            {
                GameInfo newGame = new GameInfo(stream.ReadLine());

                psvLibrary.Add(newGame.PackageName, newGame);
            }*/
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

            return returnVal;
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
                uwpInfo.NonXboxSaveLocation = psvInfo.NonXboxSaveLocation;
            }

            return uwpInfo;
        }
    }
}
