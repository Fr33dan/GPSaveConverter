using System;
using System.Collections.Generic;
using System.IO;
using GPSaveConverter.Library;
using GPSaveConverter.SaveBackups;
using GPSaveConverter.Xbox;
using Xunit;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// File > Open Xbox Save Folder: finding the save in a folder picked by hand, and reading it
    /// without ever writing to it (#138).
    /// </summary>
    public class PickedXboxSaveTests : IDisposable
    {
        private readonly FakeXboxSave save;

        /// <summary>A folder for copies of the save, removed with the made-up save.</summary>
        private readonly string copies;

        public PickedXboxSaveTests()
        {
            save = new FakeXboxSave()
                .WithXboxFile("SaveGame", "", "SaveSlot0", "xbox slot 0")
                .WithXboxFile("SaveGame", "", "SaveSlot3", "xbox slot 3")
                .Build();
            copies = Path.Combine(Path.GetDirectoryName(save.BackupFolder), "C");
        }

        public void Dispose()
        {
            save.Dispose();
        }

        private static void CopyFolder(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
            }
            foreach (string folder in Directory.GetDirectories(from))
            {
                CopyFolder(folder, Path.Combine(to, Path.GetFileName(folder)));
            }
        }

        /// <summary>A copy of the profile folder, under its own name, in a folder of its own below <see cref="copies"/>.</summary>
        private string CopyOfProfileFolder(string inFolder)
        {
            string copy = Path.Combine(copies, inFolder, Path.GetFileName(save.ProfileFolder));
            CopyFolder(save.ProfileFolder, copy);
            return copy;
        }

        [Fact]
        public void Find_TheProfileFolderPicked_IsTheSave()
        {
            string problem;

            PickedXboxSave found = PickedXboxSave.Find(save.ProfileFolder, out problem);

            Assert.Null(problem);
            Assert.Equal(save.ProfileFolder, found.ProfileFolder);
            Assert.Equal(FakeXboxSave.ProfileID, found.XboxProfileID);
            Assert.True(found.ProfileIDKnown);
        }

        [Fact]
        public void Find_ACopyOfTheGamesWholePackageFolder_FindsTheSaveThreeFoldersDown()
        {
            string packageFolder = Path.Combine(save.LocalAppData, "Packages", FakeXboxSave.PackageName);
            string problem;

            PickedXboxSave found = PickedXboxSave.Find(packageFolder, out problem);

            Assert.Equal(save.ProfileFolder, found.ProfileFolder);
            Assert.Equal(FakeXboxSave.ProfileID, found.XboxProfileID);
        }

        [Fact]
        public void Find_ASaveFourFoldersDown_IsNotLookedFor()
        {
            CopyFolder(save.ProfileFolder, Path.Combine(copies, "1", "2", "3", Path.GetFileName(save.ProfileFolder)));
            string problem;

            Assert.Null(PickedXboxSave.Find(copies, out problem));
            Assert.Contains("There is no Xbox save in", problem);
        }

        [Fact]
        public void Find_ACopyUnderAnotherName_DoesNotKnowTheProfile()
        {
            string copy = Path.Combine(copies, "My Xbox save");
            CopyFolder(save.ProfileFolder, copy);
            string problem;

            PickedXboxSave found = PickedXboxSave.Find(copy, out problem);

            Assert.Equal(copy, found.ProfileFolder);
            Assert.Equal(PickedXboxSave.UnknownProfileID, found.XboxProfileID);
            Assert.False(found.ProfileIDKnown);
        }

        [Fact]
        public void Find_ABackupTheToolMade_TakesTheProfileFromItsRecord()
        {
            SaveBackup backup = new SaveBackupStore(save.BackupFolder).BackUpXboxSave(FakeXboxSave.PackageName, "Test Game", save.ProfileFolder, "a test");
            string problem;

            PickedXboxSave found = PickedXboxSave.Find(backup.Folder, out problem);

            Assert.Equal(backup.FilesFolder, found.ProfileFolder);
            Assert.Equal(FakeXboxSave.ProfileID, found.XboxProfileID);
        }

        [Fact]
        public void Find_NoSave_SaysWhatToPick()
        {
            Directory.CreateDirectory(Path.Combine(copies, "empty"));
            string problem;

            Assert.Null(PickedXboxSave.Find(Path.Combine(copies, "empty"), out problem));
            Assert.Contains("There is no Xbox save in", problem);
            Assert.Contains("containers.index", problem);
        }

        [Fact]
        public void Find_AProfileFolderWithAnIndexAndNothingElse_IsPassedOver()
        {
            save.WithLeftoverProfileFolder(save.IndexPackageID);
            string wgsFolder = Path.GetDirectoryName(save.ProfileFolder);
            string problem;

            PickedXboxSave found = PickedXboxSave.Find(wgsFolder, out problem);

            Assert.Equal(save.ProfileFolder, found.ProfileFolder);
        }

        [Fact]
        public void Find_TwoSaves_NamesBothAndTakesNeither()
        {
            string first = CopyOfProfileFolder("first");
            string second = CopyOfProfileFolder("second");
            string problem;

            Assert.Null(PickedXboxSave.Find(copies, out problem));
            Assert.Contains("There is more than one Xbox save in", problem);
            Assert.Contains(first, problem);
            Assert.Contains(second, problem);
        }

        [Theory]
        [InlineData("0009000000000001_0001", "0009000000000001")]
        [InlineData(@"C:\Users\x\AppData\Local\Packages\Game_abc\SystemAppData\wgs\000901FD2E6B4A8F_00000000000000000000000069F80140\", "000901FD2E6B4A8F")]
        [InlineData("My_save", null)]
        [InlineData("files", null)]
        [InlineData("0009000000000001", null)]
        public void ProfileIDInName_ReadsTheXboxAppsFolderNames(string folder, string expected)
        {
            Assert.Equal(expected, PickedXboxSave.profileIDInName(folder));
        }

        [Fact]
        public void Open_TakesTheGameFromTheIndex()
        {
            string copy = CopyOfProfileFolder("copy");
            string problem;

            XboxContainerIndex index = PickedXboxSave.Find(copy, out problem).Open();

            Assert.True(index.IsPickedFolder);
            Assert.Equal(FakeXboxSave.PackageName, index.PackageName);
            Assert.Equal(FakeXboxSave.ProfileID, index.XboxProfileID);
            Assert.Equal(2, index.getFileList().Length);
            Assert.All(index.getFileList(), file => Assert.StartsWith(copy, file.Parent.getSaveFilePath()));
        }

        [Fact]
        public void Open_NothingCanBeWrittenToIt()
        {
            string copy = CopyOfProfileFolder("copy");
            SortedDictionary<string, string> before = FolderContents.Read(copy);
            string problem;
            XboxContainerIndex index = PickedXboxSave.Find(copy, out problem).Open();

            Assert.Throws<InvalidOperationException>(() => index.UpdateIndex());
            Assert.Throws<InvalidOperationException>(() => index.CreateContainer("NewSlot", ""));
            Assert.Throws<InvalidOperationException>(() => new GameInfo().getXboxFileVersion(index, null, true));

            Assert.Equal(before, FolderContents.Read(copy));
        }

        [Fact]
        public void Open_ASaveOfAGameNotOnThisPC_IsOpenedAsTheGameItsIndexNames()
        {
            // Its index names a game other than the package folder it sits in, as a copy taken
            // from another PC would, wherever it is put.
            using (FakeXboxSave other = new FakeXboxSave { IndexPackageID = "Other.Game_xyz789!App" }.WithXboxFile("Slot", "", "Data", "other game").Build())
            {
                string problem;

                XboxContainerIndex index = PickedXboxSave.Find(other.ProfileFolder, out problem).Open();

                Assert.Equal("Other.Game_xyz789", index.PackageName);
                Assert.Single(index.getFileList());
            }
        }
    }
}
