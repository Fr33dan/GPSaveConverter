namespace GPSaveConverter.Interfaces
{
    using System;
    using System.IO;

    /// <summary>
    /// Default implementation that delegates to System.IO.
    /// </summary>
    public class DefaultFileSystem : IFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);

        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

        public FileStream OpenRead(string path) => File.OpenRead(path);

        public FileStream OpenWrite(string path) => File.OpenWrite(path);

        public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) =>
            File.Copy(sourceFileName, destFileName, overwrite);

        public DateTime GetFileLastWriteTime(string path) => File.GetLastWriteTime(path);

        public void SetFileLastWriteTime(string path, DateTime lastWriteTime) =>
            File.SetLastWriteTime(path, lastWriteTime);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string[] GetDirectories(string path) => Directory.GetDirectories(path);

        public string[] GetDirectories(string path, string searchPattern) =>
            Directory.GetDirectories(path, searchPattern);

        public string[] GetFiles(string path) => Directory.GetFiles(path);

        public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);
    }
}
