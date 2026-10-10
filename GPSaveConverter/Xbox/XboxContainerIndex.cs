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

        private uint unknown1;
        private uint unknown2;
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

            unknown2 = BitConverter.ToUInt32(containerData, currentByte);
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
                
                uint containerUnknown1 = BitConverter.ToUInt32(containerData, currentByte);
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
                                                  , containerUnknown1
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

            writer.Write(unknown2);

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
                writer.Write(container.unknown1);

                writer.Write(container.ContainerGuid.ToByteArray(), 0, XboxHelper.GuidLength);

                if (container.IsOnDisk)
                {
                    writer.Write(container.getModifiedTime().ToFileTime());
                    writer.Write(container.unknown2);
                    writer.Write(container.getSize());
                }
                else
                {
                    // There is nothing on this PC to measure. A time and size worked out from a missing
                    // folder would tell the Xbox app the container is empty, so what it said is kept.
                    writer.Write(container.IndexModifiedTime);
                    writer.Write(container.unknown2);
                    writer.Write(container.IndexSize);
                }
            }
            writer.Flush();

            using (FileStream file = FileSystem.OpenWrite(this.indexPath))
            {
                file.Write(index.GetBuffer(), 0, (int)index.Length);
            }
            FileSystem.SetFileLastWriteTime(this.indexPath, saveTime);

        }
    }
}
