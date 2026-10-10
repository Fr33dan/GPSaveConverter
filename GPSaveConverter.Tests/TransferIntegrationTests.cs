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

        #region Defects, recorded as they stand

        [Fact]
        public async Task Defect106_CopyToXbox_NewBlobIsNamedWithRegexEscapes()
        {
            // Issue #106. A file with no blob yet is added to its container, but under the ID the
            // application built for matching, which has a backslash in front of the dot.
            save.WithXboxFile("Profile", "Profile", "settings.dat", "xbox settings")
                .WithNonXboxFile("peru_123abc.dat", "steam save")
                .Build();
            GameInfo game = Game(Translation("Profile", "Profile", "${FileName}", "${FileName}", "(?<FileName>[\\w\\-. \\\\]+)"));

            await CopyToXbox(game, "peru_123abc.dat");

            List<KeyValuePair<string, string>> blobs = save.ReadContainer("Profile", "Profile");
            Assert.Equal(2, blobs.Count);
            Assert.Equal(new KeyValuePair<string, string>("peru_123abc\\.dat", "steam save"), blobs[1]);
        }

        [Fact]
        public async Task DefectRoadCraft_CopyToXbox_BlobIDWithBackslashIsMissedAndASecondBlobAdded()
        {
            // Issue #164. The existing blob's ID contains a backslash. The application rebuilds the ID
            // with the backslashes multiplied, does not find the blob, and adds another one.
            save.WithXboxFile("MainSave", "MainSave", "save\\SLOT_0/CompleteSave", "xbox progress")
                .WithNonXboxFile("SLOT_0\\CompleteSave", "steam progress")
                .Build();
            GameInfo game = Game(Translation("MainSave", "MainSave", "save\\\\SLOT_${SlotNumber}/${FileName}", "SLOT_${SlotNumber}\\\\${FileName}", "(?<SlotNumber>\\d+)", "(?<FileName>.*)"));

            await CopyToXbox(game, "SLOT_0\\CompleteSave");

            List<KeyValuePair<string, string>> blobs = save.ReadContainer("MainSave", "MainSave");
            Assert.Equal(2, blobs.Count);
            Assert.Equal(new KeyValuePair<string, string>("save\\SLOT_0/CompleteSave", "xbox progress"), blobs[0]);
            Assert.Equal(new KeyValuePair<string, string>("save\\\\\\\\SLOT_0/CompleteSave", "steam progress"), blobs[1]);
        }

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
