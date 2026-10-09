using System.Collections.Generic;
using System.Linq;
using Xunit;
using GPSaveConverter.Library;
using Outcome = GPSaveConverter.Tests.TranslationSimulator.Outcome;
using XboxFile = GPSaveConverter.Tests.TranslationSimulator.XboxFile;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// Translations written for games people asked about, checked against the file tables they posted.
    /// The issue number is on each game. These also pin down how the matching behaves on real names.
    /// </summary>
    public class RequestedTranslationTests
    {
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

        #region Persona 3 Portable (#168, #48)

        private static TranslationSimulator Persona3Portable()
        {
            var translations = new[] { Translation("${FileName}", "", "progress", "${FileName}", "(?<FileName>[\\w.]+)") };
            var xboxFiles = new[]
            {
                new XboxFile("P3P.ini", "", "progress"),
                new XboxFile("P3PSAVE0001.BIN", "", "progress"),
                new XboxFile("P3PSAVE0001.BINslot", "", "progress"),
                new XboxFile("P3PSAVE0002.BIN", "", "progress"),
                new XboxFile("P3PSAVE0002.BINslot", "", "progress"),
                new XboxFile("P3PSAVE0003.BIN", "", "progress"),
                new XboxFile("P3PSAVE0003.BINslot", "", "progress")
            };
            var nonXboxFiles = new[] { "P3PSAVE0001.BIN", "P3PSAVE0001.BINslot", "P3PSAVE0002.BIN", "P3PSAVE0002.BINslot", "P3PSAVE0099.BIN", "P3PSAVE0099.BINslot" };
            return new TranslationSimulator(translations, xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("P3PSAVE0001.BIN")]
        [InlineData("P3PSAVE0001.BINslot")]
        [InlineData("P3PSAVE0002.BIN")]
        [InlineData("P3PSAVE0002.BINslot")]
        public void Persona3Portable_SaveOnBothSides_MapsBothWays(string name)
        {
            TranslationSimulator game = Persona3Portable();

            var toNonXbox = game.ToNonXbox(new XboxFile(name, "", "progress"));
            var toXbox = game.ToXbox(name);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(name, toXbox.XboxFile.ContainerName1);
            Assert.Equal("progress", toXbox.XboxFile.FileID);
        }

        [Theory]
        [InlineData("P3PSAVE0003.BIN")]
        [InlineData("P3PSAVE0003.BINslot")]
        [InlineData("P3P.ini")]
        public void Persona3Portable_XboxOnlyFile_IsCreatedUnderTheSameName(string name)
        {
            var result = Persona3Portable().ToNonXbox(new XboxFile(name, "", "progress"));

            Assert.Equal(Outcome.NewFile, result.Outcome);
            Assert.Equal(name, result.PathOnDisk);
        }

        [Fact]
        public void Persona3Portable_SlotWithNoXboxContainer_CannotBeCopiedToXbox()
        {
            Assert.Equal(Outcome.NoContainer, Persona3Portable().ToXbox("P3PSAVE0099.BIN").Outcome);
        }

        [Theory]
        [InlineData("P3PSAVE0001.BIN")]
        [InlineData("P3PSAVE0001.BINslot")]
        public void Persona3Portable_NewTranslationFromTheEditorWithTwoFieldsChanged_MapsBothWays(string name)
        {
            // The shortest fix to describe: press +, set the blob ID to "progress", clear Container Name 2.
            FileTranslation fromEditor = FileTranslation.getDefaultInstance();
            fromEditor.XboxFileID = "progress";
            fromEditor.ContainerName2 = "";
            var game = new TranslationSimulator(new[] { fromEditor }, new[] { new XboxFile(name, "", "progress") }, new[] { name });

            Assert.Equal(Outcome.ExistingFile, game.ToNonXbox(new XboxFile(name, "", "progress")).Outcome);
            Assert.Equal(Outcome.ExistingFile, game.ToXbox(name).Outcome);
        }

        #endregion

        #region RoadCraft (#164)

        private static readonly string[] RoadCraftSaveFiles =
        {
            "CompleteSave", "rb_map_01_storm_preparation", "rb_map_01_storm_preparation_extrusion", "rb_map_01_storm_preparation_fog_of_war",
            "rb_map_01_storm_preparation_fog_of_war_tile_grid", "rb_map_01_storm_preparation_terraforming", "rb_map_01_storm_preparation_terrain_deform", "screen_shot"
        };

        private static TranslationSimulator RoadCraft()
        {
            var translations = new[] { Translation("MainSave", "MainSave", "save\\\\SLOT_${SlotNumber}/${FileName}", "SLOT_${SlotNumber}\\\\${FileName}", "(?<SlotNumber>\\d+)", "(?<FileName>.*)") };
            var xboxFiles = RoadCraftSaveFiles.Concat(new[] { "CompleteSaveBackup", "rb_map_01_storm_preparation_extrusion_tile_grid" })
                                              .Select(f => new XboxFile("MainSave", "MainSave", "save\\SLOT_0/" + f))
                                              .Concat(new[] { new XboxFile("MainConfig", "MainConfig", "config\\user_profile.cfg") })
                                              .ToArray();
            var nonXboxFiles = RoadCraftSaveFiles.Concat(new[] { "rb_map_01_storm_preparation_terraforming_tile_grid" }).OrderBy(f => f, System.StringComparer.Ordinal).Select(f => "SLOT_0\\" + f).ToArray();
            return new TranslationSimulator(translations, xboxFiles, nonXboxFiles);
        }

        [Fact]
        public void RoadCraft_XboxToNonXbox_EverySharedFileMapsToItsOwnName()
        {
            TranslationSimulator game = RoadCraft();

            foreach (string file in RoadCraftSaveFiles)
            {
                var result = game.ToNonXbox(new XboxFile("MainSave", "MainSave", "save\\SLOT_0/" + file));

                Assert.Equal(Outcome.ExistingFile, result.Outcome);
                Assert.Equal("SLOT_0\\" + file, result.RelativePath);
            }
        }

        [Theory]
        [InlineData("CompleteSaveBackup")]
        [InlineData("rb_map_01_storm_preparation_extrusion_tile_grid")]
        public void RoadCraft_XboxToNonXbox_XboxOnlyFileIsCreatedInTheSlotFolder(string file)
        {
            var result = RoadCraft().ToNonXbox(new XboxFile("MainSave", "MainSave", "save\\SLOT_0/" + file));

            Assert.Equal(Outcome.NewFile, result.Outcome);
            Assert.Equal("SLOT_0\\" + file, result.PathOnDisk);
        }

        [Fact]
        public void RoadCraft_NonXboxToXbox_DoesNotFindTheExistingFile()
        {
            // The Xbox file ID holds a backslash. Going this way the application builds the ID with the
            // backslash doubled, misses the existing file, and would add a second one under a wrong name.
            var result = RoadCraft().ToXbox("SLOT_0\\CompleteSave");

            Assert.Equal(Outcome.NewFile, result.Outcome);
            Assert.Equal("save\\\\\\\\SLOT_0/CompleteSave", result.NewFileID);
        }

        #endregion

        #region Palworld (#181)

        private const string PalworldXboxWorld = "04BDF4C0422948D9B8A856BF4F981A91";
        private const string PalworldSteamWorld = "9FD2FB8F42EF417C4C03F590F9B3C655";
        private const string PalworldPlayer = "00000000000000000000000000000001";

        private static TranslationSimulator Palworld()
        {
            var translations = new[]
            {
                Translation(PalworldXboxWorld + "-Level-01", PalworldXboxWorld + "-Level-01", "Data", PalworldSteamWorld + "\\\\Level01.sav"),
                Translation(PalworldXboxWorld + "-${File}", PalworldXboxWorld + "-${File}", "Data", PalworldSteamWorld + "\\\\${File}.sav", "(?<File>LevelMeta|LocalData|WorldOption)"),
                Translation(PalworldXboxWorld + "-Players-${Player}", PalworldXboxWorld + "-Players-${Player}", "Data", PalworldSteamWorld + "\\\\Players\\\\${Player}.sav", "(?<Player>[0-9A-F]{32})")
            };
            var xboxFiles = new[] { "-Level-01", "-LevelMeta", "-LocalData", "-Players-" + PalworldPlayer, "-WorldOption" }
                .Select(c => new XboxFile(PalworldXboxWorld + c, PalworldXboxWorld + c, "Data"))
                .Concat(new[]
                {
                    new XboxFile("F2F795A1B1DE4C038CD1273AD72EF11A-LocalData", "F2F795A1B1DE4C038CD1273AD72EF11A-LocalData", "Data"),
                    new XboxFile("GDKBackupTimestamps", "GDKBackupTimestamps", "Data"),
                    new XboxFile("UserOption", "UserOption", "Data")
                })
                .ToArray();
            var nonXboxFiles = new[]
            {
                "GlobalPalStorage.sav",
                PalworldSteamWorld + "\\Level01.sav",
                PalworldSteamWorld + "\\LevelMeta.sav",
                PalworldSteamWorld + "\\LocalData.sav",
                PalworldSteamWorld + "\\WorldOption.sav",
                PalworldSteamWorld + "\\Players\\" + PalworldPlayer + ".sav"
            };
            return new TranslationSimulator(translations, xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("-Level-01", "\\Level01.sav")]
        [InlineData("-LevelMeta", "\\LevelMeta.sav")]
        [InlineData("-LocalData", "\\LocalData.sav")]
        [InlineData("-WorldOption", "\\WorldOption.sav")]
        [InlineData("-Players-" + PalworldPlayer, "\\Players\\" + PalworldPlayer + ".sav")]
        public void Palworld_WorldFile_MapsBothWays(string containerSuffix, string steamSuffix)
        {
            TranslationSimulator game = Palworld();
            string container = PalworldXboxWorld + containerSuffix;
            string steamPath = PalworldSteamWorld + steamSuffix;

            var toNonXbox = game.ToNonXbox(new XboxFile(container, container, "Data"));
            var toXbox = game.ToXbox(steamPath);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(steamPath, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container, toXbox.XboxFile.ContainerName1);
        }

        [Theory]
        [InlineData("F2F795A1B1DE4C038CD1273AD72EF11A-LocalData")]
        [InlineData("GDKBackupTimestamps")]
        [InlineData("UserOption")]
        public void Palworld_FilesOutsideTheWorld_AreLeftAlone(string container)
        {
            Assert.Equal(Outcome.NoTranslation, Palworld().ToNonXbox(new XboxFile(container, container, "Data")).Outcome);
        }

        [Fact]
        public void Palworld_FileNameWithoutItsFolder_OnlyMatchesFromTheXboxSide()
        {
            // What the reporter saw: a translation naming just "Level01.sav" finds the Steam file when
            // starting from Xbox, because that lookup is not anchored, but never matches starting from Steam.
            var translations = new[] { Translation(PalworldXboxWorld + "-Level-01", PalworldXboxWorld + "-Level-01", "Data", "Level01.sav") };
            var game = new TranslationSimulator(translations, new[] { new XboxFile(PalworldXboxWorld + "-Level-01", PalworldXboxWorld + "-Level-01", "Data") }, new[] { PalworldSteamWorld + "\\Level01.sav" });

            Assert.Equal(Outcome.ExistingFile, game.ToNonXbox(new XboxFile(PalworldXboxWorld + "-Level-01", PalworldXboxWorld + "-Level-01", "Data")).Outcome);
            Assert.Equal(Outcome.NoTranslation, game.ToXbox(PalworldSteamWorld + "\\Level01.sav").Outcome);
        }

        #endregion
    }
}
