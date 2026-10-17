using System.Collections.Generic;
using System.Linq;
using Xunit;
using GPSaveConverter.Library;
using XboxFile = GPSaveConverter.Library.TranslationSimulator.XboxFile;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// The suggester, on the file tables posted in the issues of games whose translations are known.
    /// Each suggestion has to be one that the issue's translation agrees with on every file.
    /// </summary>
    public class TranslationSuggesterTests
    {
        private static TranslationSuggester.Suggestion Suggest(IList<XboxFile> xboxFiles, params string[] nonXboxFiles)
        {
            return TranslationSuggester.Suggest(xboxFiles, nonXboxFiles, TranslationSimulator.XboxProfileID);
        }

        private static XboxFile[] InOneContainer(string containerName1, string containerName2, params string[] blobs)
        {
            return blobs.Select(blob => new XboxFile(containerName1, containerName2, blob)).ToArray();
        }

        private static void AssertTranslation(TranslationSuggester.Suggestion suggestion, string nonXboxFilename, string containerName1, string containerName2, string xboxFileID)
        {
            Assert.NotNull(suggestion);
            Assert.Equal(nonXboxFilename, suggestion.Translation.NonXboxFilename);
            Assert.Equal(containerName1, suggestion.Translation.ContainerName1);
            Assert.Equal(containerName2, suggestion.Translation.ContainerName2);
            Assert.Equal(xboxFileID, suggestion.Translation.XboxFileID);
            Assert.Null(suggestion.Translation.FindProblem());
        }

        private static string[] PairedPaths(TranslationSuggester.Suggestion suggestion)
        {
            return suggestion.Pairs.Select(p => p.Value).OrderBy(p => p, System.StringComparer.Ordinal).ToArray();
        }

        [Fact]
        public void Gears5_OneContainer_EachFileABlobOfItsName()
        {
            var suggestion = Suggest(InOneContainer("UE4DefaultSaveGameContainer", "Gears5", "CampaignProfile", "CampaignToC", "GearGameSaveGame", "GearSaveSlot_0", "GearSaveSlot_5"),
                "CampaignToC", "GearGameSaveGame", "GearSaveSlot_0", "GearSaveSlot_5", "steam_autocloud.vdf");

            AssertTranslation(suggestion, "${FileName}", "UE4DefaultSaveGameContainer", "Gears5", "${FileName}");
            Assert.Equal(new[] { "CampaignToC", "GearGameSaveGame", "GearSaveSlot_0", "GearSaveSlot_5" }, PairedPaths(suggestion));
            Assert.Empty(suggestion.Unpaired);
        }

        [Fact]
        public void EiyudenChronicleRising_TheOtherSideAddsAnEnding()
        {
            var suggestion = Suggest(InOneContainer("EiyudenSaveContainer", "EiyudenSaveContainer", "EcrSaveEc", "EcrSaveGame0", "EcrSaveGame1", "EcrSaveInfo0", "EcrSaveInfo1", "EcrSaveSys"),
                "EcrSaveGame0.sav", "EcrSaveInfo0.sav", "EcrSaveSys.sav", "steam_autocloud.vdf");

            AssertTranslation(suggestion, "${FileName}.sav", "EiyudenSaveContainer", "EiyudenSaveContainer", "${FileName}");
            Assert.Equal(new[] { "EcrSaveGame0.sav", "EcrSaveInfo0.sav", "EcrSaveSys.sav" }, PairedPaths(suggestion));
        }

        [Fact]
        public void LordsOfTheFallen_AContainerForEachSave_WithADataBlob()
        {
            var xboxFiles = new[] { "GeneralSave", "GeneralSave_b1", "GeneralSave_b2", "Save00", "Save00_b1", "Save00_b2" }.Select(c => new XboxFile(c, c, "Data")).ToArray();

            var suggestion = Suggest(xboxFiles, "GeneralSave.sav", "GeneralSaveBackup.sav", "Save00.sav", "Save02.sav", "SaveBackup.sav");

            AssertTranslation(suggestion, "${FileName}.sav", "${FileName}", "${FileName}", "Data");
            Assert.Equal(new[] { "GeneralSave.sav", "Save00.sav" }, PairedPaths(suggestion));
            // The question asks about these: copying them to Xbox would make containers for them.
            Assert.Equal(new[] { "GeneralSaveBackup.sav", "Save02.sav", "SaveBackup.sav" }, suggestion.Unpaired);
        }

        [Fact]
        public void Persona4Golden_AContainerForEachFile_WithAProgressBlob()
        {
            var xboxFiles = new[] { "P4G.ini", "data0001.bin", "data0001.binslot", "system.bin", "system.binslot" }.Select(c => new XboxFile(c, "", "progress")).ToArray();

            var suggestion = Suggest(xboxFiles, "data0001.bin", "data0001.binslot", "data0002.bin", "data0002.binslot", "system.bin", "system.binslot");

            AssertTranslation(suggestion, "${FileName}", "${FileName}", "", "progress");
            Assert.Equal(new[] { "data0001.bin", "data0001.binslot", "system.bin", "system.binslot" }, PairedPaths(suggestion));
        }

        [Fact]
        public void DeathStranding_AFolderForEachContainer()
        {
            var xboxFiles = new[]
            {
                new XboxFile("autosave3", "", "checkpoint.dat"), new XboxFile("manualsave0", "", "checkpoint.dat"), new XboxFile("quicksave0", "", "checkpoint.dat"),
                new XboxFile("profile", "", "bindings.cfg"), new XboxFile("profile", "", "game_settings.cfg"), new XboxFile("profile", "", "profile.dat"),
                new XboxFile("transfer", "", "transfer.dat")
            };

            var suggestion = Suggest(xboxFiles, "manualsave0\\checkpoint.dat", "profile\\bindings.cfg", "profile\\game_settings.cfg", "profile\\profile.dat", "quicksave0\\checkpoint.dat");

            AssertTranslation(suggestion, "${Folder}\\\\${FileName}", "${Folder}", "", "${FileName}");
            Assert.Equal(5, suggestion.Pairs.Count);
        }

        [Fact]
        public void SkywalkerSaga_FoldersInCapitalsOnSteam()
        {
            var xboxFiles = InOneContainer("profiledata", "profiledata", "achievements", "cutsceneprogress.blob", "dlcdata.blob")
                .Concat(InOneContainer("slot4", "slot4", "collectables.blob")).ToArray();

            var suggestion = Suggest(xboxFiles, "steam_autocloud.vdf", "PROFILEDATA\\ACHIEVEMENTS", "PROFILEDATA\\CUTSCENEPROGRESS.BLOB", "PROFILEDATA\\DLCDATA.BLOB", "SLOT4\\COLLECTABLES.BLOB");

            AssertTranslation(suggestion, "${Folder}\\\\${FileName}", "${Folder}", "${Folder}", "${FileName}");
            Assert.Equal(4, suggestion.Pairs.Count);
        }

        [Fact]
        public void GunfireReborn_TheSameNameEverywhere()
        {
            var xboxFiles = new[] { new XboxFile("user_1.dat", "user_1.dat", "user_1.dat"), new XboxFile("usersetting.ini", "usersetting.ini", "usersetting.ini") };

            var suggestion = Suggest(xboxFiles, "user_1.dat");

            AssertTranslation(suggestion, "${FileName}", "${FileName}", "${FileName}", "${FileName}");
            Assert.Equal(new[] { "user_1.dat" }, PairedPaths(suggestion));
        }

        [Fact]
        public void ReturnToMonkeyIsland_FilesWithNoXboxCopyAreNamedInTheSuggestion()
        {
            var suggestion = Suggest(InOneContainer("UserProfile", "UserProfile", "Prefs.json", "Savegame1.png", "Savegame1.save", "save.bin"),
                "Hintlog.txt", "Log.txt", "Playlog.txt", "Prefs.json", "save.bin", "Savegame1.png", "Savegame1.save", "Savegame2.png", "Savegame2.save");

            AssertTranslation(suggestion, "${FileName}", "UserProfile", "UserProfile", "${FileName}");
            Assert.Equal(4, suggestion.Pairs.Count);
            Assert.Equal(new[] { "Hintlog.txt", "Log.txt", "Playlog.txt", "Savegame2.png", "Savegame2.save" }, suggestion.Unpaired);
        }

        [Fact]
        public void ShadowrunReturns_NoNameOnBothSides_NothingIsSuggested()
        {
            // Each side holds different saves, so no file can be told from its name.
            var suggestion = Suggest(InOneContainer("SRR", "SRR", "7e756d9a4d3c.sav", "7e756d9b1dbd.sav"),
                "0820d50e12334d41b5e37ce584aeb850.sav", "4ab7bd8a03a3455b9fc2e22f6ff4cb0a.sav");

            Assert.Null(suggestion);
        }

        [Fact]
        public void NamesWithPatternCharacters_AreWrittenToMatchThemselvesOnly()
        {
            var suggestion = Suggest(InOneContainer("Disgaea 4 Complete+", "Save (1)", "Save_000", "Save_001"), "Save_000", "Save_001");

            AssertTranslation(suggestion, "${FileName}", "Disgaea 4 Complete\\+", "Save \\(1\\)", "${FileName}");
            Assert.Equal(2, suggestion.Pairs.Count);
        }
    }
}
