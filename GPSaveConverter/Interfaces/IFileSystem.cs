namespace GPSaveConverter.Interfaces
{
    using System;
    using System.IO;

    /// <summary>
    /// Abstracts file and directory operations for testability.
    /// </summary>
    public interface IFileSystem
    {
        // File operations
        bool FileExists(string path);
        byte[] ReadAllBytes(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
        FileStream OpenRead(string path);
        FileStream OpenWrite(string path);
        void CopyFile(string sourceFileName, string destFileName, bool overwrite = false);
        DateTime GetFileLastWriteTime(string path);
        void SetFileLastWriteTime(string path, DateTime lastWriteTime);

        // Directory operations
        bool DirectoryExists(string path);
        string[] GetDirectories(string path);
        string[] GetDirectories(string path, string searchPattern);
        string[] GetFiles(string path);
        DirectoryInfo CreateDirectory(string path);
    }
}
