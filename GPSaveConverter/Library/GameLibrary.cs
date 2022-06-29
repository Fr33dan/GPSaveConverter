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

        internal static string UserLibraryVersion = null;
        private static Dictionary<string, GameInfo> savedGameLibrary;
        
        
        private static Dictionary<string, GameInfo> uwpLibrary;


        internal static BindingList<NonXboxFileInfo> nonXboxFiles = new BindingList<NonXboxFileInfo>();
        internal static BindingList<NonXboxProfile> nonXboxProfiles = new BindingList<NonXboxProfile>();

        internal static BindingList<Xbox.XboxFileInfo> xboxFiles = new BindingList<Xbox.XboxFileInfo>();


        private static StoredGameLibrary currentDefault = null;
        internal static StoredGameLibrary Default
        {
            get
            {
                if(currentDefault == null)
                {
                    currentDefault = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Settings.Default.DefaultGameLibrary);
                }
                return currentDefault;
            }
        }

        internal static bool Initialized { get { return savedGameLibrary != null; } }

        static GameLibrary()
        {
            DefaultTranslation = new FileTranslation();
        }

        public static async Task Initialize()
        {
            if (GPSaveConverter.Properties.Settings.Default.UserGameLibrary == string.Empty)
            {
                await Task.Run(FirstTimeInitializeGameLibrary);
            }
            else
            {
                if (GPSaveConverter.Properties.Settings.Default.AllowWebDataFetch)
                {
                    UpdateDefaultLibrary();
                }
            }
            await Task.Run(LoadSavedLibrary);
            await Task.Run(GetInstalledApps);
        }

        private static void FirstTimeInitializeGameLibrary()
        {
            GPSaveConverter.Properties.Settings.Default.DefaultGameLibrary = GPSaveConverter.Properties.Resources.GameLibrary;

            if (GPSaveConverter.Properties.Settings.Default.AllowWebDataFetch)
            {
                UpdateDefaultLibrary();
            }

            GPSaveConverter.Properties.Settings.Default.UserGameLibrary = GPSaveConverter.Properties.Settings.Default.DefaultGameLibrary;
            GPSaveConverter.Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Checks Github for the latest version of the game library.
        /// </summary>
        /// <returns>True if the default library was updated.</returns>
        internal static bool UpdateDefaultLibrary()
        {
            string sourceURL = @"https://raw.githubusercontent.com/Fr33dan/GPSaveConverter/master/GPSaveConverter/Resources/GameLibrary.json";
            bool returnVal = false;
            using (WebClient wc = new WebClient())
            {
                string githubLibraryJson = wc.DownloadString(sourceURL);

                StoredGameLibrary githubLibrary = JsonSerializer.Deserialize<StoredGameLibrary>(githubLibraryJson);

                if(githubLibrary.Version.CompareTo(Default.Version) > 0)
                {
                    currentDefault = githubLibrary;
                    GPSaveConverter.Properties.Settings.Default.DefaultGameLibrary = githubLibraryJson;
                    GPSaveConverter.Properties.Settings.Default.Save();
                    returnVal = true;
                    logger.Info("Default game library updated");
                }
                else
                {
                    logger.Info("Default game library up to date");
                }
            }

            return returnVal;
        }

        internal static void LoadDefaultLibrary()
        {
            UserLibraryVersion = Default.Version;
            foreach (GameInfo newGame in Default.GameInfo)
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
            IList<GameInfo> infoSource = null;
            try
            {
                StoredGameLibrary jsonLibrary = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Settings.Default.UserGameLibrary);

                UserLibraryVersion = jsonLibrary.Version;
                savedGameLibrary = new Dictionary<string, GameInfo>();
                infoSource = jsonLibrary.GameInfo;
            } catch(Exception e)
            {
                logger.Error(e);
            }
            if(infoSource == null)
            {
                infoSource = JsonSerializer.Deserialize<IList<GameInfo>>(GPSaveConverter.Properties.Settings.Default.UserGameLibrary);

                UserLibraryVersion = DateTime.Parse(Default.Version).AddDays(-1).ToString("yyyy-MM-dd");
                savedGameLibrary = new Dictionary<string, GameInfo>();
            }
            foreach (GameInfo newGame in infoSource)
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
            StoredGameLibrary returnVal = new StoredGameLibrary();

            returnVal.Version = UserLibraryVersion;
            returnVal.GameInfo = savedGameLibrary.Values.ToList();

            foreach(GameInfo uwpGame in uwpLibrary.Values)
            {
                if (uwpGame.NonUWPDataPopulated)
                {
                    GameInfo saveGameVersion;
                    if (savedGameLibrary.TryGetValue(uwpGame.PackageName,out saveGameVersion))
                    {
                        returnVal.GameInfo.Remove(saveGameVersion);
                    }
                    returnVal.GameInfo.Add(uwpGame);
                }
            }


            return JsonSerializer.Serialize(returnVal, options);
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
