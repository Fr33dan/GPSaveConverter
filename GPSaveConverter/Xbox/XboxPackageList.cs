using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Xbox
{
    internal class XboxPackageList
    {
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();
        private static Library.GameInfo[] internalList;
        static XboxPackageList()
        {
            logger.Info("Loading Xbox Package List...");
            string packageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
            List<Library.GameInfo> list = new List<Library.GameInfo>();
            foreach(string package in Directory.GetDirectories(packageFolder))
            {
                string wgsFolder = Path.Combine(package, "SystemAppData", "wgs");
                if(Directory.Exists(wgsFolder) && Directory.GetDirectories(wgsFolder).Length >= 2)
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
            internalList = list.ToArray();
            if (internalList.Length > 0)
            {
                logger.Info("Xbox packages loaded!");
            }
            else
            {
                logger.Info("No Xbox packages found.");
            }
        }
        internal static string getWGSFolder(string packageName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", packageName, "SystemAppData", "wgs") + "\\";
        }

        public static Library.GameInfo[] GetList()
        {
            return internalList.ToArray();
        }
    }
}
