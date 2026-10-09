using System;
using System.IO;
using System.Text.RegularExpressions;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter.Library
{
    internal class Steam
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();

        private const ulong SteamID64IndividualProfile = 0x0110000100000000;

        // A 64-bit process only sees a 32-bit Steam install under WOW6432Node.
        private static readonly string[] InstallRegistryKeys = new string[] { @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam"
                                                                           , @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" };

        private readonly IFileSystem fileSystem;
        private readonly IRegistry registry;

        internal Steam(IFileSystem fileSystem, IRegistry registry)
        {
            this.fileSystem = fileSystem;
            this.registry = registry;
        }

        /// <summary>
        /// Gets the folder Steam is installed in.
        /// </summary>
        /// <returns>Null if Steam is not installed.</returns>
        internal static string GetInstallPath(IRegistry registry)
        {
            foreach (string key in InstallRegistryKeys)
            {
                string installPath = registry.GetValue(key, "InstallPath", null) as string;
                if (!string.IsNullOrEmpty(installPath))
                {
                    return installPath;
                }
            }
            return null;
        }

        /// <summary>
        /// Looks up the profile's display name and avatar in the files Steam keeps for accounts
        /// that have signed in on this PC. A profile Steam has no record of keeps its ID as its name.
        /// </summary>
        internal void GetUserInformation(NonXboxProfile profile)
        {
            try
            {
                string steamFolder = GetInstallPath(registry);
                if (steamFolder == null) return;

                ulong steamID64 = profile.IDType == NonXboxProfile.UserIDType.steamID3 ? GetSteamID64(profile.UserID) : ulong.Parse(profile.UserID);

                string loginUsersFile = Path.Combine(steamFolder, "config", "loginusers.vdf");
                if (fileSystem.FileExists(loginUsersFile))
                {
                    string personaName = ParsePersonaName(fileSystem.ReadAllText(loginUsersFile), steamID64);
                    if (personaName != null)
                    {
                        profile.UserName = personaName;
                    }
                }

                string avatarFile = Path.Combine(steamFolder, "config", "avatarcache", steamID64 + ".png");
                if (fileSystem.FileExists(avatarFile))
                {
                    profile.UserIconLocation = avatarFile;
                }
            }
            catch (Exception e)
            {
                logger.Debug(e, "Unable to read Steam profile information");
            }
        }

        internal System.Drawing.Bitmap LoadIcon(NonXboxProfile profile)
        {
            System.Drawing.Bitmap returnVal = null;
            if (profile.UserIconLocation == null) return returnVal;

            try
            {
                // Load from a copy in memory so Steam's own file is not left locked.
                byte[] imageData = fileSystem.ReadAllBytes(profile.UserIconLocation);

                returnVal = new System.Drawing.Bitmap(new System.IO.MemoryStream(imageData));
            }
            catch (Exception e)
            {
                logger.Debug(e, "Unable to load Steam profile icon");
                profile.UserIconLocation = null;
            }
            return returnVal;
        }

        /// <summary>
        /// Finds a user's display name in the contents of Steam's loginusers.vdf.
        /// </summary>
        /// <returns>Null if the user is not listed or has no name.</returns>
        internal static string ParsePersonaName(string loginUsers, ulong steamID64)
        {
            // Quoted values are matched whole so a brace inside a name does not end the block early.
            Match user = Regex.Match(loginUsers, "\"" + steamID64 + "\"\\s*\\{(?<Properties>(\"(\\\\.|[^\"\\\\])*\"|[^{}\"])*)\\}");
            if (!user.Success) return null;

            Match name = Regex.Match(user.Groups["Properties"].Value, "\"PersonaName\"\\s+\"(?<Name>(\\\\.|[^\"\\\\])*)\"", RegexOptions.IgnoreCase);
            if (!name.Success || name.Groups["Name"].Length == 0) return null;

            // Steam writes quotes and backslashes with a leading backslash.
            return Regex.Replace(name.Groups["Name"].Value, @"\\(.)", "$1");
        }

        /// <summary>
        /// https://developer.valvesoftware.com/wiki/SteamID
        /// </summary>
        /// <param name="steam3ID"></param>
        /// <param name="accoundIDY"></param>
        /// <returns></returns>
        internal static ulong GetSteamID64(string steam3ID)
        {
            ulong steam3IDValue = ulong.Parse(steam3ID);

            return SteamID64IndividualProfile | (steam3IDValue);
        }
    }
}
