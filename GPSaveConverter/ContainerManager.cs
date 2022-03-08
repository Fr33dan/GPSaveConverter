using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal class ContainerManager
    {
        public string PackageName;
        string wgsFolder;
        string wgsProfile;
        string saveFilePath;
        byte[] containersIndexData;
        int indexHeaderCount = 16;
        const int ContainerHeaderLength = 8;
        string containerPath;
        byte[] containerData;
        private List<XboxFileInfo> fileList;
        public ContainerManager(string packageName)
        {
            this.PackageName = packageName;
            initPaths(packageName);
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

        private void initPaths(string packageName)
        {
            wgsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", packageName, "SystemAppData", "wgs");
            wgsProfile = Directory.GetDirectories(wgsFolder).Where(s => s != "t").First();

            saveFilePath = Directory.GetDirectories(wgsProfile).First();
            containerPath = Directory.GetFiles(saveFilePath, "container.*").First();
        }

        public string getIndexText()
        {
            containersIndexData = File.ReadAllBytes(Path.Combine(wgsProfile, "containers.index"));
            string unicode = Encoding.Unicode.GetString(containersIndexData, indexHeaderCount, containersIndexData.Length - indexHeaderCount);
            return unicode;
        }

        private void parseContainer()
        {
            containerData = File.ReadAllBytes(containerPath);
            this.fileList = new List<XboxFileInfo>();
            for(int j = ContainerHeaderLength; j < containerData.Length;j += XboxFileInfo.EntryByteLength)
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

        public void AddFile(string relativePath, string sourceParent)
        {
            string filePath = Path.Combine(sourceParent, relativePath);
            XboxFileInfo existingFile = this.fileList.Where(i => i.GetRelativeFilePath() == relativePath).FirstOrDefault();
            if (existingFile != null)
            {
                File.Copy(filePath, existingFile.getFilePath(), true);
            }
            else
            {
                this.fileList.Add(new XboxFileInfo(this, filePath, relativePath));
                this.SaveContainer();
            }
        }
    }
}
