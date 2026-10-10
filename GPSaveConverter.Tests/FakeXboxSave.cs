using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// Builds a throwaway Xbox save on disk in the layout the application reads: a profile folder
    /// holding containers.index, plus one folder per container with its container file and blobs.
    /// It also holds a non-Xbox save folder. The real transfer code can then be run end to end.
    /// The byte layout written here is the one XboxContainerIndex and XboxFileContainer parse.
    /// </summary>
    internal sealed class FakeXboxSave : IDisposable
    {
        internal const string PackageName = "Test.Game_abc123";
        internal const string ProfileID = "0009000000000001";

        private const int FileIDByteLength = 128;
        private const int EntryByteLength = FileIDByteLength + 16 + 16;
        private const byte ContainerVersion = 1;

        // The save is written the way the Xbox app leaves one it has just synced. The values are the
        // ones in a DOOM Eternal save looked at on 2026-10-10: the save marked 3, each container
        // marked 1 and carrying the mark the cloud gave it.
        internal const uint IndexSynced = 3;
        internal const uint ContainerSynced = 1;
        internal const string SyncedTag = "\"0x8DF26F0B0826728\"";
        internal const ulong IndexFooter = 0x10000000;

        private class Blob
        {
            public string FileID;
            public Guid FileCode = Guid.NewGuid();
        }

        private class Container
        {
            public string Name1;
            public string Name2;
            public Guid Guid = Guid.NewGuid();
            public List<Blob> Blobs = new List<Blob>();

            // What the index says about the container. A container that is on disk has version 1.
            public bool OnDisk = true;
            public byte Version = ContainerVersion;
            public long IndexTime = DateTime.Now.ToFileTime();
            public ulong IndexSize;
        }

        private readonly string root;
        private readonly List<Container> containers = new List<Container>();

        // Folder names are kept short. A blob's full path is two 32-character names below the profile
        // folder, and the test host does not handle paths past 260 characters.

        /// <summary>Stands in for %LOCALAPPDATA%.</summary>
        internal string LocalAppData { get { return Path.Combine(root, "L"); } }

        /// <summary>The non-Xbox save folder, with a trailing separator as the application stores it.</summary>
        internal string NonXboxFolder { get { return Path.Combine(root, "N") + "\\"; } }

        /// <summary>A folder for backups, outside both save folders.</summary>
        internal string BackupFolder { get { return Path.Combine(root, "B"); } }

        /// <summary>
        /// Where the non-Xbox save folders of a game that keeps one per profile go, with a trailing
        /// separator. A save location for such a game is this followed by "&lt;user-id&gt;\".
        /// </summary>
        internal string NonXboxProfilesFolder { get { return Path.Combine(root, "P") + "\\"; } }

        internal string ProfileFolder
        {
            get { return Path.Combine(LocalAppData, "Packages", PackageName, "SystemAppData", "wgs", ProfileID + "_0001"); }
        }

        /// <summary>
        /// A second folder of the same profile, of the kind the Xbox app leaves behind: an index and
        /// nothing else. Its name sorts before <see cref="ProfileFolder"/>, so it is the one found first.
        /// </summary>
        internal string LeftoverProfileFolder
        {
            get { return Path.Combine(LocalAppData, "Packages", PackageName, "SystemAppData", "wgs", ProfileID + "_0000"); }
        }

        /// <summary>
        /// What the index says the package is. The Xbox app writes the package name, "!" and an app ID.
        /// </summary>
        internal string IndexPackageID { get; set; } = PackageName + "!App";

        /// <summary>
        /// The ID the index gives the whole save. The Xbox app writes a GUID here.
        /// </summary>
        internal string IndexID { get; set; } = Guid.NewGuid().ToString();

        internal FakeXboxSave()
        {
            root = Path.Combine(Path.GetTempPath(), "gpsc", Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ProfileFolder);
            Directory.CreateDirectory(NonXboxFolder);
        }

        /// <summary>
        /// Adds <see cref="LeftoverProfileFolder"/>, holding an index that lists no containers.
        /// </summary>
        /// <param name="indexPackageID">What that index says the package is.</param>
        internal FakeXboxSave WithLeftoverProfileFolder(string indexPackageID)
        {
            Directory.CreateDirectory(LeftoverProfileFolder);
            using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(LeftoverProfileFolder, "containers.index"))))
            {
                WriteIndexHeader(writer, 0, indexPackageID, Guid.NewGuid().ToString());
            }
            return this;
        }

        /// <summary>
        /// Adds a container that the index lists but that has no folder on disk, as happens with a
        /// container the Xbox app knows of and has not downloaded. The index gives it version 0.
        /// </summary>
        internal FakeXboxSave WithContainerNotOnDisk(string containerName1, string containerName2)
        {
            containers.Add(new Container
            {
                Name1 = containerName1,
                Name2 = containerName2,
                OnDisk = false,
                Version = 0,
                IndexTime = new DateTime(2023, 4, 4, 12, 18, 44, DateTimeKind.Utc).ToFileTimeUtc(),
                IndexSize = 123456
            });
            return this;
        }

        /// <summary>
        /// Removes the folder that holds the save, leaving whatever else was added.
        /// </summary>
        internal FakeXboxSave WithoutProfileFolder()
        {
            Directory.Delete(ProfileFolder, true);
            return this;
        }

        internal FakeXboxSave WithXboxFile(string containerName1, string containerName2, string fileID, string content)
        {
            Container container = containers.FirstOrDefault(c => c.Name1 == containerName1 && c.Name2 == containerName2);
            if (container == null)
            {
                container = new Container { Name1 = containerName1, Name2 = containerName2 };
                containers.Add(container);
            }

            Blob blob = new Blob { FileID = fileID };
            container.Blobs.Add(blob);

            Directory.CreateDirectory(ContainerFolder(container));
            string blobFile = Path.Combine(ContainerFolder(container), FolderName(blob.FileCode));
            File.WriteAllText(blobFile, content);

            // The index gives each container the total size of its blobs.
            container.IndexSize += (ulong)new FileInfo(blobFile).Length;
            return this;
        }

        internal FakeXboxSave WithNonXboxFile(string relativePath, string content)
        {
            string path = Path.Combine(NonXboxFolder, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
            return this;
        }

        /// <summary>
        /// Adds an empty save folder for one profile under <see cref="NonXboxProfilesFolder"/>.
        /// </summary>
        /// <returns>The folder, with a trailing separator.</returns>
        internal string AddNonXboxProfile(string profileID)
        {
            string folder = Path.Combine(NonXboxProfilesFolder, profileID) + "\\";
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Writes containers.index and each container file. Call once, after adding the Xbox files.
        /// </summary>
        internal FakeXboxSave Build()
        {
            foreach (Container container in containers.Where(c => c.OnDisk))
            {
                using (BinaryWriter writer = new BinaryWriter(File.Create(ContainerFile(container))))
                {
                    writer.Write(4u);
                    writer.Write(container.Blobs.Count);
                    foreach (Blob blob in container.Blobs)
                    {
                        byte[] name = new byte[FileIDByteLength];
                        Encoding.Unicode.GetBytes(blob.FileID, 0, blob.FileID.Length, name, 0);
                        writer.Write(name);
                        // Once a blob is uploaded, the copy the cloud has and the one on disk are the same.
                        writer.Write(blob.FileCode.ToByteArray());
                        writer.Write(blob.FileCode.ToByteArray());
                    }
                }
            }

            using (BinaryWriter writer = new BinaryWriter(File.Create(Path.Combine(ProfileFolder, "containers.index"))))
            {
                WriteIndexHeader(writer, containers.Count, IndexPackageID, IndexID);

                foreach (Container container in containers)
                {
                    WriteString(writer, container.Name1);
                    WriteString(writer, container.Name2);
                    WriteString(writer, SyncedTag);
                    writer.Write(container.Version);
                    writer.Write(container.OnDisk ? ContainerSynced : 0u);
                    writer.Write(container.Guid.ToByteArray());
                    writer.Write(container.IndexTime);
                    writer.Write(0ul);
                    writer.Write(container.IndexSize);
                }
            }
            return this;
        }

        /// <summary>
        /// Reads back, without the application's parser, what the index says about one container.
        /// </summary>
        /// <returns>Its version, modified time and size as "version time size".</returns>
        internal string ReadIndexEntry(string containerName1, string containerName2)
        {
            byte[] data = File.ReadAllBytes(Path.Combine(ProfileFolder, "containers.index"));
            int count = BitConverter.ToInt32(data, 4);
            int position = 12;
            ReadString(data, ref position);
            position += 8 + 4;
            ReadString(data, ref position);
            position += 8;

            for (int entry = 0; entry < count; entry++)
            {
                string name1 = ReadString(data, ref position);
                string name2 = ReadString(data, ref position);
                ReadString(data, ref position);
                byte version = data[position];
                long time = BitConverter.ToInt64(data, position + 1 + 4 + 16);
                ulong size = BitConverter.ToUInt64(data, position + 1 + 4 + 16 + 8 + 8);
                position += 1 + 4 + 16 + 8 + 8 + 8;

                if (name1 == containerName1 && name2 == containerName2)
                {
                    return version + " " + time + " " + size;
                }
            }
            return null;
        }

        /// <summary>One container as the index describes it.</summary>
        internal class IndexEntry
        {
            public string Name1;
            public string Name2;
            public string CloudTag;
            public byte Number;
            public uint SyncState;
            public Guid Folder;
            public long Time;
            public ulong Size;
        }

        /// <summary>One blob as a container file describes it.</summary>
        internal class BlobEntry
        {
            public string FileID;
            public Guid CloudCopy;
            public Guid LocalCopy;

            /// <summary>The text of the file on disk, or null if it is not there.</summary>
            public string Content;
        }

        /// <summary>
        /// Reads back, without the application's parser, every container the index lists, in the
        /// order it lists them.
        /// </summary>
        internal List<IndexEntry> ReadIndexEntries()
        {
            byte[] data = File.ReadAllBytes(Path.Combine(ProfileFolder, "containers.index"));
            int count = BitConverter.ToInt32(data, 4);
            int position = 12;
            ReadString(data, ref position);
            position += 8 + 4;
            ReadString(data, ref position);
            position += 8;

            List<IndexEntry> entries = new List<IndexEntry>();
            for (int entry = 0; entry < count; entry++)
            {
                IndexEntry read = new IndexEntry();
                read.Name1 = ReadString(data, ref position);
                read.Name2 = ReadString(data, ref position);
                read.CloudTag = ReadString(data, ref position);
                read.Number = data[position];
                read.SyncState = BitConverter.ToUInt32(data, position + 1);
                byte[] folder = new byte[16];
                Array.Copy(data, position + 1 + 4, folder, 0, 16);
                read.Folder = new Guid(folder);
                read.Time = BitConverter.ToInt64(data, position + 1 + 4 + 16);
                read.Size = BitConverter.ToUInt64(data, position + 1 + 4 + 16 + 8 + 8);
                position += 1 + 4 + 16 + 8 + 8 + 8;
                entries.Add(read);
            }
            return entries;
        }

        /// <summary>
        /// Reads back what the index says of the whole save: whether it has changes the cloud has not seen.
        /// </summary>
        internal uint ReadIndexSyncState()
        {
            byte[] data = File.ReadAllBytes(Path.Combine(ProfileFolder, "containers.index"));
            int position = 12;
            ReadString(data, ref position);
            return BitConverter.ToUInt32(data, position + 8);
        }

        /// <summary>
        /// Reads back the last 8 bytes of the index header, which are the same in every save.
        /// </summary>
        internal ulong ReadIndexFooter()
        {
            byte[] data = File.ReadAllBytes(Path.Combine(ProfileFolder, "containers.index"));
            int position = 12;
            ReadString(data, ref position);
            position += 8 + 4;
            ReadString(data, ref position);
            return BitConverter.ToUInt64(data, position);
        }

        /// <summary>
        /// Reads a container file of any container the index lists, straight from disk.
        /// </summary>
        internal List<BlobEntry> ReadContainerFile(IndexEntry container)
        {
            string folder = Path.Combine(ProfileFolder, FolderName(container.Folder));
            byte[] data = File.ReadAllBytes(Path.Combine(folder, "container." + container.Number));
            int count = BitConverter.ToInt32(data, 4);

            List<BlobEntry> blobs = new List<BlobEntry>();
            for (int entry = 0; entry < count; entry++)
            {
                int offset = 8 + entry * EntryByteLength;
                byte[] cloud = new byte[16];
                byte[] local = new byte[16];
                Array.Copy(data, offset + FileIDByteLength, cloud, 0, 16);
                Array.Copy(data, offset + FileIDByteLength + 16, local, 0, 16);

                BlobEntry blob = new BlobEntry
                {
                    FileID = Encoding.Unicode.GetString(data, offset, FileIDByteLength).TrimEnd('\0'),
                    CloudCopy = new Guid(cloud),
                    LocalCopy = new Guid(local)
                };
                string blobFile = Path.Combine(folder, FolderName(blob.LocalCopy));
                blob.Content = File.Exists(blobFile) ? File.ReadAllText(blobFile) : null;
                blobs.Add(blob);
            }
            return blobs;
        }

        /// <summary>
        /// Reads back the ID the index of <see cref="ProfileFolder"/> gives the whole save.
        /// </summary>
        internal string ReadIndexID()
        {
            byte[] data = File.ReadAllBytes(Path.Combine(ProfileFolder, "containers.index"));
            int position = 12;
            ReadString(data, ref position);
            position += 8 + 4;
            return ReadString(data, ref position);
        }

        /// <summary>
        /// Tells whether a container has a folder on disk.
        /// </summary>
        internal bool ContainerFolderExists(string containerName1, string containerName2)
        {
            return Directory.Exists(ContainerFolder(containers.First(c => c.Name1 == containerName1 && c.Name2 == containerName2)));
        }

        private static string ReadString(byte[] data, ref int position)
        {
            int length = BitConverter.ToInt32(data, position);
            string value = Encoding.Unicode.GetString(data, position + 4, length * 2);
            position += 4 + length * 2;
            return value;
        }

        /// <summary>
        /// Reads a container file straight from disk, without the application's parser.
        /// </summary>
        /// <returns>Each blob's ID and the text of the file it points at, in container order.</returns>
        internal List<KeyValuePair<string, string>> ReadContainer(string containerName1, string containerName2)
        {
            Container container = containers.First(c => c.Name1 == containerName1 && c.Name2 == containerName2);
            byte[] data = File.ReadAllBytes(ContainerFile(container));
            int count = BitConverter.ToInt32(data, 4);

            List<KeyValuePair<string, string>> blobs = new List<KeyValuePair<string, string>>();
            for (int entry = 0; entry < count; entry++)
            {
                int offset = 8 + entry * EntryByteLength;
                string fileID = Encoding.Unicode.GetString(data, offset, FileIDByteLength).TrimEnd('\0');

                byte[] code = new byte[16];
                Array.Copy(data, offset + FileIDByteLength + 16, code, 0, 16);
                string blobFile = Path.Combine(ContainerFolder(container), FolderName(new Guid(code)));

                blobs.Add(new KeyValuePair<string, string>(fileID, File.Exists(blobFile) ? File.ReadAllText(blobFile) : null));
            }
            return blobs;
        }

        internal string ReadNonXboxFile(string relativePath)
        {
            string path = Path.Combine(NonXboxFolder, relativePath);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        private string ContainerFolder(Container container)
        {
            return Path.Combine(ProfileFolder, FolderName(container.Guid));
        }

        private string ContainerFile(Container container)
        {
            return Path.Combine(ContainerFolder(container), "container." + ContainerVersion);
        }

        private static string FolderName(Guid guid)
        {
            return guid.ToString().ToUpper().Replace("-", "");
        }

        /// <summary>
        /// Reads back what the index of <see cref="ProfileFolder"/> says the package is.
        /// </summary>
        internal string ReadIndexPackageID()
        {
            byte[] data = File.ReadAllBytes(Path.Combine(ProfileFolder, "containers.index"));
            return Encoding.Unicode.GetString(data, 16, BitConverter.ToInt32(data, 12) * 2);
        }

        private static void WriteIndexHeader(BinaryWriter writer, int containerCount, string packageID, string indexID)
        {
            writer.Write(0x0000000E);
            writer.Write(containerCount);
            writer.Write(0u);
            WriteString(writer, packageID);
            writer.Write(DateTime.Now.ToFileTime());
            writer.Write(IndexSynced);
            WriteString(writer, indexID);
            writer.Write(IndexFooter);
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            writer.Write(value.Length);
            writer.Write(Encoding.Unicode.GetBytes(value));
        }
    }
}
