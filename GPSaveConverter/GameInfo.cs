using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;

namespace GPSaveConverter
{
    internal class GameInfo
    {
        public string Name { get; set; }

        [Browsable(false)]
        public string PackageName { get; set; }

        [Browsable(false)]
        public string NonXboxSaveLocation { get; set; }

        [Browsable(false)]
        public string IconLocation { get; set; }

        public Image GameIcon
        {
            get
            {
                Image returnVal = null;
                if (File.Exists(IconLocation))
                {
                    returnVal = Image.FromFile(IconLocation);
                }
                else
                {
                    returnVal = new Bitmap(150, 150);
                }
                return returnVal;
            }
        }

        internal GameInfo(string psvDataLine)
        {
            string[] gameData = psvDataLine.Split('|');
            this.Name = gameData[0];
            this.PackageName = gameData[1];
            this.NonXboxSaveLocation = GameLibrary.ExpandSaveFileLocation(gameData[2]);
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
