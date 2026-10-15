using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// Files the game library says not to copy, and how a game decides which files a transfer leaves out.
    /// The Xbox side, which needs a real save, is in TransferIntegrationTests.
    /// </summary>
    public class FileLeftOutTests
    {
        private const string XboxProfileID = "000900000000ABCD";

        private static FileTranslation Translation(string nonXboxFilename, string containerName1, string xboxFileID, params string[] namedRegexGroups)
        {
            return new FileTranslation
            {
                NonXboxFilename = nonXboxFilename,
                ContainerName1 = containerName1,
                ContainerName2 = containerName1,
                XboxFileID = xboxFileID,
                NamedRegexGroups = namedRegexGroups
            };
        }

        private static NonXboxFileInfo File(string relativePath)
        {
            return new NonXboxFileInfo { RelativePath = relativePath };
        }

        // The two kinds of entry the game library starts with: RoadCraft's settings, which only the
        // Xbox save has, and The Alters' profile, which both sides have and which crashed the game.
        private static readonly FileLeftOut Settings = new FileLeftOut { ContainerName1 = "MainConfig", Reason = "Settings." };
        private static readonly FileLeftOut Profile = new FileLeftOut { ContainerName1 = "PlayerProfile", NonXboxFilename = "PlayerProfile\\.sav", Reason = "It crashed the game." };

        [Fact]
        public void MatchesXbox_ContainerNameOnly_TakesEveryFileInThatContainer()
        {
            Assert.True(Settings.MatchesXbox("MainConfig", "MainConfig", "config\\CommonSslSave.cfg"));
            Assert.True(Settings.MatchesXbox("MainConfig", "MainConfig", "config\\user_profile.cfg"));
            Assert.False(Settings.MatchesXbox("MainSave", "MainSave", "save\\SLOT_0/CompleteSave"));
        }

        [Fact]
        public void MatchesXbox_EveryNameGiven_HasToMatch()
        {
            FileLeftOut entry = new FileLeftOut { ContainerName1 = "Save.*", XboxFileID = "icon", Reason = "Icons." };

            Assert.True(entry.MatchesXbox("Save1", "", "icon"));
            Assert.False(entry.MatchesXbox("Save1", "", "data"));
        }

        [Fact]
        public void MatchesXbox_EntryWithNoXboxName_TakesNoXboxFile()
        {
            FileLeftOut entry = new FileLeftOut { NonXboxFilename = "steam_autocloud\\.vdf", Reason = "Steam's own." };

            Assert.False(entry.MatchesXbox("steam_autocloud.vdf", "", "steam_autocloud.vdf"));
        }

        [Fact]
        public void MatchesNonXbox_WholePathInAnyCapitals()
        {
            Assert.True(Profile.MatchesNonXbox("PlayerProfile.sav"));
            Assert.True(Profile.MatchesNonXbox("playerprofile.SAV"));
            Assert.False(Profile.MatchesNonXbox("PlayerProfile.sav.bak"));
            Assert.False(Profile.MatchesNonXbox("Old\\PlayerProfile.sav"));
        }

        [Fact]
        public void MatchesNonXbox_EntryWithNoNonXboxName_TakesNoNonXboxFile()
        {
            Assert.False(Settings.MatchesNonXbox("MainConfig"));
        }

        [Fact]
        public void FindProblem_CompleteEntries_ReturnNull()
        {
            Assert.Null(Settings.FindProblem());
            Assert.Null(Profile.FindProblem());
        }

        [Fact]
        public void FindProblem_NoReason_IsReported()
        {
            Assert.Contains("Reason", new FileLeftOut { ContainerName1 = "MainConfig" }.FindProblem());
        }

        [Fact]
        public void FindProblem_NoName_IsReported()
        {
            Assert.Contains("names no file", new FileLeftOut { Reason = "Settings." }.FindProblem());
        }

        [Fact]
        public void FindProblem_PatternThatIsNotValid_IsReported()
        {
            Assert.Contains("not a valid pattern", new FileLeftOut { ContainerName1 = "Save[", Reason = "Settings." }.FindProblem());
        }

        [Fact]
        public void ReasonToLeaveOut_FileTheLibraryLeavesOut_IsLeftOutEvenWithATranslation()
        {
            GameInfo game = new GameInfo { FilesLeftOut = new List<FileLeftOut> { Profile } };
            game.FileTranslations.Add(Translation("${FileName}.sav", "${FileName}", "Data", "(?<FileName>\\w+)"));

            Assert.Equal("It crashed the game.", game.ReasonToLeaveOut(File("PlayerProfile.sav"), XboxProfileID));
            Assert.Null(game.ReasonToLeaveOut(File("playthrough_8F3A2C.sav"), XboxProfileID));
        }

        [Fact]
        public void ReasonToLeaveOut_FileNoTranslationCovers_IsLeftOut()
        {
            GameInfo game = new GameInfo();
            game.FileTranslations.Add(Translation("${FileName}.sav", "${FileName}", "Data", "(?<FileName>\\w+)"));

            Assert.Equal(GameInfo.NoTranslationReason, game.ReasonToLeaveOut(File("steam_autocloud.vdf"), XboxProfileID));
        }

        [Fact]
        public void ApplyDeserializedInfo_EntryThatNamesFilesLeftOut_ReplacesThem()
        {
            GameInfo game = new GameInfo { FilesLeftOut = new List<FileLeftOut> { Settings } };

            game.ApplyDeserializedInfo(new GameInfo { FilesLeftOut = new List<FileLeftOut> { Profile } });

            Assert.Equal(new[] { Profile }, game.FilesLeftOut);
        }

        [Fact]
        public void ApplyDeserializedInfo_EntryThatSaysNothingOfThem_LeavesThem()
        {
            // A game profile a user loads names translations only. The library's word stays.
            GameInfo game = new GameInfo { FilesLeftOut = new List<FileLeftOut> { Settings } };

            game.ApplyDeserializedInfo(new GameInfo());

            Assert.Equal(new[] { Settings }, game.FilesLeftOut);
        }

        [Fact]
        public void ApplyPreview_FilesLeftOutOfTheEntryBeingTested_AreInForceUntilItGoes()
        {
            GameInfo game = new GameInfo();
            game.FileTranslations.Add(Translation("${FileName}.sav", "${FileName}", "Data", "(?<FileName>\\w+)"));
            GameInfo preview = new GameInfo { FilesLeftOut = new List<FileLeftOut> { Profile } };

            game.ApplyPreview(preview);
            Assert.Equal("It crashed the game.", game.ReasonToLeaveOut(File("PlayerProfile.sav"), XboxProfileID));

            game.ApplyPreview(null);
            Assert.Null(game.ReasonToLeaveOut(File("PlayerProfile.sav"), XboxProfileID));
        }

        private static GameInfo Shipped(string packageName)
        {
            StoredGameLibrary library = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Resources.GameLibrary);
            return library.GameInfo.Single(g => g.PackageName == packageName);
        }

        [Fact]
        public void GameLibrary_RoadCraft_LeavesOutTheSettingsButNotTheSave()
        {
            // The reporter in #164 copied the MainSave files only, and the save loaded.
            GameInfo roadCraft = Shipped("FocusHomeInteractiveSA.Roadbuilder_4hny5m903y3g0");

            Assert.Contains(roadCraft.FilesLeftOut, f => f.MatchesXbox("MainConfig", "MainConfig", "config\\CommonSslSave.cfg"));
            Assert.DoesNotContain(roadCraft.FilesLeftOut, f => f.MatchesXbox("MainSave", "MainSave", "save\\SLOT_0/CompleteSave"));
        }

        [Fact]
        public void GameLibrary_TheAlters_LeavesOutThePlayerProfileButNotTheSaves()
        {
            // A copied PlayerProfile crashed the game for one of the people in #135.
            GameInfo alters = Shipped("4063811bitstudios.TheAlters_gwy9gn5q9j1y6");

            Assert.StartsWith("The player profile.", alters.ReasonToLeaveOut(File("PlayerProfile.sav"), XboxProfileID));
            Assert.Contains(alters.FilesLeftOut, f => f.MatchesXbox("PlayerProfile", "PlayerProfile", "Data"));
            Assert.Null(alters.ReasonToLeaveOut(File("playthrough_8F3A2C.sav"), XboxProfileID));
        }
    }
}
