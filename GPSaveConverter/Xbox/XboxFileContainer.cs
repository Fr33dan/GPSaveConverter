using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter.Xbox
{
    internal class XboxFileContainer
    {
        internal static IFileSystem FileSystem { get; set; } = new DefaultFileSystem();
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();

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

        /// <summary>
        /// The modified time the index gave the container, as it was written there.
        /// </summary>
        internal long IndexModifiedTime { get; private set; }

        /// <summary>
        /// The size the index gave the container.
        /// </summary>
        internal ulong IndexSize { get; private set; }

        /// <summary>
        /// False for a container the index lists but whose files are not on this PC: the Xbox app
        /// knows of it and has not put it here. Nothing can be read from it, and nothing is added to it.
        /// </summary>
        internal bool IsOnDisk { get { return FileSystem.FileExists(containerPath); } }

        public XboxFileContainer(XboxContainerIndex parent
                               , Guid containerGuid
                               , byte containerVer
                               , string[] containerID
                               , uint u1
                               , ulong u2
                               , long indexModifiedTime
                               , ulong indexSize)
        {
            this.parent = parent;
            this.ContainerGuid = containerGuid;
            this.ContainerID = containerID;
            this.containerVersion = containerVer;

            this.unknown1 = u1;
            this.unknown2 = u2;
            this.IndexModifiedTime = indexModifiedTime;
            this.IndexSize = indexSize;


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
            foreach(XboxFileInfo f in getFileList())
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
            this.fileList = new List<XboxFileInfo>();
            if (!IsOnDisk)
            {
                // Shown as empty. Failing here would make every other container unusable as well.
                logger.Debug("Xbox container {0} is listed but its files are not on this PC ({1})", ContainerID[0], containerPath);
                return;
            }

            containerData = FileSystem.ReadAllBytes(containerPath);
            for(int j = ContainerHeaderLength; j < containerData.Length;j += XboxHelper.EntryByteLength)
            {
                fileList.Add(new XboxFileInfo(this,containerData, j));
            }

        }

        public void SaveContainer()
        {
            FileStream s = FileSystem.OpenWrite(containerPath);

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
            if (!IsOnDisk)
            {
                throw new InvalidOperationException("The Xbox save lists the container " + ContainerID[0] + ", but its files are not on this PC, so nothing can be added to it.");
            }

            XboxFileInfo returnVal = new XboxFileInfo(this, info.FilePath, xboxFileID);
            this.fileList.Add(returnVal);
            this.SaveContainer();
            return returnVal;
        }
    }
}
