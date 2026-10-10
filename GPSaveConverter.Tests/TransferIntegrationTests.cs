using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using GPSaveConverter.SaveBackups;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;
using GPSaveConverter.Xbox;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// These tests point the application at a save folder under the temp directory, which means
    /// replacing statics that other test classes also use. They must not run alongside anything else.
    /// </summary>
    [CollectionDefinition("Application statics", DisableParallelization = true)]
    public class ApplicationStaticsCollection
    {
    }

    /// <summary>
    /// Runs the real transfer code, GameInfo.getNonXboxFileVersion and GameInfo.getXboxFileVersion,
    /// against a throwaway save on disk and then reads the result back from the files.
    /// </summary>
    [Collection("Application statics")]
    public class TransferIntegrationTests : IDisposable
    {
        private readonly FakeXboxSave save = new FakeXboxSave();

        public TransferIntegrationTests()
        {
            // Nothing here may reach the real settings, the network, PowerShell or the real save folders.
            ISettingsProvider settings = Substitute.For<ISettingsProvider>();
            settings.UserGameLibrary = "{ \"Version\": \"2026-01-01\", \"GameInfo\": [] }";
            settings.DefaultGameLibrary = settings.UserGameLibrary;
            settings.AllowWebDataFetch = false;
            GameLibrary.Settings = settings;
            GameLibrary.HttpClient = Substitute.For<IHttpClient>();
            GameLibrary.ScriptRunner = Substitute.For<IScriptRunner>();
            GameLibrary.Environment = new DefaultEnvironment();
            GameLibrary.Registry = Substitute.For<IRegistry>();
            GameLibrary.Initialize().GetAwaiter().GetResult();

            IEnvironment environment = Substitute.For<IEnvironment>();
            environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Returns(save.LocalAppData);
            XboxPackageList.Environment = environment;
        }

        public void Dispose()
        {
            XboxPackageList.Environment = new DefaultEnvironment();
            GameLibrary.nonXboxFiles.Clear();
            save.Dispose();
        }

        private static FileTranslation Translation(string containerName1, string containerName2, string xboxFileID, string nonXboxFilename, params string[] namedRegexGroups)
        {
            return new FileTranslation
            {
                ContainerName1 = containerName1,
                ContainerName2 = containerName2,
                XboxFileID = xboxFileID,
                NonXboxFilename = nonXboxFilename,
                NamedRegexGroups = namedRegexGroups
            };
        }

        private GameInfo Game(params FileTranslation[] translations)
        {
            GameInfo game = new GameInfo { PackageName = FakeXboxSave.PackageName, Name = "Test Game", BaseNonXboxSaveLocation = save.NonXboxFolder };
            game.FileTranslations.AddRange(translations);
            return game;
        }

        /// <summary>Copies one Xbox file to the non-Xbox folder the way the "˅" button does.</summary>
        private async Task CopyFromXbox(GameInfo game, string containerName1, string fileID)
        {
            await game.refreshNonXboxSaveFiles();
            XboxContainerIndex index = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            XboxFileInfo file = index.getFileList().Single(f => f.ContainerName1 == containerName1 && f.FileID == fileID);

            game.getNonXboxFileVersion(file, true);
        }

        /// <summary>Copies one non-Xbox file to the Xbox save the way the "˄" button does.</summary>
        private async Task CopyToXbox(GameInfo game, string relativePath)
        {
            await game.refreshNonXboxSaveFiles();
            XboxContainerIndex index = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            index.getFileList();
            NonXboxFileInfo file = GameLibrary.nonXboxFiles.Single(f => f.RelativePath == relativePath);

            game.getXboxFileVersion(index, file, true);
            index.UpdateIndex();
        }

        [Fact]
        public async Task CopyFromXbox_ReplacesTheMatchingNonXboxFile()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox progress")
                .WithNonXboxFile("saveFile0.sav", "old steam progress")
                .Build();
            GameInfo game = Game(Translation("SaveGame", "", "SaveSlot${FileSlot}", "saveFile${FileSlot}.sav", "(?<FileSlot>[0-9]+)"));

            await CopyFromXbox(game, "SaveGame", "SaveSlot0");

            Assert.Equal("xbox progress", save.ReadNonXboxFile("saveFile0.sav"));
        }

        [Fact]
        public async Task CopyFromXbox_CreatesAFileThatDoesNotExistYet()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot3", "xbox progress").Build();
            GameInfo game = Game(Translation("SaveGame", "", "SaveSlot${FileSlot}", "saveFile${FileSlot}.sav", "(?<FileSlot>[0-9]+)"));

            await CopyFromXbox(game, "SaveGame", "SaveSlot3");

            Assert.Equal("xbox progress", save.ReadNonXboxFile("saveFile3.sav"));
        }

        [Fact]
        public async Task CopyToXbox_ReplacesTheMatchingBlobAndLeavesTheOthersAlone()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "old xbox progress")
                .WithXboxFile("SaveGame", "", "SaveSlot1", "other slot")
                .WithNonXboxFile("saveFile0.sav", "steam progress")
                .Build();
            GameInfo game = Game(Translation("SaveGame", "", "SaveSlot${FileSlot}", "saveFile${FileSlot}.sav", "(?<FileSlot>[0-9]+)"));

            await CopyToXbox(game, "saveFile0.sav");

            List<KeyValuePair<string, string>> blobs = save.ReadContainer("SaveGame", "");
            Assert.Equal(2, blobs.Count);
            Assert.Equal(new KeyValuePair<string, string>("SaveSlot0", "steam progress"), blobs[0]);
            Assert.Equal(new KeyValuePair<string, string>("SaveSlot1", "other slot"), blobs[1]);
        }

        [Fact]
        public async Task CopyToXbox_ContainerMissing_IsRefused()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox progress")
                .WithNonXboxFile("other.sav", "steam progress")
                .Build();
            GameInfo game = Game(Translation("OtherContainer", "", "Data", "other.sav"));

            Exception refused = await Assert.ThrowsAnyAsync<Exception>(() => CopyToXbox(game, "other.sav"));

            Assert.Contains("container creation is not supported", refused.Message);
        }

        [Fact]
        public async Task CopyToXbox_FileWithNoBlobYet_IsAddedUnderItsPlainName()
        {
            // Issue #106. The new blob used to be named with the escapes the application adds for
            // matching: "peru_123abc\.dat".
            save.WithXboxFile("Profile", "Profile", "settings.dat", "xbox settings")
                .WithNonXboxFile("peru_123abc.dat", "steam save")
                .Build();
            GameInfo game = Game(Translation("Profile", "Profile", "${FileName}", "${FileName}", "(?<FileName>[\\w\\-. \\\\]+)"));

            await CopyToXbox(game, "peru_123abc.dat");

            List<KeyValuePair<string, string>> blobs = save.ReadContainer("Profile", "Profile");
            Assert.Equal(2, blobs.Count);
            Assert.Equal(new KeyValuePair<string, string>("settings.dat", "xbox settings"), blobs[0]);
            Assert.Equal(new KeyValuePair<string, string>("peru_123abc.dat", "steam save"), blobs[1]);
        }

        [Fact]
        public async Task CopyToXbox_BlobIDWithABackslash_ReplacesThatBlob()
        {
            // Issue #164. The backslash in the blob's ID is written "\\" in the translation. The
            // application used to miss the blob and add a second one named "save\\\\SLOT_0/CompleteSave".
            save.WithXboxFile("MainSave", "MainSave", "save\\SLOT_0/CompleteSave", "xbox progress")
                .WithNonXboxFile("SLOT_0\\CompleteSave", "steam progress")
                .Build();
            GameInfo game = Game(Translation("MainSave", "MainSave", "save\\\\SLOT_${SlotNumber}/${FileName}", "SLOT_${SlotNumber}\\\\${FileName}", "(?<SlotNumber>\\d+)", "(?<FileName>.*)"));

            await CopyToXbox(game, "SLOT_0\\CompleteSave");

            List<KeyValuePair<string, string>> blobs = save.ReadContainer("MainSave", "MainSave");
            Assert.Equal(new KeyValuePair<string, string>("save\\SLOT_0/CompleteSave", "steam progress"), Assert.Single(blobs));
        }

        [Fact]
        public async Task CopyToXbox_BlobIDTypedAsTheToolShowsIt_StillReplacesTheBlob()
        {
            // People type the ID with its single backslash, as the tool displays it. Going to Xbox that
            // has always worked, and it has to keep working.
            save.WithXboxFile("MainConfig", "MainConfig", "config\\achievements_common.cfg", "xbox achievements")
                .WithNonXboxFile("achievements_common.cfg", "steam achievements")
                .Build();
            GameInfo game = Game(Translation("MainConfig", "MainConfig", "config\\achievements_common.cfg", "achievements_common.cfg"));

            await CopyToXbox(game, "achievements_common.cfg");

            Assert.Equal(new KeyValuePair<string, string>("config\\achievements_common.cfg", "steam achievements"), Assert.Single(save.ReadContainer("MainConfig", "MainConfig")));
        }

        [Fact]
        public async Task CopyToXbox_BlobIDTypedAsShownThatAlsoReadsAsAnEscape_StillReplacesTheBlob()
        {
            // "\." reads as an escaped dot, but here the real ID has the backslash in it. The reading
            // earlier versions used is tried first, so this keeps matching.
            save.WithXboxFile("Container", "Container", "saves\\.backup", "xbox backup")
                .WithNonXboxFile("backup.dat", "steam backup")
                .Build();
            GameInfo game = Game(Translation("Container", "Container", "saves\\.backup", "backup.dat"));

            await CopyToXbox(game, "backup.dat");

            Assert.Equal(new KeyValuePair<string, string>("saves\\.backup", "steam backup"), Assert.Single(save.ReadContainer("Container", "Container")));
        }

        [Fact]
        public async Task CopyToXbox_NewBlobWithIDTypedAsTheToolShowsIt_IsAddedAsWritten()
        {
            save.WithXboxFile("MainConfig", "MainConfig", "config\\user_profile.cfg", "xbox profile")
                .WithNonXboxFile("achievements_common.cfg", "steam achievements")
                .Build();
            GameInfo game = Game(Translation("MainConfig", "MainConfig", "config\\achievements_common.cfg", "achievements_common.cfg"));

            await CopyToXbox(game, "achievements_common.cfg");

            List<KeyValuePair<string, string>> blobs = save.ReadContainer("MainConfig", "MainConfig");
            Assert.Equal(2, blobs.Count);
            Assert.Equal(new KeyValuePair<string, string>("config\\achievements_common.cfg", "steam achievements"), blobs[1]);
        }

        [Fact]
        public async Task CopyToXbox_EscapedDotInTheBlobID_FindsTheBlob()
        {
            // One library entry writes its blob ID as "${MapNumber}\.map".
            save.WithXboxFile("Maps", "Maps", "3.map", "xbox map")
                .WithNonXboxFile("3.map\\3.map", "steam map")
                .Build();
            GameInfo game = Game(Translation("Maps", "Maps", "${MapNumber}\\.map", "${MapNumber}.map\\\\${MapNumber}.map", "(?<MapNumber>[0-9_]+)"));

            await CopyToXbox(game, "3.map\\3.map");

            Assert.Equal(new KeyValuePair<string, string>("3.map", "steam map"), Assert.Single(save.ReadContainer("Maps", "Maps")));
        }

        [Fact]
        public async Task CopyToXbox_BlobIDNeedsAValueThePathDoesNotGive_FindsTheBlobByItsPattern()
        {
            save.WithXboxFile("Profile", "Profile", "save_EU", "xbox progress")
                .WithNonXboxFile("save.dat", "steam progress")
                .Build();
            GameInfo game = Game(Translation("Profile", "Profile", "save_${Region}", "save.dat", "(?<Region>[A-Z]+)"));

            await CopyToXbox(game, "save.dat");

            Assert.Equal(new KeyValuePair<string, string>("save_EU", "steam progress"), Assert.Single(save.ReadContainer("Profile", "Profile")));
        }

        [Fact]
        public async Task CopyToXbox_BlobIDNeedsAValueThePathDoesNotGive_WillNotInventABlob()
        {
            save.WithXboxFile("Profile", "Profile", "something else", "xbox progress")
                .WithNonXboxFile("save.dat", "steam progress")
                .Build();
            GameInfo game = Game(Translation("Profile", "Profile", "save_${Region}", "save.dat", "(?<Region>[A-Z]+)"));

            Exception refused = await Assert.ThrowsAnyAsync<Exception>(() => CopyToXbox(game, "save.dat"));

            Assert.Contains("No substitution data found", refused.Message);
            Assert.Equal(new KeyValuePair<string, string>("something else", "xbox progress"), Assert.Single(save.ReadContainer("Profile", "Profile")));
        }

        [Fact]
        public async Task CopyFromXbox_BlobIDWithAFolderSeparator_ReplacesTheFileInThatFolder()
        {
            // The path built from the Xbox names used to be read as a pattern when looking for an
            // existing file. "\L" is not a valid pattern, so this threw "Unrecognized escape sequence".
            save.WithXboxFile("Container", "Container", "Saves\\Level.sav", "xbox progress")
                .WithNonXboxFile("Saves\\Level.sav", "old steam progress")
                .Build();
            GameInfo game = Game(Translation("Container", "Container", "${FileName}", "${FileName}", "(?<FileName>[\\w\\-. \\\\]+)"));

            await CopyFromXbox(game, "Container", "Saves\\Level.sav");

            Assert.Equal("xbox progress", save.ReadNonXboxFile("Saves\\Level.sav"));
        }

        [Fact]
        public async Task CopyFromXbox_AnotherFileNameContainsTheTarget_OnlyTheTargetIsReplaced()
        {
            // The lookup matched anywhere in a name, and "autoprofile.sav" is listed before "profile.sav".
            save.WithXboxFile("Profile", "Profile", "profile", "xbox profile")
                .WithNonXboxFile("autoprofile.sav", "steam autosave")
                .WithNonXboxFile("profile.sav", "old steam profile")
                .Build();
            GameInfo game = Game(Translation("Profile", "Profile", "${FileName}", "${FileName}.sav", "(?<FileName>[\\w]+)"));

            await CopyFromXbox(game, "Profile", "profile");

            Assert.Equal("xbox profile", save.ReadNonXboxFile("profile.sav"));
            Assert.Equal("steam autosave", save.ReadNonXboxFile("autoprofile.sav"));
        }

        [Fact]
        public async Task CopyFromXbox_ProfileIDInTheFileName_IsFilledIn()
        {
            // Issue #90, with the first Forza Horizon 5 translation from the library. The file used to
            // be created as "User_${XboxProfileID}.ProfileData".
            save.WithXboxFile("User_9000000000001", "User_9000000000001", "ProfileData", "xbox profile").Build();
            GameInfo game = Game(Translation("User_${XboxProfileID}", "User_${XboxProfileID}", "${FileExtension}", "User_${XboxProfileID}.${FileExtension}", "(?<FileExtension>[\\w]+)"));

            await CopyFromXbox(game, "User_9000000000001", "ProfileData");

            Assert.Equal("xbox profile", save.ReadNonXboxFile("User_9000000000001.ProfileData"));
            Assert.Null(save.ReadNonXboxFile("User_${XboxProfileID}.ProfileData"));
        }

        [Fact]
        public async Task CopyFromXbox_ProfileIDInTheFileName_ReplacesTheExistingFile()
        {
            save.WithXboxFile("User_9000000000001", "User_9000000000001", "ProfileData", "xbox profile")
                .WithNonXboxFile("User_9000000000001.ProfileData", "old steam profile")
                .Build();
            GameInfo game = Game(Translation("User_${XboxProfileID}", "User_${XboxProfileID}", "${FileExtension}", "User_${XboxProfileID}.${FileExtension}", "(?<FileExtension>[\\w]+)"));

            await CopyFromXbox(game, "User_9000000000001", "ProfileData");

            Assert.Equal("xbox profile", save.ReadNonXboxFile("User_9000000000001.ProfileData"));
        }

        [Fact]
        public async Task CopyFromXbox_NewFileWithBracketsInItsName_IsCreated()
        {
            // Brackets in the name were counted as unfilled named groups: "No substitution data found."
            save.WithXboxFile("Saves", "Saves", "Save (1)", "xbox progress").Build();
            GameInfo game = Game(Translation("Saves", "Saves", "${FileName}", "${FileName}.sav", "(?<FileName>[\\w ()]+)"));

            await CopyFromXbox(game, "Saves", "Save (1)");

            Assert.Equal("xbox progress", save.ReadNonXboxFile("Save (1).sav"));
        }

        [Fact]
        public async Task CopyFromXbox_EscapedSeparatorInTheFileName_WritesIntoTheSubfolder()
        {
            // How the library writes a subfolder: "Save${FileSlot}\\EscapeAcademyProfile.json".
            save.WithXboxFile("Save0", "Save", "Profile", "xbox profile").Build();
            GameInfo game = Game(Translation("Save${FileSlot}", "Save", "Profile", "Save${FileSlot}\\\\EscapeAcademyProfile.json", "(?<FileSlot>[0-9]+)"));

            await CopyFromXbox(game, "Save0", "Profile");

            Assert.Equal("xbox profile", save.ReadNonXboxFile("Save0\\EscapeAcademyProfile.json"));
        }

        [Fact]
        public async Task CopyFromXbox_NameWithoutItsFolder_StillFindsTheFileAsEarlierVersionsDid()
        {
            // Not a correct translation, but it has always found the file going this way, and people
            // have translations like it saved.
            save.WithXboxFile("World-Level", "World-Level", "Data", "xbox world")
                .WithNonXboxFile("9FD2\\Level01.sav", "old steam world")
                .Build();
            GameInfo game = Game(Translation("World-Level", "World-Level", "Data", "Level01.sav"));

            await CopyFromXbox(game, "World-Level", "Data");

            Assert.Equal("xbox world", save.ReadNonXboxFile("9FD2\\Level01.sav"));
        }

        [Fact]
        public async Task CopyToXbox_FileInASubfolderWithTheDefaultTranslation_IsRefusedWithoutAPatternError()
        {
            // Issue #115. The file's own path went into the container pattern unescaped, and "\L" is not
            // a valid pattern: selecting the file threw "Unrecognized escape sequence".
            save.WithXboxFile("84C9-Level", "84C9-Level", "Data", "xbox world")
                .WithNonXboxFile("84C9\\Level.sav", "steam world")
                .Build();
            GameInfo game = Game(FileTranslation.getDefaultInstance());

            await game.refreshNonXboxSaveFiles();
            XboxContainerIndex index = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            index.getFileList();
            NonXboxFileInfo file = GameLibrary.nonXboxFiles.Single();

            Assert.Null(game.getXboxFileVersion(index, file));
            Exception refused = Assert.ThrowsAny<Exception>(() => game.getXboxFileVersion(index, file, true));
            Assert.Contains("container creation is not supported", refused.Message);
        }

        [Fact]
        public async Task CopyToXbox_ContainerNamedAfterAFileWithBracketsAndAPlus_IsFound()
        {
            save.WithXboxFile("Save (1)+.sav", "Save (1)+.sav", "Save (1)+.sav", "xbox progress")
                .WithNonXboxFile("Save (1)+.sav", "steam progress")
                .Build();
            GameInfo game = Game(Translation("${FileName}", "${FileName}", "${FileName}", "${FileName}", "(?<FileName>[\\w ()+.]+)"));

            await CopyToXbox(game, "Save (1)+.sav");

            Assert.Equal(new KeyValuePair<string, string>("Save (1)+.sav", "steam progress"), Assert.Single(save.ReadContainer("Save (1)+.sav", "Save (1)+.sav")));
        }

        [Fact]
        public async Task CopyToXbox_ContainerOnlyTheOldReadingMatches_IsStillFound()
        {
            // Earlier versions put the file's name into the container pattern unescaped, so the dot in
            // "a.c" matched any character. Nothing sensible relies on that, but it is kept as a fallback.
            save.WithXboxFile("abc", "abc", "abc", "xbox progress")
                .WithNonXboxFile("a.c", "steam progress")
                .Build();
            GameInfo game = Game(Translation("${FileName}", "${FileName}", "${FileName}", "${FileName}", "(?<FileName>[\\w.]+)"));

            await CopyToXbox(game, "a.c");

            List<KeyValuePair<string, string>> blobs = save.ReadContainer("abc", "abc");
            Assert.Equal(2, blobs.Count);
            Assert.Equal(new KeyValuePair<string, string>("a.c", "steam progress"), blobs[1]);
        }

        [Fact]
        public async Task CopyToXbox_NoXboxProfileOpen_FindsNothingInsteadOfFailing()
        {
            save.WithNonXboxFile("save.dat", "steam progress").Build();
            GameInfo game = Game(FileTranslation.getDefaultInstance());
            await game.refreshNonXboxSaveFiles();

            Assert.Null(game.getXboxFileVersion(null, GameLibrary.nonXboxFiles.Single()));
        }

        [Fact]
        public async Task CopyToXbox_ProfileIDInTheContainerName_ComesFromTheProfileBeingWrittenTo()
        {
            // The second Forza Horizon 5 direction. This used to depend on an Xbox file having been
            // selected first, and failed without one.
            save.WithXboxFile("User_9000000000001", "User_9000000000001", "ProfileData", "xbox profile")
                .WithNonXboxFile("User_9000000000001.ProfileData", "steam profile")
                .Build();
            GameInfo game = Game(Translation("User_${XboxProfileID}", "User_${XboxProfileID}", "${FileExtension}", "${Group}.${FileExtension}", "(?<Group>[\\w_]+)", "(?<FileExtension>[\\w_.]+)"));

            await CopyToXbox(game, "User_9000000000001.ProfileData");

            Assert.Equal(new KeyValuePair<string, string>("ProfileData", "steam profile"), Assert.Single(save.ReadContainer("User_9000000000001", "User_9000000000001")));
        }

        #region Opening an Xbox profile

        [Fact]
        public void OpenProfile_LeftoverFolderBesideTheSave_ReadsTheFolderThatHoldsTheSave()
        {
            // Issues #29 and #114. The Xbox app can leave several folders for one profile, and only
            // one of them has containers in it. The first one found used to be opened whatever it
            // held, which showed no Xbox files at all.
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox progress")
                .WithLeftoverProfileFolder(FakeXboxSave.PackageName + "!App")
                .Build();

            XboxContainerIndex index = new XboxContainerIndex(Game(), FakeXboxSave.ProfileID);

            Assert.Equal(save.ProfileFolder, index.xboxProfileFolder.TrimEnd('\\'));
            Assert.Equal("SaveSlot0", Assert.Single(index.getFileList()).FileID);
        }

        [Fact]
        public void OpenProfile_LeftoverFolderWhoseIndexNamesNoPackage_ReadsTheFolderThatHoldsTheSave()
        {
            // Issue #113, Forza Horizon 4: "Length cannot be less than zero" on selecting the game.
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox progress")
                .WithLeftoverProfileFolder(string.Empty)
                .Build();

            XboxContainerIndex index = new XboxContainerIndex(Game(), FakeXboxSave.ProfileID);

            Assert.Equal("SaveSlot0", Assert.Single(index.getFileList()).FileID);
        }

        [Fact]
        public void OpenProfile_OnlyALeftoverFolder_OpensItWithNoFiles()
        {
            save.WithLeftoverProfileFolder(FakeXboxSave.PackageName + "!App")
                .WithoutProfileFolder();

            XboxContainerIndex index = new XboxContainerIndex(Game(), FakeXboxSave.ProfileID);

            Assert.Empty(index.getFileList());
        }

        [Fact]
        public async Task OpenProfile_IndexNamesThePackageWithoutAnAppID_IsReadAndWrittenBackUnchanged()
        {
            // The other way to get "Length cannot be less than zero": no "!" after the package name.
            save.IndexPackageID = FakeXboxSave.PackageName;
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "old xbox progress")
                .WithNonXboxFile("saveFile0.sav", "steam progress")
                .Build();
            GameInfo game = Game(Translation("SaveGame", "", "SaveSlot${FileSlot}", "saveFile${FileSlot}.sav", "(?<FileSlot>[0-9]+)"));

            await CopyToXbox(game, "saveFile0.sav");

            Assert.Equal(new KeyValuePair<string, string>("SaveSlot0", "steam progress"), Assert.Single(save.ReadContainer("SaveGame", "")));
            Assert.Equal(FakeXboxSave.PackageName, save.ReadIndexPackageID());
        }

        [Theory]
        [InlineData("Another.Game_xyz789!App")]
        [InlineData("Another.Game_xyz789")]
        public void OpenProfile_IndexOfAnotherPackage_IsRefused(string indexPackageID)
        {
            save.IndexPackageID = indexPackageID;
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox progress").Build();

            Exception refused = Assert.ThrowsAny<Exception>(() => new XboxContainerIndex(Game(), FakeXboxSave.ProfileID));

            Assert.Equal("FileFormatException", refused.GetType().Name);
            Assert.Contains("package name mismatch", refused.Message);
        }

        [Fact]
        public void OpenProfile_NoFolderForTheProfile_SaysSo()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox progress").Build();

            Exception failure = Assert.Throws<System.IO.DirectoryNotFoundException>(() => new XboxContainerIndex(Game(), "000900000000FFFF"));

            Assert.Contains("000900000000FFFF", failure.Message);
        }

        #endregion

        #region Backups

        [Fact]
        public async Task CopyFromXbox_WithABackup_CanBeUndone()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox slot 0")
                .WithXboxFile("SaveGame", "", "SaveSlot3", "xbox slot 3")
                .WithNonXboxFile("saveFile0.sav", "steam slot 0")
                .WithNonXboxFile("settings.ini", "steam settings")
                .Build();
            GameInfo game = Game(Translation("SaveGame", "", "SaveSlot${FileSlot}", "saveFile${FileSlot}.sav", "(?<FileSlot>[0-9]+)"));
            SortedDictionary<string, string> before = FolderContents.Read(save.NonXboxFolder);
            SaveBackupStore store = new SaveBackupStore(save.BackupFolder);
            SaveBackup backup = store.StartNonXboxBackup(game.PackageName, game.Name, game.NonXboxSaveLocation, "copying 2 files from Xbox");

            await game.refreshNonXboxSaveFiles();
            XboxContainerIndex index = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            foreach (XboxFileInfo file in index.getFileList())
            {
                game.getNonXboxFileVersion(file, true, backup);
            }
            backup.Complete();

            Assert.Equal("xbox slot 0", save.ReadNonXboxFile("saveFile0.sav"));
            Assert.Equal("xbox slot 3", save.ReadNonXboxFile("saveFile3.sav"));

            store.Restore(Assert.Single(store.List(game.PackageName)));

            Assert.Equal(before, FolderContents.Read(save.NonXboxFolder));
        }

        [Fact]
        public async Task CopyFromXbox_IntoAFolderThatWasNotThere_UndoRemovesTheFolder()
        {
            save.WithXboxFile("Save0", "Save", "Profile", "xbox profile")
                .WithNonXboxFile("settings.ini", "steam settings")
                .Build();
            GameInfo game = Game(Translation("Save${FileSlot}", "Save", "Profile", "Save${FileSlot}\\\\EscapeAcademyProfile.json", "(?<FileSlot>[0-9]+)"));
            SortedDictionary<string, string> before = FolderContents.Read(save.NonXboxFolder);
            SaveBackupStore store = new SaveBackupStore(save.BackupFolder);
            SaveBackup backup = store.StartNonXboxBackup(game.PackageName, game.Name, game.NonXboxSaveLocation, "copying 1 file from Xbox");

            await game.refreshNonXboxSaveFiles();
            XboxContainerIndex index = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            game.getNonXboxFileVersion(index.getFileList().Single(), true, backup);
            backup.Complete();

            Assert.Equal("xbox profile", save.ReadNonXboxFile("Save0\\EscapeAcademyProfile.json"));

            store.Restore(Assert.Single(store.List(game.PackageName)));

            Assert.Equal(before, FolderContents.Read(save.NonXboxFolder));
        }

        [Fact]
        public async Task CopyFromXbox_WithoutABackup_WorksAsBefore()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox progress")
                .WithNonXboxFile("saveFile0.sav", "old steam progress")
                .Build();
            GameInfo game = Game(Translation("SaveGame", "", "SaveSlot${FileSlot}", "saveFile${FileSlot}.sav", "(?<FileSlot>[0-9]+)"));

            await game.refreshNonXboxSaveFiles();
            XboxContainerIndex index = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            game.getNonXboxFileVersion(index.getFileList().Single(), true, null);

            Assert.Equal("xbox progress", save.ReadNonXboxFile("saveFile0.sav"));
            Assert.False(System.IO.Directory.Exists(save.BackupFolder));
        }

        [Fact]
        public async Task CopyToXbox_AfterABackup_CanBeUndone()
        {
            save.WithXboxFile("SaveGame", "", "SaveSlot0", "xbox slot 0")
                .WithXboxFile("Profile", "Profile", "settings.dat", "xbox settings")
                .WithNonXboxFile("saveFile0.sav", "steam slot 0")
                .WithNonXboxFile("saveFile5.sav", "steam slot 5")
                .Build();
            GameInfo game = Game(Translation("SaveGame", "", "SaveSlot${FileSlot}", "saveFile${FileSlot}.sav", "(?<FileSlot>[0-9]+)"));
            SortedDictionary<string, string> before = FolderContents.Read(save.ProfileFolder);
            SaveBackupStore store = new SaveBackupStore(save.BackupFolder);
            store.BackUpXboxSave(game.PackageName, game.Name, new XboxContainerIndex(game, FakeXboxSave.ProfileID).xboxProfileFolder, "copying 2 files to Xbox");

            // The first replaces a blob. The second adds one, which also rewrites the container file.
            await CopyToXbox(game, "saveFile0.sav");
            await CopyToXbox(game, "saveFile5.sav");

            Assert.Equal(new[] { "steam slot 0", "steam slot 5" }, save.ReadContainer("SaveGame", "").Select(blob => blob.Value));
            Assert.NotEqual(before, FolderContents.Read(save.ProfileFolder));

            store.Restore(Assert.Single(store.List(game.PackageName)));

            Assert.Equal(before, FolderContents.Read(save.ProfileFolder));

            // And the application reads the restored save like any other.
            XboxContainerIndex restored = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            Assert.Equal(new[] { "SaveSlot0", "settings.dat" }, restored.getFileList().Select(file => file.FileID));
            Assert.Equal(new KeyValuePair<string, string>("SaveSlot0", "xbox slot 0"), Assert.Single(save.ReadContainer("SaveGame", "")));
        }

        #endregion
    }
}
