using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter.Xbox
{
    internal class XboxPackageList
    {
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();

        internal static IEnvironment Environment { get; set; } = new DefaultEnvironment();
        internal static IFileSystem FileSystem { get; set; } = new DefaultFileSystem();

        /// <summary>
        /// Looks through the Packages folder and lists the games that have a save there.
        /// </summary>
        /// <remarks>
        /// The folder is read when the list is asked for. It used to be read once, the first time
        /// anything touched this class, and a test touches it just to put a stand-in in place. That
        /// read the real Packages folder, and a game found there started the game library loading
        /// behind the test's back.
        /// </remarks>
        public static Library.GameInfo[] GetList()
        {
            logger.Info("Loading Xbox Package List...");
            string packageFolder = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Packages");
            List<Library.GameInfo> list = new List<Library.GameInfo>();
            foreach(string package in FileSystem.GetDirectories(packageFolder))
            {
                string wgsFolder = Path.Combine(package, "SystemAppData", "wgs");
                if(FileSystem.DirectoryExists(wgsFolder) && XboxHelper.HoldsSaves(FileSystem.GetDirectories(wgsFolder)))
                {
                    try
                    {
                        Library.GameInfo info = Library.GameLibrary.getGameInfo(Path.GetFileName(package));
                        list.Add(info);
                    }
                    catch(Exception ex)
                    {
                        logger.Info(String.Format("Could not load Xbox package {0}: {1}", Path.GetFileName(package), ex.Message));
                    }
                }
            }
            if (list.Count > 0)
            {
                logger.Info("Xbox packages loaded!");
            }
            else
            {
                logger.Info("No Xbox packages found.");
            }
            return list.ToArray();
        }

        internal static string getWGSFolder(string packageName)
        {
            return Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Packages", packageName, "SystemAppData", "wgs") + "\\";
        }
    }
}
