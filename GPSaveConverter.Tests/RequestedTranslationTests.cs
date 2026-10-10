using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using GPSaveConverter.Library;
using Outcome = GPSaveConverter.Tests.TranslationSimulator.Outcome;
using XboxFile = GPSaveConverter.Tests.TranslationSimulator.XboxFile;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// Translations written for games people asked about, checked against the file tables they posted.
    /// The issue number is on each game. These also pin down how the matching behaves on real names.
    /// Each game's translations are read from Fixtures\RequestedTranslations, a Game Profile file
    /// in the form File > Load Game Profile accepts, so the text given to a user is the text tested.
    /// </summary>
    public class RequestedTranslationTests
    {
        private static IList<FileTranslation> Profile(string game)
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "RequestedTranslations", game + ".json");
            return JsonSerializer.Deserialize<GameInfo>(File.ReadAllText(file)).FileTranslations;
        }

        [Theory]
        [InlineData("Persona3Portable")]
        [InlineData("RoadCraft")]
        [InlineData("Palworld")]
        [InlineData("Disgaea4Complete")]
        [InlineData("LikeADragonInfiniteWealth")]
        [InlineData("GenerationZero")]
        [InlineData("FinalFantasyVIIRemakeIntergrade")]
        public void Profile_PassesTheLibraryChecks(string game)
        {
            foreach (FileTranslation translation in Profile(game))
            {
                string problem = translation.FindProblem();
                Assert.True(problem == null, problem);
            }
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

        #region Persona 3 Portable (#168, #48)

        private static TranslationSimulator Persona3Portable()
        {
            IList<FileTranslation> translations = Profile("Persona3Portable");
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
            IList<FileTranslation> translations = Profile("RoadCraft");
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
        public void RoadCraft_NonXboxToXbox_FindsTheFileDespiteTheBackslashInItsID()
        {
            // The Xbox file ID holds a backslash, written "\\" in the translation. This used to miss the
            // existing file and add a second one under a wrong name.
            var result = RoadCraft().ToXbox("SLOT_0\\CompleteSave");

            Assert.Equal(Outcome.ExistingFile, result.Outcome);
            Assert.Equal("save\\SLOT_0/CompleteSave", result.XboxFile.FileID);
        }

        [Fact]
        public void RoadCraft_NonXboxToXbox_SteamOnlyFileWouldBeAddedUnderTheRightID()
        {
            var result = RoadCraft().ToXbox("SLOT_0\\rb_map_01_storm_preparation_terraforming_tile_grid");

            Assert.Equal(Outcome.NewFile, result.Outcome);
            Assert.Equal("save\\SLOT_0/rb_map_01_storm_preparation_terraforming_tile_grid", result.NewFileID);
        }

        #endregion

        #region Disgaea 4 Complete+ (#150)

        private const string Disgaea4Container2 = "Disgaea 4 Complete+";

        private static TranslationSimulator Disgaea4()
        {
            var xboxFiles = new[] { "save.001", "save.002", "save.003", "save.004", "save.005", "save.lst" }
                .Select(c => new XboxFile(c, Disgaea4Container2, "data"))
                .Concat(new[] { new XboxFile("SystemSave", "SystemSave", "data") })
                .ToArray();
            var nonXboxFiles = new[] { "Save_002.sav", "Save_003.sav", "Save_004.sav", "Save_005.sav", "Save_006.sav", "steam_autocloud.vdf", "SystemSave.sav" };
            return new TranslationSimulator(Profile("Disgaea4Complete"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("002")]
        [InlineData("003")]
        [InlineData("004")]
        [InlineData("005")]
        public void Disgaea4_SaveOnBothSides_MapsBothWaysByNumber(string slot)
        {
            TranslationSimulator game = Disgaea4();

            var toNonXbox = game.ToNonXbox(new XboxFile("save." + slot, Disgaea4Container2, "data"));
            var toXbox = game.ToXbox("Save_" + slot + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal("Save_" + slot + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal("save." + slot, toXbox.XboxFile.ContainerName1);
        }

        [Fact]
        public void Disgaea4_SystemSave_MapsBothWays()
        {
            TranslationSimulator game = Disgaea4();

            Assert.Equal("SystemSave.sav", game.ToNonXbox(new XboxFile("SystemSave", "SystemSave", "data")).RelativePath);
            Assert.Equal("SystemSave", game.ToXbox("SystemSave.sav").XboxFile.ContainerName1);
        }

        [Fact]
        public void Disgaea4_XboxOnlySave_IsCreatedUnderTheSameNumber()
        {
            var result = Disgaea4().ToNonXbox(new XboxFile("save.001", Disgaea4Container2, "data"));

            Assert.Equal(Outcome.NewFile, result.Outcome);
            Assert.Equal("Save_001.sav", result.PathOnDisk);
        }

        [Theory]
        [InlineData("save.lst")]
        public void Disgaea4_SaveList_IsLeftAlone(string container)
        {
            Assert.Equal(Outcome.NoTranslation, Disgaea4().ToNonXbox(new XboxFile(container, Disgaea4Container2, "data")).Outcome);
        }

        [Fact]
        public void Disgaea4_SteamOnlyFiles_AreNotSentToXbox()
        {
            TranslationSimulator game = Disgaea4();

            Assert.Equal(Outcome.NoContainer, game.ToXbox("Save_006.sav").Outcome);
            Assert.Equal(Outcome.NoTranslation, game.ToXbox("steam_autocloud.vdf").Outcome);
        }

        [Fact]
        public void Disgaea4_PlusSignTypedAsIs_NeverMatchesTheContainer()
        {
            // "+" means "one or more of the previous character" in a pattern, so the container name
            // typed as it is shown matches nothing. It has to be written "Complete\+".
            var typedAsShown = new[] { Translation("save.${Slot}", Disgaea4Container2, "data", "Save_${Slot}.sav", "(?<Slot>[0-9]{3})") };
            var game = new TranslationSimulator(typedAsShown, new[] { new XboxFile("save.002", Disgaea4Container2, "data") }, new[] { "Save_002.sav" });

            Assert.Equal(Outcome.NoTranslation, game.ToNonXbox(new XboxFile("save.002", Disgaea4Container2, "data")).Outcome);
            Assert.Equal(Outcome.NoContainer, game.ToXbox("Save_002.sav").Outcome);
        }

        #endregion

        #region Like a Dragon: Infinite Wealth (#145)

        private const string InfiniteWealthSave001 = "5802100a19b60000bac801008f0010050505040401141300000000000000021723220000000000000000000000000100";
        private const string InfiniteWealthSave002 = "5802362d6f2d04005aca7206280017050e0104010a150c09000000000000170324070500000000000000000101010100";

        private static TranslationSimulator InfiniteWealth()
        {
            // Container Name 2 is a long value that differs for every save.
            var slots = new Dictionary<string, string>
            {
                { "save001", InfiniteWealthSave001 },
                { "save002", InfiniteWealthSave002 },
                { "save003", "5802342cef3d04000a2cda0595006604630504040614010c15090a0d1300020423170724050306220000010100000100" },
                { "save031", "5802342cf03d04000a2cda0595006604630504040614010c15090a0d1300020423170724050306220000010100000100" },
                { "save032", "5802352cda2104005aa2d8047200de030e0204010a15090c000000000000170324050700000000000000000101010100" },
                { "save033", "5802342c0a3d0400da33da056d000000630504041406130d000000000000022304220600000000000000000100000100" }
            };
            var xboxFiles = slots.SelectMany(s => new[] { new XboxFile("save/" + s.Key + "/data.sav", s.Value, "data"), new XboxFile("save/" + s.Key + "/data.sav", s.Value, "icon") })
                                 .Concat(new[] { new XboxFile("save/system/data.sys", "displayName", "data") })
                                 .ToArray();
            var nonXboxFiles = new[]
            {
                "steam_autocloud.vdf", "save001\\data.sav", "save001\\save001_icon0.dds", "save031\\data.sav", "save031\\save031_icon0.dds",
                "save032\\data.sav", "save032\\save032_icon0.dds", "system\\data.sys"
            };
            return new TranslationSimulator(Profile("LikeADragonInfiniteWealth"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("save/save001/data.sav", InfiniteWealthSave001, "data", "save001\\data.sav")]
        [InlineData("save/save001/data.sav", InfiniteWealthSave001, "icon", "save001\\save001_icon0.dds")]
        [InlineData("save/system/data.sys", "displayName", "data", "system\\data.sys")]
        public void InfiniteWealth_FileOnBothSides_MapsBothWays(string container1, string container2, string fileID, string steamPath)
        {
            TranslationSimulator game = InfiniteWealth();

            var toNonXbox = game.ToNonXbox(new XboxFile(container1, container2, fileID));
            var toXbox = game.ToXbox(steamPath);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(steamPath, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container1, toXbox.XboxFile.ContainerName1);
            Assert.Equal(fileID, toXbox.XboxFile.FileID);
        }

        [Theory]
        [InlineData("data", "save002\\data.sav")]
        [InlineData("icon", "save002\\save002_icon0.dds")]
        public void InfiniteWealth_XboxOnlySlot_IsCreatedInItsOwnFolder(string fileID, string expectedPath)
        {
            var result = InfiniteWealth().ToNonXbox(new XboxFile("save/save002/data.sav", InfiniteWealthSave002, fileID));

            Assert.Equal(Outcome.NewFile, result.Outcome);
            Assert.Equal(expectedPath, result.PathOnDisk);
        }

        [Theory]
        [InlineData("save031\\data.sav", "save/save031/data.sav", "data")]
        [InlineData("save032\\save032_icon0.dds", "save/save032/data.sav", "icon")]
        public void InfiniteWealth_HigherSlots_MapToTheirOwnContainer(string steamPath, string container1, string fileID)
        {
            var result = InfiniteWealth().ToXbox(steamPath);

            Assert.Equal(Outcome.ExistingFile, result.Outcome);
            Assert.Equal(container1, result.XboxFile.ContainerName1);
            Assert.Equal(fileID, result.XboxFile.FileID);
        }

        [Fact]
        public void InfiniteWealth_SlotWithNoXboxContainer_CannotBeCopiedToXbox()
        {
            // Not in the posted tables: a slot saved only on Steam.
            Assert.Equal(Outcome.NoContainer, InfiniteWealth().ToXbox("save050\\data.sav").Outcome);
        }

        #endregion

        #region Generation Zero (#144)

        private static TranslationSimulator GenerationZero()
        {
            var xboxFiles = new[] { new XboxFile("GenerationZero", "GenerationZero", "savegame") };
            var nonXboxFiles = new[] { "savegame", "savegame.bac", "steam_autocloud.vdf" };
            return new TranslationSimulator(Profile("GenerationZero"), xboxFiles, nonXboxFiles);
        }

        [Fact]
        public void GenerationZero_SaveGame_MapsBothWays()
        {
            TranslationSimulator game = GenerationZero();

            var toNonXbox = game.ToNonXbox(new XboxFile("GenerationZero", "GenerationZero", "savegame"));
            var toXbox = game.ToXbox("savegame");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal("savegame", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal("savegame", toXbox.XboxFile.FileID);
        }

        [Theory]
        [InlineData("savegame.bac")]
        [InlineData("steam_autocloud.vdf")]
        public void GenerationZero_OtherSteamFiles_AreNotSentToXbox(string file)
        {
            Assert.Equal(Outcome.NoTranslation, GenerationZero().ToXbox(file).Outcome);
        }

        #endregion

        #region Palworld (#181)

        private const string PalworldXboxWorld = "04BDF4C0422948D9B8A856BF4F981A91";
        private const string PalworldSteamWorld = "9FD2FB8F42EF417C4C03F590F9B3C655";
        private const string PalworldPlayer = "00000000000000000000000000000001";

        private static TranslationSimulator Palworld()
        {
            IList<FileTranslation> translations = Profile("Palworld");
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

        #region Final Fantasy VII Remake Intergrade (#180)

        private static TranslationSimulator FinalFantasyVIIRemake()
        {
            // The Xbox table as pasted in the issue: a container for each of fourteen slots, and one more.
            var xboxFiles = Enumerable.Range(0, 14).Select(slot => "ff7remake" + slot.ToString("D3")).Concat(new[] { "ff7remakecommon" })
                                      .Select(container => new XboxFile(container, container, "Data")).ToArray();

            // The Steam files in the reporter's screenshot. The list went on below what it shows.
            var nonXboxFiles = Enumerable.Range(0, 8).Select(slot => "ff7remake" + slot.ToString("D3") + ".sav")
                                         .Concat(new[] { "ff7remakecommon.sav", "ff7remakedevice.sav", "ff7remakeplus000.sav" }).ToArray();

            return new TranslationSimulator(Profile("FinalFantasyVIIRemakeIntergrade"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("000")]
        [InlineData("003")]
        [InlineData("007")]
        [InlineData("common")]
        public void FinalFantasyVIIRemake_SaveOnBothSides_MapsBothWays(string slot)
        {
            TranslationSimulator game = FinalFantasyVIIRemake();

            var toNonXbox = game.ToNonXbox(new XboxFile("ff7remake" + slot, "ff7remake" + slot, "Data"));
            var toXbox = game.ToXbox("ff7remake" + slot + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal("ff7remake" + slot + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal("ff7remake" + slot + " | ff7remake" + slot + " | Data", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("008")]
        [InlineData("013")]
        public void FinalFantasyVIIRemake_XboxOnlySlot_IsCreatedUnderTheSameNumber(string slot)
        {
            var toNonXbox = FinalFantasyVIIRemake().ToNonXbox(new XboxFile("ff7remake" + slot, "ff7remake" + slot, "Data"));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal("ff7remake" + slot + ".sav", toNonXbox.RelativePath);
        }

        [Theory]
        [InlineData("ff7remakedevice.sav", "ff7remakedevice")]
        [InlineData("ff7remakeplus000.sav", "ff7remakeplus000")]
        public void FinalFantasyVIIRemake_SteamFileWithNoXboxContainer_NeedsOneOfItsOwnName(string file, string container)
        {
            // Versions up to v.0.4.12 refuse these two. Later ones ask whether to make the container.
            Assert.Equal(Outcome.NoContainer, FinalFantasyVIIRemake().ToXbox(file).Outcome);

            string problem;
            string[] names = Profile("FinalFantasyVIIRemakeIntergrade").Single().NewContainerNames(file, TranslationSimulator.XboxProfileID, out problem);
            Assert.Equal(new[] { container, container }, names);
        }

        #endregion
    }
}
