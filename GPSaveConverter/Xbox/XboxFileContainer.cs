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

        internal XboxContainerIndex Parent { get { return parent; } }
        internal string[] ContainerID { get; private set; }
        internal Guid ContainerGuid { get; private set; }
        public byte ContainerVersion { get => containerVersion; }

        string saveFilePath;
        const int ContainerHeaderLength = 8;
        private string containerPath;
        private byte[] containerData;
        private List<XboxFileInfo> fileList;
        private byte containerVersion;
        internal uint unknown1;
        internal ulong unknown2;
        public XboxFileContainer(XboxContainerIndex parent
                               , Guid containerGuid
                               , byte containerVer
                               , string[] containerID
                               , uint u1
                               , ulong u2)
        {
            this.parent = parent;
            this.ContainerGuid = containerGuid;
            this.ContainerID = containerID;
            this.containerVersion = containerVer;

            this.unknown1 = u1;
            this.unknown2 = u2;


            initPaths();
        }

        internal DateTime getModifiedTime()
        {
            FileInfo fileInfo = new FileInfo(saveFilePath);
            return fileInfo.LastWriteTime;
        }

        internal long getSize()
        {
            long returnVal = 0;
            foreach(XboxFileInfo f in fileList)
            {
                FileInfo fi = new FileInfo(f.getFilePath());
                returnVal += fi.Length;
            }
            return returnVal;
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
            containerPath = Path.Combine(saveFilePath, String.Format("container.{0}", this.containerVersion));
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
