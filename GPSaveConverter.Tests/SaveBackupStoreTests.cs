using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using GPSaveConverter.SaveBackups;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// Works on real folders under the temp directory. A backup is only worth something if the bytes
    /// on disk come back, so nothing here is faked.
    /// </summary>
    public class SaveBackupStoreTests : IDisposable
    {
        private const string Package = "Test.Game_abc123";

        private readonly string root = Path.Combine(Path.GetTempPath(), "gpsc", Guid.NewGuid().ToString("N").Substring(0, 8));
        private readonly SaveBackupStore store;
        private DateTime clock = new DateTime(2026, 10, 9, 21, 0, 0);

        /// <summary>The non-Xbox save folder, with a trailing separator as the application stores it.</summary>
        private string SaveFolder { get { return Path.Combine(root, "N") + "\\"; } }

        /// <summary>An Xbox profile folder, laid out as the Xbox app does.</summary>
        private string ProfileFolder { get { return ProfileFolderOf(Package); } }

        private string ProfileFolderOf(string package)
        {
            return Path.Combine(root, "Packages", package, "SystemAppData", "wgs", "0009000000000001_0001");
        }

        public SaveBackupStoreTests()
        {
            Directory.CreateDirectory(SaveFolder);
            Directory.CreateDirectory(ProfileFolder);

            store = new SaveBackupStore(Path.Combine(root, "B"));
            // Each backup is dated a minute after the one before, so their order is known.
            store.Now = () => clock = clock.AddMinutes(1);
        }

        public void Dispose()
        {
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(root, true);
        }

        private static void Write(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }

        private void WriteSave(string relativePath, string content)
        {
            Write(Path.Combine(SaveFolder, relativePath), content);
        }

        private string ReadSave(string relativePath)
        {
            string path = Path.Combine(SaveFolder, relativePath);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private void WriteXbox(string relativePath, string content)
        {
            Write(Path.Combine(ProfileFolder, relativePath), content);
        }

        private SaveBackup StartBackup()
        {
            return store.StartNonXboxBackup(Package, "Test Game", SaveFolder, "copying files from Xbox");
        }

        /// <summary>Does to one file what a transfer does: keeps it, then writes it.</summary>
        private void Transfer(SaveBackup backup, string relativePath, string content)
        {
            backup.Preserve(Path.Combine(SaveFolder, relativePath));
            WriteSave(relativePath, content);
        }

        /// <summary>The only backup in the store, read back from disk.</summary>
        private SaveBackup Stored()
        {
            return Assert.Single(store.List(Package));
        }

        private void BuildXboxSave()
        {
            WriteXbox("containers.index", "index v1");
            WriteXbox("AAAA\\container.1", "container A v1");
            WriteXbox("AAAA\\BLOB1", "progress");
            WriteXbox("AAAA\\BLOB2", "settings");
            WriteXbox("BBBB\\container.1", "container B v1");
            WriteXbox("BBBB\\BLOB3", "world");
        }

        /// <summary>What copying to Xbox does: a blob replaced in place, one added, the container and the index rewritten.</summary>
        private void TransferToXbox()
        {
            WriteXbox("AAAA\\BLOB1", "steam progress");
            WriteXbox("AAAA\\BLOB9", "steam extra file");
            WriteXbox("AAAA\\container.1", "container A v2");
            WriteXbox("containers.index", "index v2");
        }

        private SaveBackup BackUpXbox(string madeBefore = "copying 2 files to Xbox")
        {
            return store.BackUpXboxSave(Package, "Test Game", ProfileFolder, madeBefore);
        }

        private const string OtherPackage = "Other.Game_xyz789";

        private SaveBackup BackUpOtherGame()
        {
            Write(Path.Combine(ProfileFolderOf(OtherPackage), "containers.index"), "other index");
            return store.BackUpXboxSave(OtherPackage, "Other Game", ProfileFolderOf(OtherPackage), "copying 1 file to Xbox");
        }

        #region Non-Xbox files

        [Fact]
        public void Restore_FileThatWasReplaced_GetsItsOldContentBack()
        {
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "xbox progress");
            backup.Complete();

            store.Restore(Stored());

            Assert.Equal("steam progress", ReadSave("save.dat"));
        }

        [Fact]
        public void Restore_FileTheTransferAdded_IsRemoved()
        {
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "extra.dat", "xbox progress");
            backup.Complete();

            store.Restore(Stored());

            Assert.Null(ReadSave("extra.dat"));
            Assert.Equal("steam progress", ReadSave("save.dat"));
        }

        [Fact]
        public void Restore_FilesTheTransferDidNotTouch_AreLeftAsTheyAreNow()
        {
            WriteSave("save.dat", "steam progress");
            WriteSave("other.dat", "untouched");
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "xbox progress");
            backup.Complete();
            WriteSave("other.dat", "changed by the game since");
            WriteSave("later.dat", "made by the game since");

            store.Restore(Stored());

            Assert.Equal("changed by the game since", ReadSave("other.dat"));
            Assert.Equal("made by the game since", ReadSave("later.dat"));
        }

        [Fact]
        public void Restore_FoldersMadeForANewFile_AreRemovedWithIt()
        {
            SaveBackup backup = StartBackup();
            Transfer(backup, "SLOT_3\\maps\\world.sav", "xbox world");
            backup.Complete();

            store.Restore(Stored());

            Assert.False(Directory.Exists(Path.Combine(SaveFolder, "SLOT_3")));
        }

        [Fact]
        public void Restore_FolderThatWasAlreadyThere_IsKeptEvenIfEmpty()
        {
            Directory.CreateDirectory(Path.Combine(SaveFolder, "SLOT_0"));
            SaveBackup backup = StartBackup();
            Transfer(backup, "SLOT_0\\maps\\world.sav", "xbox world");
            backup.Complete();

            store.Restore(Stored());

            Assert.True(Directory.Exists(Path.Combine(SaveFolder, "SLOT_0")));
            Assert.False(Directory.Exists(Path.Combine(SaveFolder, "SLOT_0", "maps")));
        }

        [Fact]
        public void Restore_NewFolderSomethingElseWasPutInSince_IsKept()
        {
            SaveBackup backup = StartBackup();
            Transfer(backup, "SLOT_3\\world.sav", "xbox world");
            backup.Complete();
            WriteSave("SLOT_3\\autosave.sav", "made by the game since");

            store.Restore(Stored());

            Assert.Null(ReadSave("SLOT_3\\world.sav"));
            Assert.Equal("made by the game since", ReadSave("SLOT_3\\autosave.sav"));
        }

        [Fact]
        public void Preserve_SamePathAgain_KeepsWhatWasThereFirst()
        {
            // Two Xbox files can map to one path, and a failed copy can be retried.
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "first xbox file");
            Transfer(backup, "save.dat", "second xbox file");
            backup.Complete();

            store.Restore(Stored());

            Assert.Equal("steam progress", ReadSave("save.dat"));
        }

        [Fact]
        public void Preserve_SamePathInAnotherCase_IsTheSameFile()
        {
            WriteSave("Save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "Save.dat", "first xbox file");
            Transfer(backup, "SAVE.DAT", "second xbox file");
            backup.Complete();

            Assert.Single(Stored().Files);
            store.Restore(Stored());
            Assert.Equal("steam progress", ReadSave("save.dat"));
        }

        [Fact]
        public void Preserve_FileOutsideTheSaveFolder_IsRefused()
        {
            Write(Path.Combine(root, "elsewhere.dat"), "not a save");
            SaveBackup backup = StartBackup();

            Exception refused = Assert.Throws<InvalidOperationException>(() => backup.Preserve(Path.Combine(SaveFolder, "..\\elsewhere.dat")));

            Assert.Contains("outside the save folder", refused.Message);
            Assert.Empty(backup.Files);
        }

        [Theory]
        [InlineData("N")]
        [InlineData("N\\")]
        [InlineData("N/")]
        [InlineData("N\\..\\N\\")]
        [InlineData("n\\")]
        public void Preserve_SaveFolderWrittenAnotherWay_StillFindsTheFileInside(string folderAsWritten)
        {
            // Save locations in the library are written with "..", with either separator, and with or without a trailing one.
            WriteSave("SLOT_0\\save.dat", "steam progress");
            SaveBackup backup = store.StartNonXboxBackup(Package, "Test Game", Path.Combine(root, folderAsWritten), "copying files from Xbox");
            Transfer(backup, "SLOT_0\\save.dat", "xbox progress");
            backup.Complete();

            Assert.Equal("SLOT_0\\save.dat", Assert.Single(Stored().Files).RelativePath);
            store.Restore(Stored());
            Assert.Equal("steam progress", ReadSave("SLOT_0\\save.dat"));
        }

        [Fact]
        public void Preserve_FileThatCannotBeCopied_SaysSo_AndWorksWhenTriedAgain()
        {
            // As when the game still has the file open. The message ends up in front of the user.
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();

            using (new FileStream(Path.Combine(SaveFolder, "save.dat"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Exception failure = Assert.Throws<IOException>(() => backup.Preserve(Path.Combine(SaveFolder, "save.dat")));
                Assert.StartsWith("The file could not be backed up, so it was left as it is.", failure.Message);
            }
            Assert.Empty(backup.Files);
            Assert.Empty(store.List(Package));

            Transfer(backup, "save.dat", "xbox progress");
            backup.Complete();
            store.Restore(Stored());

            Assert.Equal("steam progress", ReadSave("save.dat"));
        }

        [Fact]
        public void List_BackupThatStoppedBeforeItsFirstFile_IsLeftOut()
        {
            SaveBackup backup = StartBackup();
            backup.Save();

            Assert.True(File.Exists(Path.Combine(backup.Folder, SaveBackup.ManifestName)));
            Assert.Empty(store.List(Package));
        }

        [Fact]
        public void StartNonXboxBackup_NothingKept_LeavesNothingOnDisk()
        {
            SaveBackup backup = StartBackup();
            backup.Complete();

            Assert.Empty(store.List(Package));
            Assert.False(Directory.Exists(backup.Folder));
        }

        [Fact]
        public void Restore_BacksUpWhatItReplaces_SoTheRestoreCanBeUndone()
        {
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "xbox progress");
            Transfer(backup, "SLOT_1\\extra.dat", "xbox extra");
            backup.Complete();
            SortedDictionary<string, string> afterTransfer = FolderContents.Read(SaveFolder);

            store.Restore(Stored());

            List<SaveBackup> backups = store.List(Package);
            Assert.Equal(2, backups.Count);
            Assert.StartsWith("restoring the backup from", backups[0].MadeBefore);
            Assert.Equal("steam progress", ReadSave("save.dat"));

            store.Restore(backups[0]);

            Assert.Equal(afterTransfer, FolderContents.Read(SaveFolder));
        }

        [Fact]
        public void Restore_FileDeletedSince_ComesBack_AndUndoingRemovesItAgain()
        {
            WriteSave("SLOT_0\\save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "SLOT_0\\save.dat", "xbox progress");
            backup.Complete();
            Directory.Delete(Path.Combine(SaveFolder, "SLOT_0"), true);

            SaveBackup replaced = store.Restore(Stored());
            Assert.Equal("steam progress", ReadSave("SLOT_0\\save.dat"));

            store.Restore(replaced);
            Assert.False(Directory.Exists(Path.Combine(SaveFolder, "SLOT_0")));
        }

        [Fact]
        public void Restore_BackupWithAFileMissing_IsRefusedBeforeAnythingChanges()
        {
            WriteSave("a.dat", "steam a");
            WriteSave("b.dat", "steam b");
            SaveBackup backup = StartBackup();
            Transfer(backup, "a.dat", "xbox a");
            Transfer(backup, "b.dat", "xbox b");
            backup.Complete();
            File.Delete(Path.Combine(backup.FilesFolder, "b.dat"));

            Exception refused = Assert.Throws<InvalidDataException>(() => store.Restore(Stored()));

            Assert.Contains("incomplete", refused.Message);
            Assert.Equal("xbox a", ReadSave("a.dat"));
            Assert.Equal("xbox b", ReadSave("b.dat"));
            Assert.Single(store.List(Package));
        }

        [Fact]
        public void Restore_BackupNamingAFileOutsideItsFolder_IsRefused()
        {
            // backup.json is a file anyone can edit. It must not be able to point a restore elsewhere.
            Write(Path.Combine(root, "elsewhere.dat"), "not a save");
            SaveBackup backup = StartBackup();
            Transfer(backup, "extra.dat", "xbox progress");
            backup.Complete();
            backup.Files[0].RelativePath = "..\\elsewhere.dat";
            backup.Save();

            Assert.Throws<InvalidDataException>(() => store.Restore(Stored()));

            Assert.Equal("not a save", File.ReadAllText(Path.Combine(root, "elsewhere.dat")));
        }

        [Fact]
        public void Restore_ReadOnlyFiles_AreReplacedAndRemoved()
        {
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "xbox progress");
            Transfer(backup, "extra.dat", "xbox extra");
            backup.Complete();
            File.SetAttributes(Path.Combine(SaveFolder, "save.dat"), FileAttributes.ReadOnly);
            File.SetAttributes(Path.Combine(SaveFolder, "extra.dat"), FileAttributes.ReadOnly);

            store.Restore(Stored());

            Assert.Equal("steam progress", ReadSave("save.dat"));
            Assert.Null(ReadSave("extra.dat"));
        }

        [Fact]
        public void Restore_GivesAFileItsOldModifiedTimeBack()
        {
            DateTime lastPlayed = new DateTime(2026, 3, 14, 15, 9, 26, DateTimeKind.Utc);
            WriteSave("save.dat", "steam progress");
            File.SetLastWriteTimeUtc(Path.Combine(SaveFolder, "save.dat"), lastPlayed);
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "xbox progress");
            backup.Complete();

            store.Restore(Stored());

            Assert.Equal(lastPlayed, File.GetLastWriteTimeUtc(Path.Combine(SaveFolder, "save.dat")));
        }

        [Fact]
        public void Complete_PutsEverythingInTheManifest_AndRemovesTheJournal()
        {
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "xbox progress");
            Transfer(backup, "extra.dat", "xbox extra");
            Assert.True(File.Exists(Path.Combine(backup.Folder, SaveBackup.JournalName)));

            backup.Complete();

            Assert.False(File.Exists(Path.Combine(backup.Folder, SaveBackup.JournalName)));
            SaveBackup stored = Stored();
            Assert.Equal(new[] { "save.dat", "extra.dat" }, stored.Files.Select(f => f.RelativePath));
            Assert.Equal(new[] { true, false }, stored.Files.Select(f => f.Existed));
            Assert.Equal("steam progress".Length, stored.Size);
        }

        [Fact]
        public void List_TransferThatNeverFinished_IsStillThereToRestore()
        {
            // The application was closed, or stopped responding, part way through. Complete was never called.
            WriteSave("save.dat", "steam progress");
            SaveBackup backup = StartBackup();
            Transfer(backup, "save.dat", "xbox progress");
            Transfer(backup, "extra.dat", "xbox extra");
            // And it died while writing the line for a third file, which it therefore never got to write.
            File.AppendAllText(Path.Combine(backup.Folder, SaveBackup.JournalName), "{\"RelativePath\":\"thi");

            SaveBackup stored = Stored();
            Assert.Equal(2, stored.Files.Count);
            store.Restore(stored);

            Assert.Equal("steam progress", ReadSave("save.dat"));
            Assert.Null(ReadSave("extra.dat"));
        }

        #endregion

        #region Xbox saves

        [Fact]
        public void Restore_XboxSaveAfterATransfer_IsExactlyAsItWas()
        {
            BuildXboxSave();
            SortedDictionary<string, string> before = FolderContents.Read(ProfileFolder);
            BackUpXbox();
            TransferToXbox();

            store.Restore(Stored());

            Assert.Equal(before, FolderContents.Read(ProfileFolder));
        }

        [Fact]
        public void Restore_XboxSaveTheGameHasRewrittenSince_IsExactlyAsItWas()
        {
            // Starting the game after a transfer: a container gets a new blob and a new container file,
            // the old ones are deleted, one container is dropped and another is made.
            BuildXboxSave();
            SortedDictionary<string, string> before = FolderContents.Read(ProfileFolder);
            BackUpXbox();
            TransferToXbox();
            File.Delete(Path.Combine(ProfileFolder, "AAAA", "BLOB1"));
            File.Delete(Path.Combine(ProfileFolder, "AAAA", "container.1"));
            WriteXbox("AAAA\\BLOB7", "game progress");
            WriteXbox("AAAA\\container.2", "container A v3");
            Directory.Delete(Path.Combine(ProfileFolder, "BBBB"), true);
            WriteXbox("CCCC\\container.1", "container C v1");
            WriteXbox("CCCC\\BLOB8", "new world");
            WriteXbox("containers.index", "index v3");

            store.Restore(Stored());

            Assert.Equal(before, FolderContents.Read(ProfileFolder));
        }

        [Fact]
        public void Restore_XboxSave_BacksUpTheSaveItReplaces()
        {
            BuildXboxSave();
            BackUpXbox();
            TransferToXbox();
            SortedDictionary<string, string> afterTransfer = FolderContents.Read(ProfileFolder);

            SaveBackup replaced = store.Restore(Stored());

            List<SaveBackup> backups = store.List(Package);
            Assert.Equal(2, backups.Count);
            Assert.Equal(BackupSide.Xbox, backups[0].Side);
            Assert.StartsWith("restoring the backup from", backups[0].MadeBefore);

            store.Restore(replaced);

            Assert.Equal(afterTransfer, FolderContents.Read(ProfileFolder));
        }

        [Fact]
        public void Restore_XboxSaveCutShort_LeavesTheIndexThatWasThere()
        {
            // containers.index must only go back once everything it names is back. Here a file of the
            // second container cannot be written, as when the game still has it open.
            BuildXboxSave();
            SortedDictionary<string, string> before = FolderContents.Read(ProfileFolder);
            BackUpXbox();
            TransferToXbox();
            WriteXbox("BBBB\\BLOB3", "changed world");

            using (new FileStream(Path.Combine(ProfileFolder, "BBBB", "BLOB3"), FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.ThrowsAny<IOException>(() => store.Restore(Stored()));
            }

            Assert.Equal("index v2", File.ReadAllText(Path.Combine(ProfileFolder, "containers.index")));
            Assert.Equal("progress", File.ReadAllText(Path.Combine(ProfileFolder, "AAAA", "BLOB1")));

            // Trying again once the file is free finishes the job.
            store.Restore(store.List(Package).Last());
            Assert.Equal(before, FolderContents.Read(ProfileFolder));
        }

        [Fact]
        public void BackUpXboxSave_HoldsEveryFileAndFolder()
        {
            BuildXboxSave();
            Directory.CreateDirectory(Path.Combine(ProfileFolder, "EMPTY"));

            SaveBackup backup = BackUpXbox();

            Assert.Equal(FolderContents.Read(ProfileFolder).Where(e => e.Value != null), FolderContents.Read(backup.FilesFolder).Where(e => e.Value != null));
            SaveBackup stored = Stored();
            Assert.Equal(6, stored.Files.Count);
            Assert.Equal(new[] { "AAAA", "BBBB", "EMPTY" }, stored.Folders.OrderBy(f => f));
            Assert.Equal(ProfileFolder, stored.OriginalFolder);
            Assert.Equal("copying 2 files to Xbox", stored.MadeBefore);
        }

        [Fact]
        public void Restore_XboxSave_PutsBackAnEmptyFolderToo()
        {
            BuildXboxSave();
            Directory.CreateDirectory(Path.Combine(ProfileFolder, "EMPTY"));
            SortedDictionary<string, string> before = FolderContents.Read(ProfileFolder);
            BackUpXbox();
            Directory.Delete(Path.Combine(ProfileFolder, "EMPTY"));

            store.Restore(Stored());

            Assert.Equal(before, FolderContents.Read(ProfileFolder));
        }

        [Fact]
        public void BackUpXboxSave_ProfileFolderMissing_IsRefused()
        {
            Directory.Delete(ProfileFolder);

            Assert.Throws<DirectoryNotFoundException>(() => BackUpXbox());

            Assert.Empty(store.List(Package));
        }

        [Fact]
        public void Restore_XboxSaveWhoseFolderIsGone_IsRefused()
        {
            BuildXboxSave();
            BackUpXbox();
            Directory.Delete(ProfileFolder, true);

            Assert.Throws<DirectoryNotFoundException>(() => store.Restore(Stored()));

            Assert.False(Directory.Exists(ProfileFolder));
        }

        [Fact]
        public void BackUpXboxSave_FolderThatIsNotAnXboxSaveFolder_IsRefused()
        {
            WriteSave("save.dat", "steam progress");

            Assert.Throws<ArgumentException>(() => store.BackUpXboxSave(Package, "Test Game", SaveFolder, "copying 1 file to Xbox"));
            // The right kind of folder, but another game's.
            Assert.Throws<ArgumentException>(() => store.BackUpXboxSave(OtherPackage, "Other Game", ProfileFolder, "copying 1 file to Xbox"));

            Assert.False(Directory.Exists(store.Root));
        }

        [Fact]
        public void Restore_XboxBackupPointedAtAnotherFolder_IsRefusedAndDeletesNothing()
        {
            // Restoring an Xbox backup removes whatever the backup does not hold. If backup.json could
            // send that anywhere, a damaged or edited file would empty the folder it named.
            BuildXboxSave();
            SaveBackup backup = BackUpXbox();
            WriteSave("save.dat", "steam progress");
            WriteSave("SLOT_0\\world.sav", "steam world");
            SortedDictionary<string, string> before = FolderContents.Read(SaveFolder);
            backup.OriginalFolder = SaveFolder;
            backup.Save();

            Exception refused = Assert.Throws<InvalidDataException>(() => store.Restore(Stored()));

            Assert.Contains("does not name an Xbox save folder", refused.Message);
            Assert.Equal(before, FolderContents.Read(SaveFolder));
            Assert.Single(store.List(Package));
        }

        [Theory]
        [InlineData("C:\\Users\\me\\AppData\\Local\\Packages\\Test.Game_abc123\\SystemAppData\\wgs\\0009000000000001_0001", true)]
        [InlineData("C:\\Users\\me\\AppData\\Local\\Packages\\Test.Game_abc123\\SystemAppData\\wgs\\0009000000000001_0001\\", true)]
        [InlineData("c:\\users\\me\\appdata\\local\\packages\\test.game_abc123\\systemappdata\\WGS\\0009000000000001_0001", true)]
        [InlineData("C:\\Users\\me\\AppData\\Local\\Packages\\Test.Game_abc123\\SystemAppData\\wgs", false)]
        [InlineData("C:\\Users\\me\\AppData\\Local\\Packages\\Test.Game_abc123\\SystemAppData\\wgs\\0009000000000001_0001\\AAAA", false)]
        [InlineData("C:\\Users\\me\\AppData\\Local\\Packages\\Other.Game_xyz789\\SystemAppData\\wgs\\0009000000000001_0001", false)]
        [InlineData("C:\\Users\\me\\AppData\\Local\\Packages\\Test.Game_abc123\\SystemAppData\\wgs\\0009000000000001_0001\\..\\..\\..\\..\\..", false)]
        [InlineData("C:\\Users\\me\\Documents", false)]
        [InlineData("C:\\", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsXboxProfileFolder_OnlyAProfileFolderOfThatGame(string folder, bool expected)
        {
            Assert.Equal(expected, SaveBackup.IsXboxProfileFolder(folder, Package));
        }

        [Fact]
        public void BackUpXboxSave_NotEnoughFreeSpace_IsRefusedBeforeCopyingAnything()
        {
            BuildXboxSave();
            store.FreeSpace = folder => 10;

            Exception refused = Assert.Throws<IOException>(() => BackUpXbox());

            Assert.Contains("not enough free disk space", refused.Message);
            Assert.False(Directory.Exists(store.Root));
        }

        [Fact]
        public void BackUpXboxSave_FreeSpaceUnknown_GoesAhead()
        {
            BuildXboxSave();
            store.FreeSpace = folder => null;

            BackUpXbox();

            Assert.Equal(6, Stored().Files.Count);
        }

        [Fact]
        public void BackUpXboxSave_AFileCannotBeRead_LeavesNoHalfBackup()
        {
            BuildXboxSave();

            using (new FileStream(Path.Combine(ProfileFolder, "BBBB", "BLOB3"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.ThrowsAny<IOException>(() => BackUpXbox());
            }

            Assert.Empty(store.List(Package));
            Assert.Empty(Directory.GetDirectories(Path.Combine(store.Root, Package)));
        }

        #endregion

        #region The store

        [Fact]
        public void List_NewestFirst()
        {
            BuildXboxSave();
            BackUpXbox("the first transfer");
            BackUpXbox("the second transfer");
            BackUpXbox("the third transfer");

            Assert.Equal(new[] { "the third transfer", "the second transfer", "the first transfer" }, store.List(Package).Select(b => b.MadeBefore));
        }

        [Fact]
        public void List_OnlyTheGameAskedFor()
        {
            BuildXboxSave();
            BackUpXbox();
            BackUpOtherGame();

            Assert.Equal("Test Game", Assert.Single(store.List(Package)).GameName);
            Assert.Equal("Other Game", Assert.Single(store.List(OtherPackage)).GameName);
            Assert.Empty(store.List("Never.BackedUp_000"));
        }

        [Fact]
        public void List_FoldersThatAreNotBackups_AreLeftOut()
        {
            BuildXboxSave();
            SaveBackup real = BackUpXbox();
            string gameFolder = Path.GetDirectoryName(real.Folder);
            Directory.CreateDirectory(Path.Combine(gameFolder, "no manifest"));
            Write(Path.Combine(gameFolder, "not json", SaveBackup.ManifestName), "this is not json");
            Write(Path.Combine(gameFolder, "empty object", SaveBackup.ManifestName), "{}");
            Write(Path.Combine(gameFolder, "from a later version", SaveBackup.ManifestName), File.ReadAllText(Path.Combine(real.Folder, SaveBackup.ManifestName)).Replace("\"FormatVersion\": 1", "\"FormatVersion\": 2"));

            Assert.Equal(real.Folder, Assert.Single(store.List(Package)).Folder);
        }

        [Fact]
        public void BackupsMadeInTheSameSecond_GetFoldersOfTheirOwn()
        {
            BuildXboxSave();
            store.Now = () => new DateTime(2026, 10, 9, 21, 0, 0);

            SaveBackup first = BackUpXbox();
            SaveBackup second = BackUpXbox();
            // Not on disk yet, so only the store knows these two names are taken.
            SaveBackup third = StartBackup();
            SaveBackup fourth = StartBackup();

            Assert.Equal(4, new[] { first.Folder, second.Folder, third.Folder, fourth.Folder }.Distinct().Count());
            Assert.Equal(2, store.List(Package).Count);
        }

        [Fact]
        public void Prune_KeepsTheNewest()
        {
            BuildXboxSave();
            BackUpXbox("the first transfer");
            BackUpXbox("the second transfer");
            BackUpXbox("the third transfer");
            BackUpXbox("the fourth transfer");
            BackUpOtherGame();

            int deleted = store.Prune(Package, 2);

            Assert.Equal(2, deleted);
            Assert.Equal(new[] { "the fourth transfer", "the third transfer" }, store.List(Package).Select(b => b.MadeBefore));
            Assert.Single(store.List(OtherPackage));
        }

        [Fact]
        public void Prune_FewerThanTheLimit_DeletesNothing()
        {
            BuildXboxSave();
            BackUpXbox();

            Assert.Equal(0, store.Prune(Package, 10));
            Assert.Single(store.List(Package));
        }

        [Fact]
        public void Delete_RemovesTheBackup_ReadOnlyFilesIncluded()
        {
            BuildXboxSave();
            File.SetAttributes(Path.Combine(ProfileFolder, "AAAA", "BLOB1"), FileAttributes.ReadOnly);
            SaveBackup backup = BackUpXbox();

            store.Delete(Stored());

            Assert.False(Directory.Exists(backup.Folder));
            Assert.Empty(store.List(Package));
        }

        [Fact]
        public void Delete_FolderThatIsNotABackupInTheStore_IsRefused()
        {
            WriteSave("save.dat", "steam progress");
            Write(Path.Combine(SaveFolder, SaveBackup.ManifestName), "{}");

            Assert.Throws<InvalidOperationException>(() => store.Delete(new SaveBackup { Folder = SaveFolder }));
            Assert.Throws<InvalidOperationException>(() => store.Delete(new SaveBackup { Folder = Path.Combine(store.Root, "..", "N") }));

            Assert.Equal("steam progress", ReadSave("save.dat"));
        }

        [Theory]
        [InlineData("C:\\Saves", "C:\\Saves\\a.sav", "a.sav")]
        [InlineData("C:\\Saves\\", "C:\\Saves\\SLOT_0\\a.sav", "SLOT_0\\a.sav")]
        [InlineData("C:\\Saves", "c:\\saves\\A.sav", "A.sav")]
        [InlineData("C:\\Saves", "C:\\Saves\\SLOT_0\\..\\a.sav", "a.sav")]
        [InlineData("C:\\Saves\\Game\\..", "C:\\Saves\\a.sav", "a.sav")]
        [InlineData("C:\\Saves", "C:\\Saves", null)]
        [InlineData("C:\\Saves", "C:\\SavesOld\\a.sav", null)]
        [InlineData("C:\\Saves", "C:\\Saves\\..\\a.sav", null)]
        [InlineData("C:\\Saves", "D:\\Saves\\a.sav", null)]
        [InlineData("C:\\", "C:\\a.sav", "a.sav")]
        public void RelativeTo_GivesThePathInsideTheFolder_OrNothing(string folder, string path, string expected)
        {
            Assert.Equal(expected, SaveBackup.RelativeTo(folder, path));
        }

        [Theory]
        [InlineData(0, "0 bytes")]
        [InlineData(1, "1 byte")]
        [InlineData(1023, "1023 bytes")]
        [InlineData(1024, "1 KB")]
        [InlineData(5 * 1024 * 1024, "5 MB")]
        [InlineData(3L * 1024 * 1024 * 1024, "3 GB")]
        public void FormatSize_UsesTheLargestUnitThatFits(long bytes, string expected)
        {
            Assert.Equal(expected, SaveBackup.FormatSize(bytes));
        }

        [Fact]
        public void FormatSize_RoundsToOneDecimal()
        {
            Assert.Equal(1.5.ToString("0.#") + " KB", SaveBackup.FormatSize(1536));
        }

        #endregion
    }
}
