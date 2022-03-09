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
        private static Dictionary<string, GameInfo> library;

        static GameLibrary()
        {
            LoadPSV();
        }

        public static void LoadPSV()
        {
            StreamReader stream = new StreamReader(new MemoryStream(GPSaveConverter.Properties.Resources.GameLibrary));
            library = new Dictionary<string, GameInfo>();
            while (!stream.EndOfStream)
            {
                GameInfo newGame = new GameInfo(stream.ReadLine());
                library.Add(newGame.PackageName, newGame);
            }
        }

        public static GameInfo getGameInfo(string gamePassID)
        {
            GameInfo location;

            if(!library.TryGetValue(gamePassID, out location))
            {
                location = new GameInfo();
                location.PackageName = gamePassID;
            }

            return location;
        }
    }
}
