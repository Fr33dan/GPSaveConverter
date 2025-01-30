using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GPSaveConverter.Library
{
    internal class GameInfo
    {

        private static NLog.Logger logger = LogHelper.getClassLogger();


        internal bool NonUWPDataPopulated = false;
        private string name;

        public string Name { 
            get 
            {
                return this.name != null ? this.name : this.PackageName; 
            }
            set
            {
                this.name = value;
            }
        }

        [Browsable(false)]
        public string PackageName { get; set; }

        [Browsable(false), JsonIgnore]
        public string NonXboxSaveLocation
        {
            get
            {
                return this.expandSaveFileLocation();
            }
        }

        [Browsable(false)]
        public NonXboxProfile.ProfileType[] TargetProfileTypes { get; set; }

        [Browsable(false), JsonIgnore]
        internal NonXboxProfile[] TargetProfiles { get; set; }

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
                    } else if (IconLocation != null && File.Exists(IconLocation.Replace(".png", ".scale-200.png")))
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

        private string expandSaveFileLocation()
        {
            string returnVal = GameLibrary.ExpandSaveFileLocation(BaseNonXboxSaveLocation);


            if (this.TargetProfiles != null) foreach (NonXboxProfile p in this.TargetProfiles)
            {
                returnVal = p.ExpandSaveLocation(returnVal);
            }

            return returnVal;
        }

        private Image gameIcon;

        public GameInfo()
        {
            this.fileTranslations = new List<FileTranslation>();
        }

        internal async Task<NonXboxProfile[]> getProfileOptions(int index)
        {
            if (this.TargetProfiles == null || this.TargetProfiles.Length == 0) return Array.Empty<NonXboxProfile>();

            string baseLocation = GameLibrary.ExpandSaveFileLocation(BaseNonXboxSaveLocation);
            for(int j = 0; j < index; j++)
            {
                baseLocation = this.TargetProfiles[j].ExpandSaveLocation(baseLocation);
            }
            return await this.TargetProfiles[index].getProfileOptions(baseLocation);
        }

        internal void ApplyDeserializedInfo(GameInfo deserializedInfo)
        {
            this.BaseNonXboxSaveLocation = deserializedInfo.BaseNonXboxSaveLocation;

            foreach(FileTranslation t in deserializedInfo.fileTranslations)
            {
                if (!this.FileTranslations.Contains(t))
                {
                    this.FileTranslations.Add(t);
                }
            }
            this.WGSProfileSuffix = deserializedInfo.WGSProfileSuffix;

            if (deserializedInfo.TargetProfileTypes != null)
            {
                this.TargetProfileTypes = deserializedInfo.TargetProfileTypes;

                this.TargetProfiles = new NonXboxProfile[TargetProfileTypes.Length];

                for (int j = 0; j < TargetProfileTypes.Length; j++)
                {
                    TargetProfiles[j] = new NonXboxProfile(j, deserializedInfo.TargetProfileTypes[j]);
                }
            }
        }

        private FileTranslation findTranslation(NonXboxFileInfo file)
        {
            foreach (FileTranslation t in this.FileTranslations)
            {
                t.NonXboxFileInfo = file;
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
                t.XboxFileInfo = file;
                if (Regex.Match(file.ContainerName1, t.ContainerName1Regex).Success
                    && Regex.Match(file.ContainerName2, t.ContainerName2Regex).Success
                    && Regex.Match(file.FileID, t.XboxFileIDRegex).Success)
                {
                    return t;
                }
            }
            return null;
        }

        internal async Task fetchNonXboxSaveFiles()
        {
            string fetchLocation = this.NonXboxSaveLocation;

            if (Directory.Exists(fetchLocation)) await fetchNonXboxSaveFiles(fetchLocation, fetchLocation);

        }
        private async Task fetchNonXboxSaveFiles(string folder, string root)
        {
            foreach (string file in Directory.GetFiles(folder))
            {
                NonXboxFileInfo newInfo = await Task.Run(() => {
                    NonXboxFileInfo ni = new NonXboxFileInfo();
                    ni.FilePath = file;
                    ni.RelativePath = file.Replace(root, "");
                    ni.Timestamp = System.IO.File.GetLastWriteTime(file);
                    return ni;
                });
                GameLibrary.nonXboxFiles.Add(newInfo);
            }

            foreach (string dir in Directory.GetDirectories(folder))
            {
                await fetchNonXboxSaveFiles(dir, root);
            }
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
                //returnVal.RelativePath = t.replaceRegex(returnVal.RelativePath, true);

                NonXboxFileInfo match = null;
                foreach (NonXboxFileInfo fi in GameLibrary.nonXboxFiles)
                {
                    if (Regex.Match(fi.RelativePath, returnVal.RelativePath).Success)
                    {
                        match = fi;
                        break;
                    }
                }
                if(match != null)
                {
                    returnVal = match;
                }
                else
                {
                    if (createOrUpdate)
                    {
                        Regex r = new Regex(t.replaceRegex(returnVal.RelativePath));
                        returnVal.FilePath = Path.Combine(this.NonXboxSaveLocation, returnVal.RelativePath);
                        if(r.GetGroupNames().Length > 1)
                        {
                            throw new Exception("No substitution data found.");
                        }
                    }
                    else
                    {
                        returnVal = null;
                    }
                }

                if (createOrUpdate)
                {
                    logger.Info("Extracting Xbox save file: {0} -> {1}", file.FileID, returnVal.FilePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(returnVal.FilePath));
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
                string container1Name = t.replaceRegex(FileTranslation.ExactRegex(Regex.Replace(file.RelativePath, t.NonXboxFilenameRegex, t.ContainerName1)));
                string container2Name = t.replaceRegex(FileTranslation.ExactRegex(Regex.Replace(file.RelativePath, t.NonXboxFilenameRegex, t.ContainerName2)));
                
                
                IEnumerable<Xbox.XboxFileContainer> containers = index.Children.Where(c => Regex.Match(c.ContainerID[0], container1Name).Success && Regex.Match(c.ContainerID[1], container2Name).Success);

                if (containers.Count() > 1)
                {
                    throw new ArgumentException("Ambiguous Xbox container results");
                }
                else if (containers.Count() == 1)
                {
                    Xbox.XboxFileContainer xboxFileContainer = containers.First();

                    string xboxFileID = Regex.Escape(Regex.Replace(file.RelativePath, t.NonXboxFilenameRegex, t.XboxFileID));

                    matchedFile = xboxFileContainer.getFileList().Where(f => Regex.Match(f.FileID, FileTranslation.ExactRegex(t.replaceRegex(xboxFileID))).Success).FirstOrDefault();

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
