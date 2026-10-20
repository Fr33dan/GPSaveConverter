using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using GPSaveConverter.Library;
using Outcome = GPSaveConverter.Library.TranslationSimulator.Outcome;
using XboxFile = GPSaveConverter.Library.TranslationSimulator.XboxFile;

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
        [InlineData("HogwartsLegacy")]
        [InlineData("HellbladeSenuasSacrifice")]
        [InlineData("Gears5")]
        [InlineData("CallOfTheWildTheAngler")]
        [InlineData("TheAlters")]
        [InlineData("Dirt5")]
        [InlineData("LordsOfTheFallen")]
        [InlineData("ManorLords")]
        [InlineData("EiyudenChronicleRising")]
        [InlineData("Control")]
        [InlineData("MountAndBladeIIBannerlord")]
        [InlineData("ReturnToMonkeyIsland")]
        [InlineData("DeathStranding")]
        [InlineData("Persona4Golden")]
        [InlineData("TCGCardShopSimulator")]
        [InlineData("ResidentEvilVillage")]
        [InlineData("ResidentEvil2")]
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

        #region Hogwarts Legacy (#147, #152)

        private static readonly string[] HogwartsSaves = { "HL-00-00", "HL-00-10", "HL-00-11", "HL-00-12", "HL-00-13", "HL-00-14" };

        private static TranslationSimulator HogwartsLegacy()
        {
            // The Xbox table in the screenshots of #152. Every container holds one blob, "Data".
            var xboxFiles = new[] { "DisclaimerState" }.Concat(HogwartsSaves).Concat(new[] { "ModOperations", "SaveGameList", "UnplayedCharacters", "WelcomeState" })
                                                         .Select(container => new XboxFile(container, container, "Data")).ToArray();

            // The save files in the folder the reporter had copied to.
            var nonXboxFiles = HogwartsSaves.Select(save => save + ".sav").Concat(new[] { "SaveGameList.sav" }).ToArray();

            return new TranslationSimulator(Profile("HogwartsLegacy"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("HL-00-00")]
        [InlineData("HL-00-10")]
        [InlineData("HL-00-14")]
        [InlineData("SaveGameList")]
        public void HogwartsLegacy_FileOnBothSides_MapsBothWays(string name)
        {
            TranslationSimulator game = HogwartsLegacy();

            var toNonXbox = game.ToNonXbox(new XboxFile(name, name, "Data"));
            var toXbox = game.ToXbox(name + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(name + " | " + name + " | Data", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("DisclaimerState")]
        [InlineData("ModOperations")]
        [InlineData("UnplayedCharacters")]
        [InlineData("WelcomeState")]
        public void HogwartsLegacy_ContainersNobodyReportedNeeding_AreLeftAlone(string container)
        {
            Assert.Equal(Outcome.NoTranslation, HogwartsLegacy().ToNonXbox(new XboxFile(container, container, "Data")).Outcome);
        }

        [Theory]
        [InlineData("HogwartsLegacy.exe")]
        [InlineData("Engine\\Binaries\\ThirdParty\\DbgHelp\\dbghelp.dll")]
        public void HogwartsLegacy_FilesOfTheGameItself_AreNotSentToXbox(string file)
        {
            // In one screenshot of #152 the folder picked was the one the game is installed in.
            Assert.Equal(Outcome.NoTranslation, HogwartsLegacy().ToXbox(file).Outcome);
        }

        [Fact]
        public void HogwartsLegacy_WithTheSaveRuleAlone_TheSaveListHasNoTranslation()
        {
            // What the reporter of #152 had set up, and why copying "SaveGameList" stopped with
            // "Relative file path translation to Xbox file ID not found".
            var saveRuleAlone = new[] { Translation("HL-00-${slot}", "HL-00-${slot}", "Data", "HL-00-${slot}.sav", "(?<slot>[0-9]{2})") };
            var game = new TranslationSimulator(saveRuleAlone, new[] { new XboxFile("SaveGameList", "SaveGameList", "Data") }, new string[0]);

            Assert.Equal(Outcome.NoTranslation, game.ToNonXbox(new XboxFile("SaveGameList", "SaveGameList", "Data")).Outcome);
        }

        [Fact]
        public void HogwartsLegacy_XboxSaveNotOnTheOtherSide_IsCreatedUnderTheSameName()
        {
            // Not in either thread: going by the names, a second character's saves would be "HL-01-...".
            var game = new TranslationSimulator(Profile("HogwartsLegacy"), new[] { new XboxFile("HL-01-00", "HL-01-00", "Data") }, new string[0]);

            var toNonXbox = game.ToNonXbox(new XboxFile("HL-01-00", "HL-01-00", "Data"));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal("HL-01-00.sav", toNonXbox.RelativePath);
        }

        [Fact]
        public void HogwartsLegacy_SaveWithNoXboxContainer_NeedsOneOfItsOwnName()
        {
            Assert.Equal(Outcome.NoContainer, HogwartsLegacy().ToXbox("HL-00-01.sav").Outcome);

            string problem;
            string[] names = Profile("HogwartsLegacy").First().NewContainerNames("HL-00-01.sav", TranslationSimulator.XboxProfileID, out problem);
            Assert.Equal(new[] { "HL-00-01", "HL-00-01" }, names);
        }

        #endregion

        #region Hellblade: Senua's Sacrifice (#148)

        private const string HellbladeContainer = "UE4DefaultSaveGameContainer";

        private static TranslationSimulator Hellblade()
        {
            // No file table was posted. These are the names the profile in the issue spells out:
            // one container, with a blob for each of the two save files.
            var xboxFiles = new[] { "HellbladeExtSaveData", "HellbladeSave_0" }.Select(blob => new XboxFile(HellbladeContainer, HellbladeContainer, blob)).ToArray();
            var nonXboxFiles = new[] { "HellbladeExtSaveData.sav", "HellbladeSave_0.sav" };

            return new TranslationSimulator(Profile("HellbladeSenuasSacrifice"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("HellbladeExtSaveData")]
        [InlineData("HellbladeSave_0")]
        public void Hellblade_SaveFile_MapsBothWays(string name)
        {
            TranslationSimulator game = Hellblade();

            var toNonXbox = game.ToNonXbox(new XboxFile(HellbladeContainer, HellbladeContainer, name));
            var toXbox = game.ToXbox(name + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(HellbladeContainer + " | " + HellbladeContainer + " | " + name, toXbox.XboxFile.ToString());
        }

        [Fact]
        public void Hellblade_AnyOtherName_IsLeftAlone()
        {
            // The profile names its two files outright, so nothing else moves in either direction.
            Assert.Equal(Outcome.NoTranslation, Hellblade().ToXbox("HellbladeSave_1.sav").Outcome);
            Assert.Equal(Outcome.NoTranslation, Hellblade().ToNonXbox(new XboxFile(HellbladeContainer, HellbladeContainer, "HellbladeSave_1")).Outcome);
        }

        #endregion

        #region Gears 5 (#124)

        private const string GearsContainer = "UE4DefaultSaveGameContainer";

        private static TranslationSimulator Gears5()
        {
            // Both tables come from the saves attached to the issue. Every Xbox file is a blob in one container.
            var xboxFiles = new[] { "CampaignProfile", "CampaignToC", "GearGameSaveGame", "GearSaveSlot_0", "GearSaveSlot_5" }
                .Select(blob => new XboxFile(GearsContainer, "Gears5", blob)).ToArray();
            var nonXboxFiles = new[] { "CampaignToC", "GearGameSaveGame", "GearSaveSlot_0", "GearSaveSlot_5" };

            return new TranslationSimulator(Profile("Gears5"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("CampaignToC")]
        [InlineData("GearGameSaveGame")]
        [InlineData("GearSaveSlot_0")]
        [InlineData("GearSaveSlot_5")]
        public void Gears5_FileOnBothSides_MapsBothWays(string name)
        {
            TranslationSimulator game = Gears5();

            var toNonXbox = game.ToNonXbox(new XboxFile(GearsContainer, "Gears5", name));
            var toXbox = game.ToXbox(name);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(GearsContainer + " | Gears5 | " + name, toXbox.XboxFile.ToString());
        }

        [Fact]
        public void Gears5_XboxOnlyFile_IsCreatedUnderTheSameName()
        {
            var toNonXbox = Gears5().ToNonXbox(new XboxFile(GearsContainer, "Gears5", "CampaignProfile"));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal("CampaignProfile", toNonXbox.RelativePath);
        }

        [Fact]
        public void Gears5_SteamCloudFile_IsNotSentToXbox()
        {
            // The rule posted in the thread took any name, dots included. Steam can leave this file in the folder.
            Assert.Equal(Outcome.NoTranslation, Gears5().ToXbox("steam_autocloud.vdf").Outcome);
        }

        #endregion

        #region Call of the Wild: The Angler (#92)

        private static readonly string[] AnglerRootFiles = { "online_cache", "player_save_data", "player_settings" };
        private static readonly string[] AnglerSlotFiles = { "metadata", "online_cache", "player_save_data", "player_settings" };

        private static TranslationSimulator TheAngler()
        {
            // The tables posted in the issue. Each of five slots is a container of its own, and the files
            // outside the slots are in one more.
            var xboxFiles = AnglerRootFiles.Select(blob => new XboxFile("CotWTheAngler", "player_save_data", blob))
                .Concat(Enumerable.Range(0, 5).SelectMany(slot => AnglerSlotFiles.Select(blob => new XboxFile("CotWTheAngler/slots/" + slot, "metadata", blob))))
                .ToArray();
            var nonXboxFiles = AnglerRootFiles
                .Concat(Enumerable.Range(0, 5).SelectMany(slot => AnglerSlotFiles.Select(file => "slots\\" + slot + "\\" + file)))
                .ToArray();

            return new TranslationSimulator(Profile("CallOfTheWildTheAngler"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("0", "metadata")]
        [InlineData("0", "player_save_data")]
        [InlineData("2", "online_cache")]
        [InlineData("4", "player_settings")]
        public void TheAngler_SlotFile_MapsBothWays(string slot, string file)
        {
            TranslationSimulator game = TheAngler();
            string container = "CotWTheAngler/slots/" + slot;
            string path = "slots\\" + slot + "\\" + file;

            var toNonXbox = game.ToNonXbox(new XboxFile(container, "metadata", file));
            var toXbox = game.ToXbox(path);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(path, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container + " | metadata | " + file, toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("online_cache")]
        [InlineData("player_save_data")]
        [InlineData("player_settings")]
        public void TheAngler_FileOutsideTheSlots_MapsBothWays(string file)
        {
            TranslationSimulator game = TheAngler();

            var toNonXbox = game.ToNonXbox(new XboxFile("CotWTheAngler", "player_save_data", file));
            var toXbox = game.ToXbox(file);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(file, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal("CotWTheAngler | player_save_data | " + file, toXbox.XboxFile.ToString());
        }

        [Fact]
        public void TheAngler_SlotWithNoXboxContainer_NeedsOneOfItsOwnName()
        {
            Assert.Equal(Outcome.NoContainer, TheAngler().ToXbox("slots\\5\\metadata").Outcome);

            string problem;
            string[] names = Profile("CallOfTheWildTheAngler").First().NewContainerNames("slots\\5\\metadata", TranslationSimulator.XboxProfileID, out problem);
            Assert.Equal(new[] { "CotWTheAngler/slots/5", "metadata" }, names);
        }

        #endregion

        #region The Alters (#135, #136)

        // No file table was posted. These names are made up to fit the patterns of the translation in
        // the thread, which two people there used.
        private static readonly string[] AltersSaves = { "playthrough_8F3A2C", "8F3A2C-ACT1-day-3-v-1", "8F3A2C-Journey2-day-14-v-0" };

        private static TranslationSimulator TheAlters()
        {
            var names = AltersSaves.Concat(new[] { "PlayerProfile" }).ToArray();
            return new TranslationSimulator(Profile("TheAlters"), names.Select(name => new XboxFile(name, name, "Data")).ToArray(), names.Select(name => name + ".sav").ToArray());
        }

        [Theory]
        [InlineData("playthrough_8F3A2C")]
        [InlineData("8F3A2C-ACT1-day-3-v-1")]
        [InlineData("8F3A2C-Journey2-day-14-v-0")]
        public void TheAlters_Save_MapsBothWays(string name)
        {
            TranslationSimulator game = TheAlters();

            var toNonXbox = game.ToNonXbox(new XboxFile(name, name, "Data"));
            var toXbox = game.ToXbox(name + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(name + " | " + name + " | Data", toXbox.XboxFile.ToString());
        }

        [Fact]
        public void TheAlters_PlayerProfile_IsLeftAlone()
        {
            // Copying it made the game crash for one of the people in #135, who found the two versions
            // write it differently.
            Assert.Equal(Outcome.NoTranslation, TheAlters().ToXbox("PlayerProfile.sav").Outcome);
            Assert.Equal(Outcome.NoTranslation, TheAlters().ToNonXbox(new XboxFile("PlayerProfile", "PlayerProfile", "Data")).Outcome);
        }

        #endregion

        #region DIRT 5 (#108)

        private static TranslationSimulator Dirt5()
        {
            // The Xbox table in the screenshot of #108: one container holding every file under its path.
            // Two names are cut off in the screenshot; the first ghost is spelled out in the error box.
            var xboxFiles = new[] { "ghosts/6_17663734281804447716.spooky", "ghosts/6_3812766141409", "profile/items.prf", "profile/private.prf",
                                    "profile/public.prf", "profile/settings.json", "recipies/10340993300678" }
                .Select(blob => new XboxFile("Dirt5", "SaveData", blob)).ToArray();
            var nonXboxFiles = new[] { "items.prf", "private.prf", "public.prf" };

            return new TranslationSimulator(Profile("Dirt5"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("items")]
        [InlineData("private")]
        [InlineData("public")]
        public void Dirt5_ProfileFile_MapsBothWays(string name)
        {
            // The three files the reporter matched by hand, after which the progress carried over.
            TranslationSimulator game = Dirt5();

            var toNonXbox = game.ToNonXbox(new XboxFile("Dirt5", "SaveData", "profile/" + name + ".prf"));
            var toXbox = game.ToXbox(name + ".prf");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".prf", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal("Dirt5 | SaveData | profile/" + name + ".prf", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("ghosts/6_17663734281804447716.spooky")]
        [InlineData("profile/settings.json")]
        [InlineData("recipies/10340993300678")]
        public void Dirt5_OtherXboxFiles_AreLeftAlone(string blob)
        {
            Assert.Equal(Outcome.NoTranslation, Dirt5().ToNonXbox(new XboxFile("Dirt5", "SaveData", blob)).Outcome);
        }

        #endregion

        #region Lords of the Fallen (#98)

        private static TranslationSimulator LordsOfTheFallen()
        {
            // The tables posted in the issue. On Xbox each save has two backups of its own, _b1 and _b2.
            var xboxFiles = new[] { "GeneralSave", "GeneralSave_b1", "GeneralSave_b2", "Save00", "Save00_b1", "Save00_b2" }
                .Select(container => new XboxFile(container, container, "Data")).ToArray();
            var nonXboxFiles = new[] { "GeneralSave.sav", "GeneralSaveBackup.sav", "Save00.sav", "Save02.sav", "SaveBackup.sav" };

            return new TranslationSimulator(Profile("LordsOfTheFallen"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("GeneralSave")]
        [InlineData("Save00")]
        public void LordsOfTheFallen_Save_MapsBothWays(string name)
        {
            TranslationSimulator game = LordsOfTheFallen();

            var toNonXbox = game.ToNonXbox(new XboxFile(name, name, "Data"));
            var toXbox = game.ToXbox(name + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(name + " | " + name + " | Data", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("GeneralSave_b1")]
        [InlineData("Save00_b2")]
        public void LordsOfTheFallen_XboxBackups_AreLeftAlone(string container)
        {
            Assert.Equal(Outcome.NoTranslation, LordsOfTheFallen().ToNonXbox(new XboxFile(container, container, "Data")).Outcome);
        }

        [Theory]
        [InlineData("GeneralSaveBackup.sav")]
        [InlineData("SaveBackup.sav")]
        public void LordsOfTheFallen_SteamBackups_AreLeftAlone(string file)
        {
            Assert.Equal(Outcome.NoTranslation, LordsOfTheFallen().ToXbox(file).Outcome);
        }

        [Fact]
        public void LordsOfTheFallen_SteamOnlySave_NeedsAContainerOfItsOwnName()
        {
            Assert.Equal(Outcome.NoContainer, LordsOfTheFallen().ToXbox("Save02.sav").Outcome);

            string problem;
            string[] names = Profile("LordsOfTheFallen").Single().NewContainerNames("Save02.sav", TranslationSimulator.XboxProfileID, out problem);
            Assert.Equal(new[] { "Save02", "Save02" }, names);
        }

        #endregion

        #region Manor Lords (#87)

        private static readonly string[] ManorLordsSaves = { "autosave", "autosave_descr", "rtsSettings", "saveGame_1", "saveGame_1_descr" };

        private static TranslationSimulator ManorLords()
        {
            // The tables posted in the issue. The Steam side also has a picture and a coat of arms for
            // each save, which the Xbox side has nothing for.
            var xboxFiles = ManorLordsSaves.Select(container => new XboxFile(container, container, "Data")).ToArray();
            var nonXboxFiles = new[] { "autosave.png", "autosave.sav", "autosave_coat_0.png", "autosave_descr.sav", "rtsSettings.sav",
                                       "saveGame_1.png", "saveGame_1.sav", "saveGame_1_coat_0.png", "saveGame_1_descr.sav", "steam_autocloud.vdf" };

            return new TranslationSimulator(Profile("ManorLords"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("autosave")]
        [InlineData("autosave_descr")]
        [InlineData("rtsSettings")]
        [InlineData("saveGame_1")]
        [InlineData("saveGame_1_descr")]
        public void ManorLords_Save_MapsBothWays(string name)
        {
            TranslationSimulator game = ManorLords();

            var toNonXbox = game.ToNonXbox(new XboxFile(name, name, "Data"));
            var toXbox = game.ToXbox(name + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(name + " | " + name + " | Data", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("autosave.png")]
        [InlineData("saveGame_1_coat_0.png")]
        [InlineData("steam_autocloud.vdf")]
        public void ManorLords_SteamOnlyFiles_AreNotSentToXbox(string file)
        {
            Assert.Equal(Outcome.NoTranslation, ManorLords().ToXbox(file).Outcome);
        }

        #endregion

        #region Eiyuden Chronicle: Rising (#17)

        private const string EiyudenContainer = "EiyudenSaveContainer";

        private static TranslationSimulator EiyudenChronicleRising()
        {
            // The tables in the screenshot posted in the issue. Every Xbox file is a blob in one container.
            var xboxFiles = new[] { "EcrSaveEc", "EcrSaveGame0", "EcrSaveGame1", "EcrSaveGame2", "EcrSaveInfo0", "EcrSaveInfo1", "EcrSaveInfo2", "EcrSaveSys" }
                .Select(blob => new XboxFile(EiyudenContainer, EiyudenContainer, blob)).ToArray();
            var nonXboxFiles = new[] { "EcrSaveGame0.sav", "EcrSaveInfo0.sav", "EcrSaveSys.sav", "steam_autocloud.vdf" };

            return new TranslationSimulator(Profile("EiyudenChronicleRising"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("EcrSaveGame0")]
        [InlineData("EcrSaveInfo0")]
        [InlineData("EcrSaveSys")]
        public void EiyudenChronicleRising_FileOnBothSides_MapsBothWays(string name)
        {
            TranslationSimulator game = EiyudenChronicleRising();

            var toNonXbox = game.ToNonXbox(new XboxFile(EiyudenContainer, EiyudenContainer, name));
            var toXbox = game.ToXbox(name + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(EiyudenContainer + " | " + EiyudenContainer + " | " + name, toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("EcrSaveEc")]
        [InlineData("EcrSaveGame2")]
        [InlineData("EcrSaveInfo2")]
        public void EiyudenChronicleRising_XboxOnlyFile_IsCreatedUnderItsName(string name)
        {
            var toNonXbox = EiyudenChronicleRising().ToNonXbox(new XboxFile(EiyudenContainer, EiyudenContainer, name));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
        }

        [Fact]
        public void EiyudenChronicleRising_SteamCloudFile_IsNotSentToXbox()
        {
            Assert.Equal(Outcome.NoTranslation, EiyudenChronicleRising().ToXbox("steam_autocloud.vdf").Outcome);
        }

        #endregion

        #region Control (#82)

        private static TranslationSimulator Control()
        {
            // The tables in the screenshot posted in the issue in January 2026. The Steam list runs on
            // below the window; these are the rows it shows. The Steam version keeps each blob as a
            // file named after its container and itself. The Epic version keeps it as a .chunk file in
            // a folder named after the container, which is what the translations earlier in the thread are for.
            var xboxFiles = new[]
            {
                new XboxFile("achievements", "", "data"),
                new XboxFile("preferences", "", "data"),
                new XboxFile("savegame-slot-00", "", "global"),
                new XboxFile("savegame-slot-00", "", "hub"),
                new XboxFile("savegame-slot-00", "", "meta"),
                new XboxFile("savegame-slot-00", "", "persistent")
            };
            var nonXboxFiles = new[] { "preferences_data", "savegame-slot-00_containment", "savegame-slot-00_expedition", "savegame-slot-00_global",
                                       "savegame-slot-00_hub", "savegame-slot-00_maintenance", "savegame-slot-00_meta", "savegame-slot-00_persistent",
                                       "savegame-slot-00_research", "savegame-slot-01_global", "savegame-slot-01_hub" };

            return new TranslationSimulator(Profile("Control"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("preferences", "data")]
        [InlineData("savegame-slot-00", "global")]
        [InlineData("savegame-slot-00", "hub")]
        [InlineData("savegame-slot-00", "meta")]
        [InlineData("savegame-slot-00", "persistent")]
        public void Control_FileOnBothSides_MapsBothWays(string container, string blob)
        {
            TranslationSimulator game = Control();
            string file = container + "_" + blob;

            var toNonXbox = game.ToNonXbox(new XboxFile(container, "", blob));
            var toXbox = game.ToXbox(file);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(file, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container + " |  | " + blob, toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("containment")]
        [InlineData("expedition")]
        [InlineData("maintenance")]
        [InlineData("research")]
        public void Control_SteamOnlyPartOfASlot_IsAddedToTheSlotsContainer(string blob)
        {
            var toXbox = Control().ToXbox("savegame-slot-00_" + blob);

            Assert.Equal(Outcome.NewFile, toXbox.Outcome);
            Assert.Equal("savegame-slot-00", toXbox.XboxFile.ContainerName1);
            Assert.Equal(blob, toXbox.NewFileID);
        }

        [Fact]
        public void Control_SteamOnlySlot_NeedsAContainerOfItsOwnName()
        {
            Assert.Equal(Outcome.NoContainer, Control().ToXbox("savegame-slot-01_global").Outcome);

            string problem;
            string[] names = Profile("Control").Single().NewContainerNames("savegame-slot-01_global", TranslationSimulator.XboxProfileID, out problem);
            Assert.Equal(new[] { "savegame-slot-01", "" }, names);
        }

        [Fact]
        public void Control_XboxAchievements_AreLeftAlone()
        {
            // The Steam folder has no such file. Steam keeps the achievements itself.
            Assert.Equal(Outcome.NoTranslation, Control().ToNonXbox(new XboxFile("achievements", "", "data")).Outcome);
        }

        [Fact]
        public void Control_EpicLayout_IsNotMatched()
        {
            Assert.Equal(Outcome.NoTranslation, Control().ToXbox("savegame-slot-00\\global.chunk").Outcome);
        }

        #endregion

        #region LEGO Star Wars: The Skywalker Saga (#52)

        private static TranslationSimulator SkywalkerSaga()
        {
            // This game is in the game library already. The issue is about the Steam version, which
            // writes its files' names in capitals where the Xbox save has small letters.
            StoredGameLibrary library = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Resources.GameLibrary);
            IList<FileTranslation> translations = library.GameInfo.Single(g => g.PackageName == "WarnerBros.Interactive.LEGOStarWarsTheSkywalkerSag_ktmk1xygcecda").FileTranslations;

            // The tables in the screenshot posted in the issue. The Steam list stops before any slot
            // folder, so the slot file further down is written the same way as the ones shown.
            var xboxFiles = new[] { "achievements", "cutsceneprogress.blob", "dlcdata.blob", "profiledata.containerinfo", "settingsglobalprofile", "settingsplayer0profile" }
                .Select(blob => new XboxFile("profiledata", "profiledata", blob))
                .Concat(new[] { "achievementsystem.blob", "collectables.blob" }.Select(blob => new XboxFile("slot4", "slot4", blob)))
                .ToArray();
            var nonXboxFiles = new[] { "steam_autocloud.vdf", "PROFILEDATA\\ACHIEVEMENTS", "PROFILEDATA\\CUTSCENEPROGRESS.BLOB", "PROFILEDATA\\DLCDATA.BLOB",
                                       "PROFILEDATA\\PROFILEDATA.CONTAINERINFO", "PROFILEDATA\\SETTINGSGLOBALPROFILE", "PROFILEDATA\\SETTINGSPLAYER0PROFILE", "SLOT4\\COLLECTABLES.BLOB" };

            return new TranslationSimulator(translations, xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("profiledata", "achievements")]
        [InlineData("profiledata", "cutsceneprogress.blob")]
        [InlineData("profiledata", "profiledata.containerinfo")]
        [InlineData("slot4", "collectables.blob")]
        public void SkywalkerSaga_SteamNameInCapitals_MapsBothWays(string container, string blob)
        {
            TranslationSimulator game = SkywalkerSaga();
            string path = container.ToUpperInvariant() + "\\" + blob.ToUpperInvariant();

            var toXbox = game.ToXbox(path);
            var toNonXbox = game.ToNonXbox(new XboxFile(container, container, blob));

            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container + " | " + container + " | " + blob, toXbox.XboxFile.ToString());
            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(path, toNonXbox.RelativePath);
        }

        #endregion

        #region Mount & Blade II: Bannerlord (#44)

        private static TranslationSimulator Bannerlord()
        {
            // The tables posted in the issue, with some of the save names. Each Xbox save is a blob whose
            // name ends in .00, in one container. The other two containers hold settings.
            var xboxFiles = new[] { new XboxFile("Cache", "", "stats.json.00"), new XboxFile("Configs", "", "BannerlordConfig.txt.00"), new XboxFile("Configs", "", "BannerlordGameKeys.xml.00") }
                .Concat(new[] { "Cisd", "Why 2 s", "save001", "saveauto1", "saveauto2", "saveauto3" }.Select(name => new XboxFile("GameSaves", "", name + ".sav.00")))
                .ToArray();
            var nonXboxFiles = new[] { "save001.sav", "saveauto1.sav", "saveauto2.sav", "saveauto3.sav", "steam_autocloud.vdf" };

            return new TranslationSimulator(Profile("MountAndBladeIIBannerlord"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("save001")]
        [InlineData("saveauto1")]
        [InlineData("saveauto3")]
        public void Bannerlord_SaveOnBothSides_MapsBothWays(string name)
        {
            TranslationSimulator game = Bannerlord();

            var toNonXbox = game.ToNonXbox(new XboxFile("GameSaves", "", name + ".sav.00"));
            var toXbox = game.ToXbox(name + ".sav");

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal("GameSaves |  | " + name + ".sav.00", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("Cisd")]
        [InlineData("Why 2 s")]
        public void Bannerlord_XboxOnlySave_IsCreatedWithoutThePartNumber(string name)
        {
            var toNonXbox = Bannerlord().ToNonXbox(new XboxFile("GameSaves", "", name + ".sav.00"));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal(name + ".sav", toNonXbox.RelativePath);
        }

        [Fact]
        public void Bannerlord_SteamOnlySave_IsAddedAsItsFirstPart()
        {
            // A name made up for the test. The Steam saves in the table all have an Xbox copy.
            var toXbox = Bannerlord().ToXbox("save002.sav");

            Assert.Equal(Outcome.NewFile, toXbox.Outcome);
            Assert.Equal("GameSaves", toXbox.XboxFile.ContainerName1);
            Assert.Equal("save002.sav.00", toXbox.NewFileID);
        }

        [Theory]
        [InlineData("Cache", "stats.json.00")]
        [InlineData("Configs", "BannerlordConfig.txt.00")]
        [InlineData("GameSaves", "save001.sav.01")]
        public void Bannerlord_SettingsAndFurtherParts_AreLeftAlone(string container, string blob)
        {
            // No second part of a save is in the table. If a save has one, it must not become a file of its own.
            Assert.Equal(Outcome.NoTranslation, Bannerlord().ToNonXbox(new XboxFile(container, "", blob)).Outcome);
        }

        [Fact]
        public void Bannerlord_SteamCloudFile_IsNotSentToXbox()
        {
            Assert.Equal(Outcome.NoTranslation, Bannerlord().ToXbox("steam_autocloud.vdf").Outcome);
        }

        #endregion

        #region Return to Monkey Island (#49)

        private static readonly string[] MonkeyIslandXboxFiles = { "Prefs.json", "Savegame1.png", "Savegame1.save", "save.bin" };

        private static TranslationSimulator ReturnToMonkeyIsland()
        {
            // The tables in the screenshot posted in the issue. The Steam list runs on below the window.
            var xboxFiles = MonkeyIslandXboxFiles.Select(blob => new XboxFile("UserProfile", "UserProfile", blob)).ToArray();
            var nonXboxFiles = new[] { "Hintlog.txt", "Log.txt", "Playlog.txt", "Prefs.json", "save.bin", "Savegame1.png", "Savegame1.save",
                                       "Savegame2.png", "Savegame2.save", "Savegame3.png", "Savegame3.save" };

            return new TranslationSimulator(Profile("ReturnToMonkeyIsland"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("Prefs.json")]
        [InlineData("Savegame1.png")]
        [InlineData("Savegame1.save")]
        [InlineData("save.bin")]
        public void ReturnToMonkeyIsland_FileOnBothSides_MapsBothWays(string name)
        {
            TranslationSimulator game = ReturnToMonkeyIsland();

            var toNonXbox = game.ToNonXbox(new XboxFile("UserProfile", "UserProfile", name));
            var toXbox = game.ToXbox(name);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal("UserProfile | UserProfile | " + name, toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("Savegame2.save")]
        [InlineData("Savegame3.png")]
        public void ReturnToMonkeyIsland_SteamOnlySave_IsAddedToTheContainer(string name)
        {
            var toXbox = ReturnToMonkeyIsland().ToXbox(name);

            Assert.Equal(Outcome.NewFile, toXbox.Outcome);
            Assert.Equal("UserProfile", toXbox.XboxFile.ContainerName1);
            Assert.Equal(name, toXbox.NewFileID);
        }

        [Theory]
        [InlineData("Hintlog.txt")]
        [InlineData("Log.txt")]
        [InlineData("Playlog.txt")]
        public void ReturnToMonkeyIsland_Logs_AreNotSentToXbox(string name)
        {
            // The translation in the screenshot took any name, and would have put these in the Xbox save.
            Assert.Equal(Outcome.NoTranslation, ReturnToMonkeyIsland().ToXbox(name).Outcome);
        }

        #endregion

        #region Death Stranding (#61)

        private static TranslationSimulator DeathStranding()
        {
            // The tables posted in the issue. The Xbox save has 23 slots of each kind, but no quicksave15.
            // The paths on the other side are relative to the folder named with a long ID.
            var slots = new[] { "autosave", "manualsave", "quicksave" }
                .SelectMany(kind => Enumerable.Range(0, 23).Select(number => kind + number))
                .Where(slot => slot != "quicksave15");
            var xboxFiles = slots.Select(slot => new XboxFile(slot, "", "checkpoint.dat"))
                .Concat(new[] { "bindings.cfg", "game_settings.cfg", "profile.dat" }.Select(file => new XboxFile("profile", "", file)))
                .Concat(new[] { new XboxFile("transfer", "", "transfer.dat") })
                .ToArray();
            var nonXboxFiles = new[] { "manualsave0\\checkpoint.dat", "profile\\bindings.cfg", "profile\\game_settings.cfg", "profile\\profile.dat", "quicksave0\\checkpoint.dat" };

            return new TranslationSimulator(Profile("DeathStranding"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("manualsave0", "checkpoint.dat")]
        [InlineData("quicksave0", "checkpoint.dat")]
        [InlineData("profile", "bindings.cfg")]
        [InlineData("profile", "game_settings.cfg")]
        [InlineData("profile", "profile.dat")]
        public void DeathStranding_FileOnBothSides_MapsBothWays(string container, string blob)
        {
            TranslationSimulator game = DeathStranding();
            string path = container + "\\" + blob;

            var toNonXbox = game.ToNonXbox(new XboxFile(container, "", blob));
            var toXbox = game.ToXbox(path);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(path, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container + " |  | " + blob, toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("autosave3")]
        [InlineData("manualsave22")]
        public void DeathStranding_XboxOnlySlot_IsCreatedInAFolderOfItsName(string slot)
        {
            var toNonXbox = DeathStranding().ToNonXbox(new XboxFile(slot, "", "checkpoint.dat"));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal(slot + "\\checkpoint.dat", toNonXbox.RelativePath);
        }

        [Fact]
        public void DeathStranding_SlotWithNoXboxContainer_NeedsOneOfItsOwnName()
        {
            Assert.Equal(Outcome.NoContainer, DeathStranding().ToXbox("quicksave15\\checkpoint.dat").Outcome);

            string problem;
            string[] names = Profile("DeathStranding").First().NewContainerNames("quicksave15\\checkpoint.dat", TranslationSimulator.XboxProfileID, out problem);
            Assert.Equal(new[] { "quicksave15", "" }, names);
        }

        [Fact]
        public void DeathStranding_XboxTransferFile_IsLeftAlone()
        {
            Assert.Equal(Outcome.NoTranslation, DeathStranding().ToNonXbox(new XboxFile("transfer", "", "transfer.dat")).Outcome);
        }

        #endregion

        #region Persona 4 Golden (#37)

        private static TranslationSimulator Persona4Golden()
        {
            // The tables posted in the issue. Each file is a container of its own name, holding one blob, "progress".
            var xboxFiles = new[] { "P4G.ini", "data0001.bin", "data0001.binslot", "system.bin", "system.binslot" }
                .Select(container => new XboxFile(container, "", "progress")).ToArray();
            var nonXboxFiles = new[] { "data0001.bin", "data0001.binslot", "data0002.bin", "data0002.binslot", "system.bin", "system.binslot" };

            return new TranslationSimulator(Profile("Persona4Golden"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("data0001.bin")]
        [InlineData("data0001.binslot")]
        public void Persona4Golden_SaveOnBothSides_MapsBothWays(string name)
        {
            // The two translations a user in the thread reported working.
            TranslationSimulator game = Persona4Golden();

            var toNonXbox = game.ToNonXbox(new XboxFile(name, "", "progress"));
            var toXbox = game.ToXbox(name);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(name, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(name + " |  | progress", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("data0002.bin")]
        [InlineData("data0002.binslot")]
        public void Persona4Golden_SteamOnlySave_NeedsAContainerOfItsOwnName(string name)
        {
            Assert.Equal(Outcome.NoContainer, Persona4Golden().ToXbox(name).Outcome);

            string problem;
            FileTranslation translation = Profile("Persona4Golden").First(t => System.Text.RegularExpressions.Regex.IsMatch(name, t.NonXboxFilenameRegex));
            Assert.Equal(new[] { name, "" }, translation.NewContainerNames(name, TranslationSimulator.XboxProfileID, out problem));
        }

        [Theory]
        [InlineData("P4G.ini")]
        [InlineData("system.bin")]
        [InlineData("system.binslot")]
        public void Persona4Golden_FilesTheTranslationsLeaveOut_AreLeftAlone(string container)
        {
            // Not part of what was reported working, so not copied.
            Assert.Equal(Outcome.NoTranslation, Persona4Golden().ToNonXbox(new XboxFile(container, "", "progress")).Outcome);
        }

        #endregion

        #region TCG Card Shop Simulator (#254)

        private static TranslationSimulator TCGCardShopSimulator()
        {
            // The tables posted in the issue. Each Xbox slot is a container GameData_N, named twice, holding
            // one blob, PlayerData. The Steam folder has savedGames_Release0.gd and savedGames_Release1.gd,
            // with logs, settings and Unity analytics beside them.
            var xboxFiles = new[] { "GameData_0", "GameData_1", "GameData_2", "PlayerPrefs", "Settings" }
                .Select(container => new XboxFile(container, container, "PlayerData")).ToArray();
            var nonXboxFiles = new[]
            {
                "Player-prev.log",
                "Player.log",
                "SavedData.json",
                "savedGames_KeybindSetting.gd",
                "savedGames_Release0.gd",
                "savedGames_Release0.json",
                "savedGames_Release1.gd",
                "savedGames_ReleaseBackupFile0.json",
                "steam_autocloud.vdf",
                "Unity\\73bc2d01-2024-47ff-8ef6-312e0a9ff94a\\Analytics\\config",
                "Unity\\73bc2d01-2024-47ff-8ef6-312e0a9ff94a\\Analytics\\values",
                "Unity\\ShaderVariantAnalytics\\ShaderRuntimeInfoEvent.json"
            };

            return new TranslationSimulator(Profile("TCGCardShopSimulator"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        public void TCGCardShopSimulator_SlotOnBothSides_MapsBothWays(string slot)
        {
            TranslationSimulator game = TCGCardShopSimulator();
            string container = "GameData_" + slot;
            string path = "savedGames_Release" + slot + ".gd";

            var toNonXbox = game.ToNonXbox(new XboxFile(container, container, "PlayerData"));
            var toXbox = game.ToXbox(path);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(path, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container + " | " + container + " | PlayerData", toXbox.XboxFile.ToString());
        }

        [Fact]
        public void TCGCardShopSimulator_XboxOnlySlot_IsCreatedOnSteam()
        {
            var toNonXbox = TCGCardShopSimulator().ToNonXbox(new XboxFile("GameData_2", "GameData_2", "PlayerData"));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal("savedGames_Release2.gd", toNonXbox.RelativePath);
        }

        [Theory]
        [InlineData("PlayerPrefs")]
        [InlineData("Settings")]
        public void TCGCardShopSimulator_XboxSettings_AreLeftAlone(string container)
        {
            Assert.Equal(Outcome.NoTranslation, TCGCardShopSimulator().ToNonXbox(new XboxFile(container, container, "PlayerData")).Outcome);
        }

        [Theory]
        [InlineData("savedGames_Release0.json")]
        [InlineData("savedGames_ReleaseBackupFile0.json")]
        [InlineData("savedGames_KeybindSetting.gd")]
        [InlineData("SavedData.json")]
        [InlineData("Player.log")]
        public void TCGCardShopSimulator_OtherSteamFiles_AreLeftAlone(string path)
        {
            Assert.Equal(Outcome.NoTranslation, TCGCardShopSimulator().ToXbox(path).Outcome);
        }

        #endregion

        #region Resident Evil Village (#262) and Resident Evil 2 (#263)

        // Each Game Pass container, SaveDataNNN, holds three or four blobs with numbers for names, and
        // the Steam version keeps one dataNNN.bin for it. Blobs 1230789541 and 2246640833 are in every
        // container of both games. Which blob is the save is not known, so the translation takes
        // 1230789541 alone, to be tried; 2246640833 is the next candidate. Each container must give
        // exactly one blob, or several would be written onto the same Steam file.

        private static XboxFile[] Containers(params string[] rows)
        {
            // "SaveData000: 1230789541 2084659331" is a container and its blobs.
            return rows.SelectMany(row =>
            {
                string[] parts = row.Split(':');
                return parts[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(blob => new XboxFile(parts[0], "", blob));
            }).ToArray();
        }

        private static TranslationSimulator ResidentEvilVillage()
        {
            // The tables posted in #262.
            var xboxFiles = Containers(
                "SaveData00-1: 1230789541 2246640833 3789133773 926117261",
                "SaveData000: 1230789541 2084659331 2246640833",
                "SaveData001Slot: 1230789541 2084659331 2246640833",
                "SaveData002Slot: 1230789541 2084659331 2246640833",
                "SaveData003Slot: 1230789541 2084659331 2246640833",
                "SaveData004Slot: 1230789541 2084659331 2246640833",
                "SaveData030Slot: 1230789541 2084659331 2246640833",
                "SaveData040Slot: 1230789541 2246640833 3789133773",
                "SaveData041Slot: 1230789541 2084659331 2246640833",
                "SaveData042Slot: 1230789541 2084659331 2246640833",
                "SaveData043Slot: 1230789541 2084659331 2246640833",
                "SaveData044Slot: 1230789541 2084659331 2246640833");
            var nonXboxFiles = new[] { "data00-1.bin", "data030Slot.bin" };

            return new TranslationSimulator(Profile("ResidentEvilVillage"), xboxFiles, nonXboxFiles);
        }

        private static TranslationSimulator ResidentEvil2()
        {
            // The tables posted in #263.
            var xboxFiles = Containers(
                "SaveData00-1: 1230789541 2246640833 3789133773",
                "SaveData000: 1230789541 1779442705 2084659331 2246640833",
                "SaveData001Slot: 1230789541 1779442705 2084659331 2246640833",
                "SaveData021Slot: 1230789541 2246640833 4251998130");
            var nonXboxFiles = new[] { "data00-1.bin", "data000.bin", "data021Slot.bin" };

            return new TranslationSimulator(Profile("ResidentEvil2"), xboxFiles, nonXboxFiles);
        }

        private static TranslationSimulator ResidentEvil(string game)
        {
            return game == "ResidentEvilVillage" ? ResidentEvilVillage() : ResidentEvil2();
        }

        [Theory]
        [InlineData("ResidentEvilVillage", "00-1")]
        [InlineData("ResidentEvilVillage", "030Slot")]
        [InlineData("ResidentEvil2", "00-1")]
        [InlineData("ResidentEvil2", "000")]
        [InlineData("ResidentEvil2", "021Slot")]
        public void ResidentEvil_SaveOnBothSides_MapsBothWays(string game, string save)
        {
            TranslationSimulator simulator = ResidentEvil(game);
            string container = "SaveData" + save;
            string path = "data" + save + ".bin";

            var toNonXbox = simulator.ToNonXbox(new XboxFile(container, "", "1230789541"));
            var toXbox = simulator.ToXbox(path);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(path, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(container + " |  | 1230789541", toXbox.XboxFile.ToString());
        }

        [Theory]
        [InlineData("ResidentEvilVillage", "000")]
        [InlineData("ResidentEvilVillage", "001Slot")]
        [InlineData("ResidentEvilVillage", "044Slot")]
        [InlineData("ResidentEvil2", "001Slot")]
        public void ResidentEvil_XboxOnlySave_IsCreatedOnSteam(string game, string save)
        {
            var toNonXbox = ResidentEvil(game).ToNonXbox(new XboxFile("SaveData" + save, "", "1230789541"));

            Assert.Equal(Outcome.NewFile, toNonXbox.Outcome);
            Assert.Equal("data" + save + ".bin", toNonXbox.RelativePath);
        }

        [Theory]
        [InlineData("ResidentEvilVillage")]
        [InlineData("ResidentEvil2")]
        public void ResidentEvil_EachContainer_GivesOneBlobOnly(string game)
        {
            TranslationSimulator simulator = ResidentEvil(game);
            XboxFile[] xboxFiles = game == "ResidentEvilVillage"
                ? Containers("SaveData00-1: 1230789541 2246640833 3789133773 926117261", "SaveData000: 1230789541 2084659331 2246640833", "SaveData040Slot: 1230789541 2246640833 3789133773")
                : Containers("SaveData000: 1230789541 1779442705 2084659331 2246640833", "SaveData021Slot: 1230789541 2246640833 4251998130");

            foreach (var container in xboxFiles.GroupBy(f => f.ContainerName1))
            {
                var taken = container.Where(f => simulator.ToNonXbox(f).Outcome != Outcome.NoTranslation).Select(f => f.FileID).ToList();
                Assert.Equal(new[] { "1230789541" }, taken);
            }
        }

        #endregion

        #region ONE PIECE PIRATE WARRIORS 4 (#267)

        private static TranslationSimulator OnePiecePirateWarriors4()
        {
            // The tables posted in the issue. Each Game Pass container holds one blob, WholeData, and has a
            // second name of numbers that differs between containers, so the translation matches any second
            // name. The Steam folder has one file for the save slot and one for the user's data.
            var xboxFiles = new[]
            {
                new XboxFile("SlotData0", "0000009 000 39 005 000 012", "WholeData"),
                new XboxFile("SystemData", "0000000 000 39 005 000 012", "WholeData")
            };
            var nonXboxFiles = new[] { "OP4WINSLOT0000.dat", "OP4WINUSER.dat", "steam_autocloud.vdf" };

            return new TranslationSimulator(Profile("OnePiecePirateWarriors4"), xboxFiles, nonXboxFiles);
        }

        [Theory]
        [InlineData("OP4WINSLOT0000.dat", "SlotData0 | 0000009 000 39 005 000 012 | WholeData")]
        [InlineData("OP4WINUSER.dat", "SystemData | 0000000 000 39 005 000 012 | WholeData")]
        public void OnePiecePirateWarriors4_FileOnBothSides_MapsBothWays(string path, string xboxFile)
        {
            TranslationSimulator game = OnePiecePirateWarriors4();
            string[] names = xboxFile.Split(new[] { " | " }, System.StringSplitOptions.None);

            var toNonXbox = game.ToNonXbox(new XboxFile(names[0], names[1], names[2]));
            var toXbox = game.ToXbox(path);

            Assert.Equal(Outcome.ExistingFile, toNonXbox.Outcome);
            Assert.Equal(path, toNonXbox.RelativePath);
            Assert.Equal(Outcome.ExistingFile, toXbox.Outcome);
            Assert.Equal(xboxFile, toXbox.XboxFile.ToString());
        }

        [Fact]
        public void OnePiecePirateWarriors4_SteamOnlySlot_HasNoContainerToName()
        {
            // The second name of a new container is not known, so a slot the Xbox save lacks cannot be made.
            Assert.Equal(Outcome.NoContainer, OnePiecePirateWarriors4().ToXbox("OP4WINSLOT0001.dat").Outcome);
        }

        [Fact]
        public void OnePiecePirateWarriors4_SteamCloudFile_IsLeftAlone()
        {
            Assert.Equal(Outcome.NoTranslation, OnePiecePirateWarriors4().ToXbox("steam_autocloud.vdf").Outcome);
        }

        #endregion
    }
}
