using System;

namespace GPSaveConverter
{
    internal class GameInfo
    {
        public string Name { get; set; }
        public string PackageName { get; set; }
        public string NonXboxSaveLocation { get; set; }

        internal GameInfo(string psvDataLine)
        {
            string[] gameData = psvDataLine.Split('|');
            this.Name = gameData[0];
            this.PackageName = gameData[1];
            this.NonXboxSaveLocation = Environment.ExpandEnvironmentVariables(gameData[2]);
        }
        internal GameInfo()
        {
        }

        public override string ToString()
        {
            return this.Name == null ? this.PackageName : this.Name;
        }
    }
}
