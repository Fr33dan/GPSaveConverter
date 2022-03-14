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

            // Skip unknown bytes
            currentByte += 4;


            int nameLength = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            string containerPackageID = Encoding.Unicode.GetString(containerData, currentByte, nameLength * 2);
            currentByte += (nameLength * 2);

            if(containerPackageID.Substring(0, containerPackageID.IndexOf('!')) != packageName)
            {
                throw new FileFormatException("Container Index package name mismatch.");
            }

            DateTime timestamp = DateTime.FromFileTimeUtc(BitConverter.ToInt64(containerData, currentByte));
            currentByte += 8;

            int unknownNumber = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            Children = new XboxFileContainer[count];
            int stringLength = BitConverter.ToInt32(containerData, currentByte);
            currentByte += 4;

            string indexGUID = Encoding.Unicode.GetString(containerData, currentByte, stringLength * 2);
            currentByte += stringLength * 2;

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
                
                currentByte+= 5;

                byte[] tempGuidArray = new byte[XboxHelper.GuidLength];
                Array.Copy(containerData, currentByte, tempGuidArray, 0, XboxHelper.GuidLength);
                currentByte += XboxHelper.GuidLength;

                Guid containerGuid = new Guid(tempGuidArray);

                DateTime containerTimestamp = DateTime.FromFileTime(BitConverter.ToInt64(containerData, currentByte));
                currentByte += 8;


                Children[j] = new XboxFileContainer(this, containerGuid, containerStrings, containerTimestamp);
                currentByte += 16;
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
    }
}
