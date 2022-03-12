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
                if (gameIcon == null)
                {
                    if (File.Exists(IconLocation))
                    {
                        gameIcon = Image.FromFile(IconLocation);
                    }
                    else
                    {
                        gameIcon = new Bitmap(150, 150);
                    }
                }
                return gameIcon;
            }
        }

        

        private Image gameIcon;

        internal GameInfo(string psvDataLine)
        {
            string[] gameData = psvDataLine.Split('|');
            this.PackageName = gameData[0];
            this.NonXboxSaveLocation = GameLibrary.ExpandSaveFileLocation(gameData[1]);
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
