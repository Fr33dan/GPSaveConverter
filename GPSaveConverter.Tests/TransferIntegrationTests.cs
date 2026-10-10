using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
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

        #region Defects, recorded as they stand

        [Fact]
        public async Task Defect115_CopyFromXbox_BlobIDWithAFolderSeparator_Throws()
        {
            // The default pattern for ${FileName} allows a backslash. The path built from the Xbox
            // names is then used as a pattern when looking for an existing file, and "\L" is not one.
            save.WithXboxFile("Container", "Container", "Saves\\Level.sav", "xbox progress")
                .WithNonXboxFile("Saves\\Level.sav", "old steam progress")
                .Build();
            GameInfo game = Game(Translation("Container", "Container", "${FileName}", "${FileName}", "(?<FileName>[\\w\\-. \\\\]+)"));

            ArgumentException thrown = await Assert.ThrowsAnyAsync<ArgumentException>(() => CopyFromXbox(game, "Container", "Saves\\Level.sav"));

            Assert.Contains("Unrecognized escape sequence", thrown.Message);
        }

        #endregion
    }
}
