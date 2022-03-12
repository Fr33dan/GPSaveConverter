using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GPSaveConverter.Xbox
{
    internal class XboxContainerIndex
    {
        string packageName;

        internal XboxFileContainer[] Children { get; private set; }
        internal XboxContainerIndex(string packName, string xboxProfileID)
        {
            this.packageName = packName;

            string wgsFolder = XboxPackageList.getWGSFolder(packageName);
            string xboxProfileFolder = Directory.GetDirectories(wgsFolder, xboxProfileID + "_*").First();
            string indexPath = Path.Combine(wgsFolder, xboxProfileFolder, "containers.index");
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
                
                string folderName = XboxHelper.DecodeFileName(containerData, currentByte);
                currentByte += XboxHelper.FileNameDataLength;

                DateTime containerTimestamp = DateTime.FromFileTime(BitConverter.ToInt64(containerData, currentByte));
                currentByte += 8;


                Children[j] = new XboxFileContainer(packageName, containerStrings, folderName, containerTimestamp);
                currentByte += 16;
            }
        }
    }
}
