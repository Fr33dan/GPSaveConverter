using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GPSaveConverter.Library
{
    internal class GameInfo
    {
        private static NLog.Logger logger = LogHelper.getClassLogger();

        internal bool NonUWPDataPopulated = false;
        public string Name { get; set; }

        [Browsable(false)]
        public string PackageName { get; set; }

        [Browsable(false), JsonIgnore]
        public string NonXboxSaveLocation
        {
            get
            {
                return GameLibrary.ExpandSaveFileLocation(BaseNonXboxSaveLocation);
            }
        }

        private string baseNonXboxSaveLocation;

        [Browsable(false)]
        public string BaseNonXboxSaveLocation
        {
            get
            {
                return baseNonXboxSaveLocation;
            }
            set
            {
                baseNonXboxSaveLocation = value;
            }
        }

        [Browsable(false), JsonIgnore]
        public string IconLocation { get; set; }

        [JsonIgnore]
        public Image GameIcon
        {
            get
            {
                if (gameIcon == null)
                {
                    if (File.Exists(IconLocation))
                    {
                        gameIcon = Image.FromFile(IconLocation);
                    } else if (File.Exists(IconLocation.Replace(".png", ".scale-200.png")))
                    {
                        gameIcon = Image.FromFile(IconLocation.Replace(".png", ".scale-200.png"));

                    }
                    else
                    {
                        gameIcon = new Bitmap(150, 150);
                    }
                }
                return gameIcon;
            }
        }

        private string wgsProfileSuffix;
        [Browsable(false)]
        public string WGSProfileSuffix
        {
            get
            {
                return wgsProfileSuffix;
            }
            set { wgsProfileSuffix = value; }
        }
        private List<FileTranslation> fileTranslations;
        [Browsable(false)]
        public List<FileTranslation> FileTranslations
        {
            get
            {
                return fileTranslations;
            }
            set { fileTranslations = value; }
        }



        private Image gameIcon;

        public GameInfo()
        {
            this.fileTranslations = new List<FileTranslation>();
        }

        private FileTranslation findTranslation(NonXboxFileInfo file)
        {
            foreach (FileTranslation t in this.FileTranslations)
            {
                if (Regex.Match(file.RelativePath, t.NonXboxFilenameRegex).Success)
                {
                    return t;
                }
            }
            
            return null;
        }
        private FileTranslation findTranslation(Xbox.XboxFileInfo file)
        {
            foreach (FileTranslation t in this.FileTranslations)
            {
                if (Regex.Match(file.ContainerName1, t.ContainerName1Regex).Success
                    && Regex.Match(file.ContainerName1, t.ContainerName1Regex).Success
                    && Regex.Match(file.FileID, t.XboxFileIDRegex).Success)
                {
                    return t;
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

                returnVal.RelativePath = Regex.Replace(file.FileID, t.XboxFileIDRegex, t.NonXboxFilename);
                returnVal.RelativePath = Regex.Replace(file.ContainerName1, t.ContainerName1Regex, returnVal.RelativePath);
                returnVal.RelativePath = Regex.Replace(file.ContainerName2, t.ContainerName2Regex, returnVal.RelativePath);

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
                    logger.Info("Extracting Xbox save file: {0} -> {1}", file.FileID, returnVal.FilePath);
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
                string container1Name = Regex.Replace(file.RelativePath, t.NonXboxFilenameRegex, t.ContainerName1);
                string container2Name = Regex.Replace(file.RelativePath, t.NonXboxFilenameRegex, t.ContainerName2);
                IEnumerable<Xbox.XboxFileContainer> containers = index.Children.Where(c => c.ContainerID[0] == container1Name && c.ContainerID[1] == container2Name);

                if (containers.Count() > 1)
                {
                    throw new ArgumentException("Ambiguous Xbox container results");
                }
                else if (containers.Count() == 1)
                {
                    Xbox.XboxFileContainer xboxFileContainer = containers.First();

                    string xboxFileID = Regex.Replace(file.RelativePath, t.NonXboxFilenameRegex, t.XboxFileID);

                    matchedFile = xboxFileContainer.getFileList().Where(f => f.FileID == xboxFileID).FirstOrDefault();

                    if (createOrUpdate)
                    {
                        if (matchedFile != null)
                        {
                            logger.Info("Replacing existing Xbox Save file: {0} -> {1} ({2})", file.FilePath, matchedFile.FileID, matchedFile.getFileName());
                            matchedFile.Replace(file);
                        }
                        else
                        {
                            logger.Info("Adding Xbox Save file: {0} -> {1}", file.FilePath, xboxFileID);
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
