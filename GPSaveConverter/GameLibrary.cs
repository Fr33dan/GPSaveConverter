using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal class GameLibrary
    {
        private static Dictionary<string, string> library;

        static GameLibrary()
        {
            LoadCSV("GameLibrary.psv");
        }

        public static void LoadCSV(string libraryFile)
        {
            library = new Dictionary<string, string>();
            string[] gameList = File.ReadAllLines(libraryFile);
            foreach(string game in gameList)
            {
                string[] gameData = game.Split('|');
                library.Add(gameData[0], gameData[1]);
            }
        }

        public static string getNonXboxSaveLocation(string gamePassID)
        {
            string location = null;

            if(library.TryGetValue(gamePassID, out location))
            {
                location = Environment.ExpandEnvironmentVariables(location);
            }

            return location;
        }
    }
}
