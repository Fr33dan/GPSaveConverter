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
            LoadPSV();
        }

        public static void LoadPSV()
        {
            StreamReader stream = new StreamReader(new MemoryStream(GPSaveConverter.Properties.Resources.GameLibrary));
            library = new Dictionary<string, string>();
            while (!stream.EndOfStream)
            {
                string game = stream.ReadLine();
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
