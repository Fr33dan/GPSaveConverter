using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GPSaveConverter.Library;

namespace GPSaveConverter.Xbox
{
    internal class XboxContainerIndex
    {
        string packageName;
        string wgsFolder;
        internal string xboxProfileFolder;
        string indexPath;
        string containerPackageID;

        private uint unknown1;
        private uint unknown2;
        private ulong unknown3;

        private Guid containerGuid;

        internal XboxFileContainer[] Children { get; private set; }
        internal XboxContainerIndex(GameInfo info, string xboxProfileID)
        {
            this.packageName = info.PackageName;

            wgsFolder = XboxPackageList.getWGSFolder(packageName);
            xboxProfileFolder = Directory.GetDirectories(wgsFolder, xboxProfileID + "_*" + (info.WGSProfileSuffix != null ? info.WGSProfileSuffix : "")).First();
            indexPath = Path.Combine(xboxProfileFolder, "containers.index");
            byte[] containerData = File.ReadAllBytes(indexPath);


            int currentByte = 4;

            int count = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            unknown1 = BitConverter.ToUInt32(containerData, currentByte);
            currentByte += 4;


            int nameLength = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            containerPackageID = Encoding.Unicode.GetString(containerData, currentByte, nameLength * 2);
            currentByte += (nameLength * 2);

            if(containerPackageID.Substring(0, containerPackageID.IndexOf('!')) != packageName)
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

            string indexGUID = Encoding.Unicode.GetString(containerData, currentByte, stringLength * 2);
            this.containerGuid = Guid.Parse(indexGUID);
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

                DateTime containerTimestamp = DateTime.FromFileTime(BitConverter.ToInt64(containerData, currentByte));
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
                                                  , containerUnknown2);
                
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

        internal void UpdateIndex()
        {
            DateTime saveTime = DateTime.Now;
            BinaryWriter writer = new BinaryWriter(File.OpenWrite(this.indexPath), Encoding.Unicode);
            writer.Write(0x00000000E);

            writer.Write(this.Children.Length);
            
            writer.Write(unknown1);

            writer.Write(containerPackageID.Length);
            writer.Write(Encoding.Unicode.GetBytes(containerPackageID));

            writer.Write(saveTime.ToFileTime());

            writer.Write(unknown2);

            string guidString = containerGuid.ToString();
            writer.Write(guidString.Length);
            writer.Write(Encoding.Unicode.GetBytes(guidString));

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

                writer.BaseStream.Write(container.ContainerGuid.ToByteArray(), 0, XboxHelper.GuidLength);

                writer.Write(container.getModifiedTime().ToFileTime());
                writer.Write(container.unknown2);
                writer.Write(container.getSize());
            }
            writer.Flush();
            writer.Close();
            File.SetLastWriteTime(this.indexPath, saveTime);

        }
    }
}
