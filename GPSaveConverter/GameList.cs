using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal class GameList
    {
        private static string[] internalList;
        static GameList()
        {
            string packageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
            List<string> list = new List<string>();
            foreach(string package in Directory.GetDirectories(packageFolder))
            {
                string wgsFolder = Path.Combine(package, "SystemAppData", "wgs");
                if(Directory.Exists(wgsFolder) && Directory.GetDirectories(wgsFolder).Length == 2)
                {
                    ;
                    list.Add(Path.GetFileName(package));
                }
            }
            internalList = list.ToArray();
        }

        public static string[] GetList()
        {
            return internalList.ToArray();
        }
    }
}
