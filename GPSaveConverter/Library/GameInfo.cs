using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter.Library
{
    internal class GameInfo
    {
        internal static IFileSystem FileSystem { get; set; } = new DefaultFileSystem();

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
                    if (FileSystem.FileExists(IconLocation))
                    {
                        gameIcon = Image.FromFile(IconLocation);
                    } else if (IconLocation != null && FileSystem.FileExists(IconLocation.Replace(".png", ".scale-200.png")))
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

        private FileTranslation findTranslation(NonXboxFileInfo file, string xboxProfileID)
        {
            if (file == null || file.RelativePath == null)
            {
                return null;
            }
            foreach (FileTranslation t in this.FileTranslations)
            {
                t.NonXboxFileInfo = file;
                t.XboxProfileID = xboxProfileID;
                if (t.NonXboxFilenameRegex != null && Regex.Match(file.RelativePath, t.NonXboxFilenameRegex).Success)
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
                t.XboxProfileID = file.Parent.Parent.XboxProfileID;
                if (Regex.Match(file.ContainerName1, t.ContainerName1Regex).Success
                    && Regex.Match(file.ContainerName2, t.ContainerName2Regex).Success
                    && Regex.Match(file.FileID, t.XboxFileIDRegex).Success)
                {
                    return t;
                }
            }
            return null;
        }

        internal async Task refreshNonXboxSaveFiles()
        {
            string fetchLocation = this.NonXboxSaveLocation;
            GameLibrary.nonXboxFiles.Clear();
            if (FileSystem.DirectoryExists(fetchLocation)) await fetchNonXboxSaveFiles(fetchLocation, fetchLocation);

        }
        private async Task fetchNonXboxSaveFiles(string folder, string root)
        {
            foreach (string file in FileSystem.GetFiles(folder))
            {
                NonXboxFileInfo newInfo = await Task.Run(() => {
                    NonXboxFileInfo ni = new NonXboxFileInfo();
                    ni.FilePath = file;
                    ni.RelativePath = file.Replace(root, "");
                    ni.Timestamp = FileSystem.GetFileLastWriteTime(file);
                    return ni;
                });
                GameLibrary.nonXboxFiles.Add(newInfo);
            }

            foreach (string dir in FileSystem.GetDirectories(folder))
            {
                await fetchNonXboxSaveFiles(dir, root);
            }
        }

        /// <param name="backup">If given, the non-Xbox file is kept in it as it is before it is written.</param>
        internal NonXboxFileInfo getNonXboxFileVersion(Xbox.XboxFileInfo file, bool createOrUpdate = false, SaveBackups.SaveBackup backup = null)
        {
            FileTranslation t = findTranslation(file);
            NonXboxFileInfo returnVal = null;

            if (t != null)
            {
                returnVal = new NonXboxFileInfo();

                bool pathComplete;
                returnVal.RelativePath = t.FillNonXboxFilename(file.ContainerName1, file.ContainerName2, file.FileID, t.XboxProfileID, out pathComplete);
                string pathAsWritten = t.NonXboxFilenameAsWritten(file.ContainerName1, file.ContainerName2, file.FileID);

                string existingPath = FileTranslation.FindNonXboxFile(GameLibrary.nonXboxFiles.Select(fi => fi.RelativePath), returnVal.RelativePath, pathAsWritten);
                NonXboxFileInfo match = existingPath == null ? null : GameLibrary.nonXboxFiles.First(fi => fi.RelativePath == existingPath);

                if(match != null)
                {
                    returnVal = match;
                }
                else
                {
                    if (createOrUpdate)
                    {
                        if (!pathComplete)
                        {
                            throw new Exception("No substitution data found.");
                        }
                        returnVal.FilePath = Path.Combine(this.NonXboxSaveLocation, returnVal.RelativePath);
                    }
                    else
                    {
                        returnVal = null;
                    }
                }

                if (createOrUpdate)
                {
                    logger.Info("Extracting Xbox save file: {0} -> {1}", file.FileID, returnVal.FilePath);
                    if (backup != null)
                    {
                        // Before the folder is made, so the backup can tell that it was not there.
                        backup.Preserve(returnVal.FilePath);
                    }
                    FileSystem.CreateDirectory(Path.GetDirectoryName(returnVal.FilePath));
                    FileSystem.CopyFile(file.getFilePath(), returnVal.FilePath, true);
                    returnVal.Timestamp = FileSystem.GetFileLastWriteTime(returnVal.FilePath);
                }
            } else if (createOrUpdate)
            {
                throw new Exception("Relative file path translation to Xbox file ID not found." + Environment.NewLine + Environment.NewLine + "You may need to add file translations via https://github.com/Fr33dan/GPSaveConverter/wiki/File-Translations");

            }
            return returnVal;
        }

        internal Xbox.XboxFileInfo getXboxFileVersion(Xbox.XboxContainerIndex index, NonXboxFileInfo file, bool createOrUpdate = false)
        {
            // No Xbox profile is open yet, so there is nothing to match against.
            if (index == null)
            {
                return null;
            }

            FileTranslation t = findTranslation(file, index.XboxProfileID);

            Xbox.XboxFileInfo matchedFile = null;
            if (t != null)
            {
                List<Xbox.XboxFileContainer> containers = t.FindContainers(index.Children, c => c.ContainerID[0], c => c.ContainerID[1], file.RelativePath, index.XboxProfileID);

                if (containers.Count > 1)
                {
                    throw new ArgumentException("Ambiguous Xbox container results");
                }
                else if (containers.Count == 1)
                {
                    Xbox.XboxFileContainer xboxFileContainer = containers[0];

                    string xboxFileID;
                    bool xboxFileIDComplete;
                    matchedFile = t.FindXboxFile(xboxFileContainer.getFileList(), f => f.FileID, file.RelativePath, index.XboxProfileID, out xboxFileID, out xboxFileIDComplete);

                    if (createOrUpdate)
                    {
                        if (matchedFile != null)
                        {
                            logger.Info("Replacing existing Xbox Save file: {0} -> {1} ({2})", file.FilePath, matchedFile.FileID, matchedFile.getFileName());
                            matchedFile.Replace(file);
                        }
                        else
                        {
                            if (!xboxFileIDComplete)
                            {
                                throw new Exception("No substitution data found.");
                            }
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
                throw new Exception("Relative file path translation to Xbox file ID not found." + Environment.NewLine + Environment.NewLine + "You may need to add file translations via https://github.com/Fr33dan/GPSaveConverter/wiki/File-Translations");
            }
            return matchedFile;
        }

        public override string ToString()
        {
            return this.Name == null ? this.PackageName : this.Name;
        }
    }
}
