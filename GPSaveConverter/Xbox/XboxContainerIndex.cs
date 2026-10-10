using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;

namespace GPSaveConverter.Xbox
{
    internal class XboxContainerIndex
    {
        internal static IFileSystem FileSystem { get; set; } = new DefaultFileSystem();

        string packageName;
        string wgsFolder;
        internal string xboxProfileFolder;
        string indexPath;
        string containerPackageID;
        private string xboxProfileID;

        internal string XboxProfileID { get { return xboxProfileID; } }

        /// <summary>
        /// The game this index belongs to.
        /// </summary>
        internal string PackageName { get { return packageName; } }

        // Zero in every save seen.
        private uint unknown1;

        /// <summary>
        /// Whether the save has changes the cloud has not seen. One of the Index values of <see cref="XboxSyncState"/>.
        /// </summary>
        private uint syncState;

        // 0x10000000 in every save seen, which is 256 MB. Probably the room a game is given.
        private ulong unknown3;

        /// <summary>
        /// The ID the index gives the whole save: a GUID, as text. It is only ever written back, so it
        /// is kept exactly as it was read.
        /// </summary>
        private string indexID;

        internal XboxFileContainer[] Children { get; private set; }
        internal XboxContainerIndex(GameInfo info, string id)
        {
            this.packageName = info.PackageName;
            this.xboxProfileID = id;

            wgsFolder = XboxPackageList.getWGSFolder(packageName);
            string[] profileFolders = FileSystem.GetDirectories(wgsFolder, xboxProfileID + "_*" + (info.WGSProfileSuffix != null ? info.WGSProfileSuffix : ""));
            if (profileFolders.Length == 0)
            {
                throw new DirectoryNotFoundException("No Xbox save folder was found for profile " + xboxProfileID + ".");
            }

            // The Xbox app can leave several folders for one profile. Only one holds the containers.
            // The others have an index and nothing else, and opening one of those shows no files.
            xboxProfileFolder = profileFolders.FirstOrDefault(folder => FileSystem.GetDirectories(folder).Length > 0) ?? profileFolders[0];
            indexPath = Path.Combine(xboxProfileFolder, "containers.index");
            byte[] containerData = FileSystem.ReadAllBytes(indexPath);


            int currentByte = 4;

            int count = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            unknown1 = BitConverter.ToUInt32(containerData, currentByte);
            currentByte += 4;


            int nameLength = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            containerPackageID = Encoding.Unicode.GetString(containerData, currentByte, nameLength * 2);
            currentByte += (nameLength * 2);

            // Usually the package name, "!" and an app ID. Issue #113 met an index with no "!" in it.
            int appIDStart = containerPackageID.IndexOf('!');
            string indexPackageName = appIDStart < 0 ? containerPackageID : containerPackageID.Substring(0, appIDStart);
            if (indexPackageName != packageName)
            {
                throw new FileFormatException("Container Index package name mismatch.");
            }

            DateTime timestamp = DateTime.FromFileTimeUtc(BitConverter.ToInt64(containerData, currentByte));
            currentByte += 8;

            syncState = BitConverter.ToUInt32(containerData, currentByte);
            currentByte += 4;

            Children = new XboxFileContainer[count];
            int stringLength = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            this.indexID = Encoding.Unicode.GetString(containerData, currentByte, stringLength * 2);
            currentByte += stringLength * 2;

            
            unknown3 = BitConverter.ToUInt64(containerData, currentByte);
            currentByte += 8;
            for (int j = 0; j < count; j++)
            {
                string[] containerStrings = new string[3];
                for(int i = 0; i < containerStrings.Length; i++)
                {
                    stringLength = BitConverter.ToInt32(containerData, currentByte);
                    currentByte += 4;


                    containerStrings[i] = Encoding.Unicode.GetString(containerData, currentByte, stringLength * 2);
                    currentByte += stringLength * 2;
                }

                byte containerVersion = containerData[currentByte];
                currentByte++;

                uint containerSyncState = BitConverter.ToUInt32(containerData, currentByte);
                currentByte+= 4;

                byte[] tempGuidArray = new byte[XboxHelper.GuidLength];
                Array.Copy(containerData, currentByte, tempGuidArray, 0, XboxHelper.GuidLength);
                currentByte += XboxHelper.GuidLength;

                Guid containerGuid = new Guid(tempGuidArray);

                long containerTimestamp = BitConverter.ToInt64(containerData, currentByte);
                currentByte += 8;

                ulong containerUnknown2 = BitConverter.ToUInt64(containerData, currentByte);
                currentByte += 8;

                ulong containerSize = BitConverter.ToUInt64(containerData, currentByte);
                currentByte += 8;

                Children[j] = new XboxFileContainer(this
                                                  , containerGuid
                                                  , containerVersion
                                                  , containerStrings
                                                  , containerSyncState
                                                  , containerUnknown2
                                                  , containerTimestamp
                                                  , containerSize);

            }
        }

