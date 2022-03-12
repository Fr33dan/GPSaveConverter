using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GPSaveConverter.Library
{
    internal class GameInfo
    {
        public string Name { get; set; }

        [Browsable(false)]
        public string PackageName { get; set; }

        [Browsable(false)]
        public string NonXboxSaveLocation
        {
            get
            {
                return GameLibrary.ExpandSaveFileLocation(BaseNonXboxSaveLocation);
            }
        }

        [Browsable(false)]
        public string BaseNonXboxSaveLocation { get; set; }

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

        [Browsable(false)]
        public IList<FileTranslation> FileTranslations { get; set; }

        

        private Image gameIcon;

        internal GameInfo(string psvDataLine)
        {
            string[] gameData = psvDataLine.Split('|');
            this.PackageName = gameData[0];
            this.BaseNonXboxSaveLocation = gameData[1];
        }
        public GameInfo()
        {
        }

        private FileTranslation findTranslation(NonXboxFileInfo file)
        {
            foreach (FileTranslation t in this.FileTranslations)
            {
                if (Regex.Match(file.RelativePath, t.NonXboxRegex).Success)
                {
                    return t;
                }
            }
            return null;
        }
        private FileTranslation findTranslation(Xbox.XboxFileInfo file)
        {
            if (this.FileTranslations != null)
            {
                foreach (FileTranslation t in this.FileTranslations)
                {
                    if (file.ContainerName1 == t.ContainerName1
                        && file.ContainerName2 == t.ContainerName2
                        && Regex.Match(file.FileID, t.XboxFileIDRegex).Success)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        internal NonXboxFileInfo getNonXboxFileVersion(Xbox.XboxFileInfo file, bool createOrUpdate = false)
        {
            FileTranslation t = findTranslation(file);
            NonXboxFileInfo returnVal = null;

            if (t != null)
            {
                returnVal = new NonXboxFileInfo();

                returnVal.RelativePath = Regex.Replace(file.FileID, t.XboxFileIDRegex, t.XboxFileIDSubstitution);

                returnVal.FilePath = Path.Combine(this.NonXboxSaveLocation, returnVal.RelativePath);

                if (!File.Exists(returnVal.FilePath) && !createOrUpdate)
                {
                    return null;
                }
                else
                {
                    returnVal.Timestamp = File.GetLastWriteTime(returnVal.FilePath);
                }

                if (createOrUpdate)
                {
                    File.Copy(file.getFilePath(), returnVal.FilePath, true);
                    returnVal.Timestamp = File.GetLastWriteTime(returnVal.FilePath);
                }
            } else if (createOrUpdate)
            {
                throw new Exception("Relative file path translation to Xbox file ID not found.");

            }
            return returnVal;
        }

        internal Xbox.XboxFileInfo getXboxFileVersion(Xbox.XboxContainerIndex index, NonXboxFileInfo file, bool createOrUpdate = false)
        {
            FileTranslation t = findTranslation(file);

            Xbox.XboxFileInfo matchedFile = null;
            if (t != null)
            {
                IEnumerable<Xbox.XboxFileContainer> containers = index.Children.Where(c => c.ContainerID[0] == t.ContainerName1 && c.ContainerID[1] == t.ContainerName2);

                if (containers.Count() > 1)
                {
                    throw new ArgumentException("Ambiguous Xbox container results");
                }
                else if (containers.Count() == 1)
                {
                    Xbox.XboxFileContainer xboxFileContainer = containers.First();

                    string xboxFileID = Regex.Replace(file.RelativePath, t.NonXboxRegex, t.NonXboxSubstitution);

                    matchedFile = xboxFileContainer.getFileList().Where(f => f.FileID == xboxFileID).FirstOrDefault();

                    if (createOrUpdate)
                    {
                        if (matchedFile != null)
                        {
                            matchedFile.Replace(file);
                        }
                        else
                        {
                            matchedFile = xboxFileContainer.AddFile(file, xboxFileID);
                        }
                    }
                }
                else
                {
                    if (createOrUpdate)
                    {
                        throw new Exception("Target Xbox container does not exist, container creation is not supported at this time");
                    }
                }
            } else if (createOrUpdate)
            {
                throw new Exception("Relative file path translation to Xbox file ID not found.");
            }
            return matchedFile;
        }

        public override string ToString()
        {
            return this.Name == null ? this.PackageName : this.Name;
        }
    }
}
