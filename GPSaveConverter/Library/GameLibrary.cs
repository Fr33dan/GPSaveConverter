using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private static Dictionary<string, GameInfo> savedGameLibrary;
        private static Dictionary<string, GameInfo> uwpLibrary;


        internal static BindingList<NonXboxFileInfo> nonXboxFiles = new BindingList<NonXboxFileInfo>();
        internal static BindingList<NonXboxProfile> nonXboxProfiles = new BindingList<NonXboxProfile>();

        internal static BindingList<Xbox.XboxFileInfo> xboxFiles = new BindingList<Xbox.XboxFileInfo>();


        internal static bool Initialized { get { return savedGameLibrary != null; } }

        static GameLibrary()
        {
            DefaultTranslation = new FileTranslation();
        }

        public static async Task Initialize()
        {
            if (GPSaveConverter.Properties.Settings.Default.UserGameLibrary == string.Empty) await Task.Run(FirstTimeInitializeGameLibrary);
            await Task.Run(LoadSavedLibrary);
            await Task.Run(GetInstalledApps);
        }

        private static void FirstTimeInitializeGameLibrary()
        {
            GPSaveConverter.Properties.Settings.Default.UserGameLibrary = GPSaveConverter.Properties.Resources.GameLibrary;
            GPSaveConverter.Properties.Settings.Default.DefaultGameLibrary = GPSaveConverter.Properties.Resources.GameLibrary;
            GPSaveConverter.Properties.Settings.Default.Save();
        }

        internal static void LoadDefaultLibrary()
        {
            StoredGameLibrary jsonLibrary = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Resources.GameLibrary);

            foreach (GameInfo newGame in jsonLibrary.GameInfo)
            {
                GameInfo existingInfo;
                if(savedGameLibrary.TryGetValue(newGame.PackageName,out existingInfo))
                {
                    existingInfo.ApplyDeserializedInfo(newGame);
                }
                else
                {
                    savedGameLibrary.Add(newGame.PackageName, newGame);
                }
            }
        }

        private static void LoadSavedLibrary()
        {
            StoredGameLibrary jsonLibrary = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Settings.Default.UserGameLibrary);
            
            savedGameLibrary = new Dictionary<string, GameInfo>();
            foreach (GameInfo newGame in jsonLibrary.GameInfo)
            {
                savedGameLibrary.Add(newGame.PackageName, newGame);
            }

        }

        private static void CheckInitialization()
        {
            if (uwpLibrary == null) Initialize();
        }

        /// <summary>
        /// Gets a list of installed UWP Apps on the system, containing each app name + AUMID, separated by '|' 
        /// </summary>
        /// <returns>List of installed UWP Apps</returns>
        public static void GetInstalledApps()
        {
            uwpLibrary = new Dictionary<string, GameInfo>();
            try
            {
                string scriptText = GPSaveConverter.Properties.Resources.GetAUMIDScript;
                logger.Info("Getting UWP package manifests...");
                
                string scriptOutput = ScriptManager.RunScript(scriptText).Trim();
                logger.Trace("Powershell script results:\n{0}", scriptOutput);

                StringReader sr = new StringReader(scriptOutput);

                string appInfoLine = null;
                while ((appInfoLine = sr.ReadLine()) != null)
                {
                    string[] appInfo = appInfoLine.Split('|');
                    GameInfo gameInfo = new GameInfo();
                    if (appInfo.Length != 3)
                    {
                        logger.Warn("Malformed result line from UWP package script: {0}", appInfoLine);
                    }
                    else
                    {
                        string name = appInfo[0].Trim();
                        gameInfo.IconLocation = appInfo[1];
                        gameInfo.PackageName = appInfo[2];
                        gameInfo.Name = name == string.Empty ? gameInfo.PackageName : name;
                        if (!uwpLibrary.ContainsKey(gameInfo.PackageName))
                        {
                            uwpLibrary.Add(gameInfo.PackageName, gameInfo);
                        }
                    }
                }

                logger.Debug("Found {0} UWP Packages.", uwpLibrary.Count);
                logger.Info("UWP manifests successfully loaded.", uwpLibrary.Count);

            }
            catch (Exception e)
            {
                throw new Exception("Error trying to get installed apps on your PC " + Environment.NewLine + e.Message, e.InnerException);
            }
        }

        public static string GetLibraryJson(JsonSerializerOptions options = null)
        {
            List<GameInfo> loadedLibrary = savedGameLibrary.Values.ToList();

            foreach(GameInfo uwpGame in uwpLibrary.Values)
            {
                if (uwpGame.NonUWPDataPopulated)
                {
                    GameInfo saveGameVersion;
                    if (savedGameLibrary.TryGetValue(uwpGame.PackageName,out saveGameVersion))
                    {
                        loadedLibrary.Remove(saveGameVersion);
                    }
                    loadedLibrary.Add(uwpGame);
                }
            }

            return JsonSerializer.Serialize(loadedLibrary, options);
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


        public static async Task PopulateNonUWPInformation(GameInfo i)
        {
            CheckInitialization();
            GameInfo saveLibraryInfo;
            i.NonUWPDataPopulated = true;

            if (savedGameLibrary.TryGetValue(i.PackageName, out saveLibraryInfo))
            {
                RegisterSerializedInfo(saveLibraryInfo);
            }

            if (i.BaseNonXboxSaveLocation == null && GPSaveConverter.Properties.Settings.Default.AllowWebDataFetch)
            {
                await PCGameWiki.FetchSaveLocation(i);
            }
        }

        public static void RegisterSerializedInfo(GameInfo deserializedInfo)
        {
            CheckInitialization();
            GameInfo i = getGameInfo(deserializedInfo.PackageName);
            i.ApplyDeserializedInfo(deserializedInfo);
        }
        

        public static string GetNonXboxProfileLocation(string baseSaveFileLocation)
        {
            return ExpandSaveFileLocation(baseSaveFileLocation.Substring(0, baseSaveFileLocation.IndexOf(Library.GameLibrary.NonSteamProfileMarker)));
        }

        public static GameInfo getGameInfo(string packageName)
        {
            CheckInitialization();
            GameInfo uwpInfo;

            if (!uwpLibrary.TryGetValue(packageName, out uwpInfo))
            {
                logger.Warn("Game information for package {0} not found in UWP library. Game installation may be corrupt.", packageName);
                uwpInfo = new GameInfo();
                uwpInfo.PackageName = packageName;
                uwpLibrary.Add(packageName, uwpInfo);
            }
            return uwpInfo;
        }
    }
}