        internal XboxFileInfo[] getFileList()
        {
            List<XboxFileInfo> returnVal = new List<XboxFileInfo>();

            foreach(XboxFileInfo[] files in this.Children.Select(c => c.getFileList()).ToArray())
            {
                returnVal.AddRange(files);
            }

            return returnVal.ToArray();
        }

        /// <summary>
        /// How many containers the index lists that have no files on this PC. Their files are in
        /// nobody's list, which is worth telling the user.
        /// </summary>
        internal int ContainersNotOnDisk { get { return this.Children.Count(c => !c.IsOnDisk); } }

        /// <summary>
        /// Adds a new, empty container to the save. It is on disk at once. The index lists it once
        /// <see cref="UpdateIndex"/> has run.
        /// </summary>
        internal XboxFileContainer CreateContainer(string name1, string name2)
        {
            XboxFileContainer created = XboxFileContainer.Create(this, name1, name2);

            // The index is kept in order of name, capital letters first. A container the game makes
            // itself goes in at its place in that order, not at the end, and so does this one.
            List<XboxFileContainer> children = this.Children.ToList();
            int place = children.FindIndex(c => String.CompareOrdinal(c.ContainerID[0], name1) > 0);
            children.Insert(place < 0 ? children.Count : place, created);
            this.Children = children.ToArray();

            // Without this the Xbox services have no reason to look for something to upload.
            this.syncState = XboxSyncState.IndexModified;
            return created;
        }

        internal void UpdateIndex()
        {
            DateTime saveTime = DateTime.Now;

            // Put together in memory first. A failure part way must not leave half an index on disk.
            MemoryStream index = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(index, Encoding.Unicode);
            writer.Write(0x00000000E);

            writer.Write(this.Children.Length);

            writer.Write(unknown1);

            writer.Write(containerPackageID.Length);
            writer.Write(Encoding.Unicode.GetBytes(containerPackageID));

            writer.Write(saveTime.ToFileTime());

            writer.Write(syncState);

            writer.Write(indexID.Length);
            writer.Write(Encoding.Unicode.GetBytes(indexID));

            writer.Write(unknown3);

            foreach(XboxFileContainer container in Children)
            {
                foreach(string containerName in container.ContainerID)
                {
                    writer.Write(containerName.Length);
                    writer.Write(Encoding.Unicode.GetBytes(containerName));
                }
                writer.Write(container.ContainerVersion);
                writer.Write(container.SyncState);

                writer.Write(container.ContainerGuid.ToByteArray(), 0, XboxHelper.GuidLength);

                if (container.Changed && container.IsOnDisk)
                {
                    writer.Write(container.getModifiedTime().ToFileTime());
                    writer.Write(container.unknown2);
                    writer.Write(container.getSize());
                }
                else
                {
                    // Not touched, so what the index said of it is written back as it was. That goes
                    // for a container whose files are not on this PC too: a time and size worked out
                    // from a missing folder would tell the Xbox app the container is empty.
                    writer.Write(container.IndexModifiedTime);
                    writer.Write(container.unknown2);
                    writer.Write(container.IndexSize);
                }
            }
            writer.Flush();

            using (FileStream file = FileSystem.OpenWrite(this.indexPath))
            {
                file.Write(index.GetBuffer(), 0, (int)index.Length);

                // Opening for writing keeps what was in the file. Nothing may be left of it past the end.
                file.SetLength(index.Length);
            }
            FileSystem.SetFileLastWriteTime(this.indexPath, saveTime);

        }
    }
}
