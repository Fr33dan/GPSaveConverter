using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter.Xbox
{
    internal class XboxFileContainer
    {
        private XboxContainerIndex parent;
        internal string[] ContainerID { get; private set; }
        internal Guid ContainerGuid { get; private set; }
        string saveFilePath;
        const int ContainerHeaderLength = 8;
        string containerPath;
        byte[] containerData;
        private List<XboxFileInfo> fileList;
        public XboxFileContainer(XboxContainerIndex parent, Guid containerGuid, string[] containerID,DateTime timestamp)
        {
            this.parent = parent;
            this.ContainerGuid = containerGuid;
            this.ContainerID = containerID;
            initPaths();
        }

        internal string getSaveFilePath()
        {
            return saveFilePath;
        }

        public XboxFileInfo[] getFileList()
        {
            if(fileList == null)
            {
                parseContainer();
            }
            return fileList.ToArray();
        }

        private void initPaths()
        {
            saveFilePath = Path.Combine(parent.xboxProfileFolder, this.ContainerGuid.ToString().ToUpper().Replace("-",""));
            containerPath = Directory.GetFiles(saveFilePath, "container.*").First();
        }

        private void parseContainer()
        {
            containerData = File.ReadAllBytes(containerPath);
            this.fileList = new List<XboxFileInfo>();
            for(int j = ContainerHeaderLength; j < containerData.Length;j += XboxHelper.EntryByteLength)
            {
                fileList.Add(new XboxFileInfo(this,containerData, j));
            }

        }

        public void SaveContainer()
        {
            FileStream s = File.OpenWrite(containerPath);

            s.Write(BitConverter.GetBytes(4u),0,4);

            s.Write(BitConverter.GetBytes(this.fileList.Count), 0, 4);

            foreach(XboxFileInfo file in this.fileList)
            {
                file.Write(s);
            }
            s.Close();
        }

        public XboxFileInfo AddFile(NonXboxFileInfo info, string xboxFileID)
        {
            XboxFileInfo returnVal = new XboxFileInfo(this, info.FilePath, xboxFileID);
            this.fileList.Add(returnVal);
            this.SaveContainer();
            return returnVal;
        }
    }
}
