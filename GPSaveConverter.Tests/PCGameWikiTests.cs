using System.Collections.Generic;
using Xunit;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class PCGameWikiTests
    {
        [Theory]
        [InlineData("{{p|userprofile}}", "%USERPROFILE%")]
        [InlineData("{{p|appdata}}", "%APPDATA%")]
        [InlineData("{{p|localappdata}}", "%LOCALAPPDATA%")]
        [InlineData("{{p|programdata}}", "%PROGRAMDATA%")]
        [InlineData("{{p|uid}}", "<user-id>")]
        [InlineData("{{p|steam}}", "<Steam-folder>")]
        public void NameSubstitution_ReplacesFolderTokens(string input, string expected)
        {
            string result = PCGameWiki.NameSubstitution(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void NameSubstitution_ReplacesMultipleTokens()
        {
            string input = "{{p|appdata}}\\SomeGame\\{{p|uid}}";

            string result = PCGameWiki.NameSubstitution(input);

            Assert.Equal("%APPDATA%\\SomeGame\\<user-id>", result);
        }

        [Fact]
        public void NameSubstitution_IsCaseInsensitive()
        {
            string result = PCGameWiki.NameSubstitution("{{P|APPDATA}}");

            Assert.Equal("%APPDATA%", result);
        }

        [Fact]
        public void NameSubstitution_NoTokens_ReturnsUnchanged()
        {
            string input = "C:\\Users\\TestUser\\AppData";

            string result = PCGameWiki.NameSubstitution(input);

            Assert.Equal(input, result);
        }

        [Fact]
        public void ParseWikiTable_SingleEntry_ReturnsOneMapping()
        {
            string wikiText = "{{Game data/saves|%APPDATA%|\\SomeGame\\save.dat}}";

            Dictionary<string, string> result = PCGameWiki.parseWikiTable(wikiText);

            Assert.Single(result);
            Assert.Equal("\\SomeGame\\save.dat", result["%APPDATA%"]);
        }

        [Fact]
        public void ParseWikiTable_MultipleEntries_ReturnsAll()
        {
            string wikiText =
                "{{Game data/saves|%APPDATA%|\\Game1\\save.dat}}" +
                "Some text between" +
                "{{Game data/saves|%LOCALAPPDATA%|\\Game2\\config.ini}}";

            Dictionary<string, string> result = PCGameWiki.parseWikiTable(wikiText);

            Assert.Equal(2, result.Count);
            Assert.Equal("\\Game1\\save.dat", result["%APPDATA%"]);
            Assert.Equal("\\Game2\\config.ini", result["%LOCALAPPDATA%"]);
        }

        [Fact]
        public void ParseWikiTable_NoEntries_ReturnsEmptyDictionary()
        {
            string wikiText = "This is a wiki page with no save data entries.";

            Dictionary<string, string> result = PCGameWiki.parseWikiTable(wikiText);

            Assert.Empty(result);
        }

        [Fact]
        public void ParseWikiTable_NestedBraces_HandlesCorrectly()
        {
            // Nested {{}} inside the entry — parser must find the matching closing braces
            string wikiText = "{{Game data/saves|{{p|appdata}}|\\SomeGame\\save.dat}}";

            Dictionary<string, string> result = PCGameWiki.parseWikiTable(wikiText);

            Assert.Single(result);
            Assert.Equal("\\SomeGame\\save.dat", result["%APPDATA%"]);
        }

        [Theory]
        [InlineData("{{p|userprofile\\Documents}}\\Saved Games\\Hades\\", "%USERPROFILE%\\Documents\\Saved Games\\Hades\\")]
        [InlineData("{{P|userprofile\\Documents}}\\My Games\\FINAL FANTASY VII REMAKE\\Steam\\", "%USERPROFILE%\\Documents\\My Games\\FINAL FANTASY VII REMAKE\\Steam\\")]
        [InlineData("{{p|userprofile\\appdata\\locallow}}\\CoinCrewGames\\Escape Academy\\", "%USERPROFILE%\\appdata\\locallow\\CoinCrewGames\\Escape Academy\\")]
        public void NameSubstitution_FolderBelowAKnownOneInTheSameTemplate_IsKept(string input, string expected)
        {
            // How the wiki writes the Documents and LocalLow folders.
            Assert.Equal(expected, PCGameWiki.NameSubstitution(input));
        }

        [Theory]
        [InlineData("{{p|game}}\\Saves\\")]
        [InlineData("{{p|osxhome}}/Library/Application Support/Celeste/Saves")]
        [InlineData("{{p|userprofilex}}\\Saves\\")]
        public void NameSubstitution_TemplateItDoesNotKnow_IsLeftAsItIs(string input)
        {
            Assert.Equal(input, PCGameWiki.NameSubstitution(input));
        }

        #region The save location rows of real pages, as pcgamingwiki.com served them on 2026-10-10

        private const string Palworld = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Windows|{{p|localappdata}}\\Pal\\Saved\\SaveGames\\{{P|uid}}\\}}\n"
            + "{{Game data/saves|Microsoft Store|{{p|localappdata}}\\Packages\\PocketpairInc.Palworld_ad4psfrxyesvt\\SystemAppData\\wgs\\{{P|uid}}\\}}\n}}";

        private const string Hades = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Windows|{{p|userprofile\\Documents}}\\Saved Games\\Hades\\}}\n"
            + "{{Game data/saves|Microsoft Store|{{P|localappdata}}\\Packages\\SupergiantGamesLLC.Hades_q53c1yqmx7pha\\SystemAppData\\wgs}}\n"
            + "{{Game data/saves|OS X|{{P|osxhome}}/Library/Application Support/Supergiant Games/Hades/}}\n}}";

        private const string HollowKnight = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Windows|{{p|userprofile}}\\AppData\\LocalLow\\Team Cherry\\Hollow Knight\\*.dat | {{p|userprofile}}\\AppData\\LocalLow\\Team Cherry\\Hollow Knight\\*.bak}}\n"
            + "{{Game data/saves|Microsoft Store|{{p|programdata}}\\Packages\\TeamCherry.15373CD61C66B_y4jvztpgccj42\\SystemAppData\\wgs}}\n"
            + "{{Game data/saves|OS X|{{p|osxhome}}/Library/Application Support/unity.Team Cherry.Hollow Knight/}}\n"
            + "{{Game data/saves|Linux|{{p|xdgconfighome}}/unity3d/Team Cherry/Hollow Knight/*.dat}}\n}}\n"
            + "{{ii}} A note follows the table, with a [[#Game data|link]] and a reference.<ref>\n"
            + "{{Refurl|url=https://example.com/a|title=A title with a | in it?|date=May 2023}}</ref>";

        private const string DeadCells = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Windows|{{p|game}}\\save\\user_*.dat|{{p|game}}\\save\\customGameData_*.json}}\n"
            + "{{Game data/saves|Microsoft Store|{{P|localappdata}}\\Packages\\MotionTwin.DeadCellsWin10_rtjy889c6zgtg\\LocalCache\\Local\\MotionTwin\\DeadCells|{{P|localappdata}}\\MotionTwin\\DeadCells\\user_*.dat|{{P|localappdata}}\\MotionTwin\\DeadCells\\customGameData_*.json}}\n"
            + "{{Game data/saves|Steam|{{p|steam}}\\userdata\\{{p|uid}}\\588650\\remote\\user_*.dat|{{p|steam}}\\userdata\\{{p|uid}}\\588650\\remote\\customGameData_*.json}}\n"
            + "{{Game data/saves|OS X|{{p|game}}/save/user_*.dat|{{p|game}}/save/customGameData_*.json}}\n"
            + "{{Game data/saves|Linux|{{p|game}}/save/user_*.dat|{{p|game}}/save/customGameData_*.json}}\n}}";

        private const string Celeste = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Windows|{{p|game}}\\Saves\\*.celeste|{{p|game}}\\Saves\\debug.celeste}}\n"
            + "{{Game data/saves|Microsoft Store|{{p|localappdata}}\\Packages\\MattMakesGamesInc.Celeste_79daxvg0dq3v6\\SystemAppData\\wgs\\}}\n"
            + "{{Game data/saves|OS X|{{p|osxhome}}/Library/Application Support/Celeste/Saves}}\n"
            + "{{Game data/saves|Linux|{{p|xdgdatahome}}/Celeste/Saves/*.celeste}}\n}}\n"
            + "{{ii}} A note follows the table, with {{code|*}} in it.";

        private const string AtomicHeart = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Windows|{{P|localappdata}}\\AtomicHeart\\Saved\\SaveGames}}\n"
            + "{{Game data/saves|Microsoft Store|{{P|localappdata}}\\Packages\\FocusHomeInteractiveSA.579645D26CFD_4hny5m903y3g0\\SystemAppData\\wgs\\{{P|uid}}\\}}\n}}";

        private const string FinalFantasy7Remake = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Epic Games Store|{{P|userprofile\\Documents}}\\My Games\\FINAL FANTASY VII REMAKE\\EOS\\}}\n"
            + "{{Game data/saves|Steam|{{P|userprofile\\Documents}}\\My Games\\FINAL FANTASY VII REMAKE\\Steam\\}}\n"
            + "{{Game data/saves|Microsoft Store|{{P|LOCALAPPDATA}}\\Packages\\39EA002F.EXED1_n746a19ndrrjg\\SystemAppData\\wgs\\}}\n}}";

        private const string ForzaHorizon4 = "===Save game data location===\n{{Game data| \n"
            + "{{Game data/saves|Microsoft Store|{{p|localappdata}}\\Packages\\Microsoft.SunriseBaseGame_8wekyb3d8bbwe\\SystemAppData\\wgs\\}}\n\n"
            + "{{Game data/saves|Steam|{{p|Steam}}\\userdata\\{{p|uid}}\\1293830\\remote\\}}\n}}";

        private const string ClairObscur = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Epic Games Launcher|{{p|localappdata}}\\Sandfall\\Saved\\SaveGames\\{{p|uid}}\\*.sav}}\n"
            + "{{Game data/saves|Steam|{{p|localappdata}}\\Sandfall\\Saved\\SaveGames\\{{p|uid}}\\*.sav}}\n"
            + "{{Game data/saves|Microsoft Store|{{P|localappdata}}\\Packages\\KeplerInteractive.Expedition33_ymj30pw7xe604\\SystemAppData\\wgs\\}}\n}}";

        private const string CitiesSkylines = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|Windows|{{P|localappdata}}\\Colossal Order\\Cities_Skylines\\Saves\\}}\n"
            + "{{Game data/saves|Microsoft Store|}}\n"
            + "{{Game data/saves|OS X|{{p|osxhome}}/Library/Application Support/Colossal Order/Cities_Skylines/Saves/}}\n"
            + "{{Game data/saves|Linux|{{P|xdgdatahome}}/Colossal Order/Cities_Skylines/Saves/}}\n}}\n{{XDG|true}}";

        private const string APlagueTaleRequiem = "===Save game data location===\n{{Game data|\n"
            + "{{Game data/saves|GOG.com|{{p|localappdata}}\\GOG.com\\Galaxy\\Applications\\55243620139303375\\Storage\\Shared\\Files}}\n"
            + "{{Game data/saves|Microsoft Store|{{p|localappdata}}\\Packages\\FocusHomeInteractiveSA.APlagueTaleRequiem-Windows_4hny5m903y3g0\\SystemAppData\\wgs\\{{p|uid}}}}\n"
            + "{{Game data/saves|Steam|{{P|steam}}\\userdata\\{{p|uid}}\\1182900\\remote}}\n}}";

        [Theory]
        // A profile folder in the Windows row: the folder list has to be offered.
        [InlineData(Palworld, "%LOCALAPPDATA%\\Pal\\Saved\\SaveGames\\<user-id>\\", true)]
        // The Documents folder, written inside the template.
        [InlineData(Hades, "%USERPROFILE%\\Documents\\Saved Games\\Hades\\", false)]
        // File names with a wildcard after the folder, and a second path after " | ".
        [InlineData(HollowKnight, "%USERPROFILE%\\AppData\\LocalLow\\Team Cherry\\Hollow Knight\\", false)]
        // The Windows row is the game's own folder, which cannot be known. The Steam row can be used.
        [InlineData(DeadCells, "<Steam-folder>\\userdata\\<user-id>\\588650\\remote\\", true)]
        // No separator at the end.
        [InlineData(AtomicHeart, "%LOCALAPPDATA%\\AtomicHeart\\Saved\\SaveGames\\", false)]
        // A Steam row with no user folder in it: no profile to pick.
        [InlineData(FinalFantasy7Remake, "%USERPROFILE%\\Documents\\My Games\\FINAL FANTASY VII REMAKE\\Steam\\", false)]
        [InlineData(ForzaHorizon4, "<Steam-folder>\\userdata\\<user-id>\\1293830\\remote\\", true)]
        [InlineData(ClairObscur, "%LOCALAPPDATA%\\Sandfall\\Saved\\SaveGames\\<user-id>\\", true)]
        [InlineData(CitiesSkylines, "%LOCALAPPDATA%\\Colossal Order\\Cities_Skylines\\Saves\\", false)]
        // A template closed right before the row is: "{{p|uid}}}}".
        [InlineData(APlagueTaleRequiem, "<Steam-folder>\\userdata\\<user-id>\\1182900\\remote\\", true)]
        public void ChooseSaveLocation_RealPage_GivesTheFolder(string section, string expectedLocation, bool expectedProfile)
        {
            bool usesProfile;
            string location = PCGameWiki.chooseSaveLocation(section, out usesProfile);

            Assert.Equal(expectedLocation, location);
            Assert.Equal(expectedProfile, usesProfile);
        }

        [Fact]
        public void ChooseSaveLocation_OnlyTheGamesOwnFolderIsGiven_GivesNothing()
        {
            // It used to give "{{p", which was then shown as a folder that does not exist.
            bool usesProfile;

            Assert.Null(PCGameWiki.chooseSaveLocation(Celeste, out usesProfile));
            Assert.False(usesProfile);
        }

        [Fact]
        public void ParseWikiTable_RealPage_ReadsEveryRowItCan()
        {
            Dictionary<string, string> result = PCGameWiki.parseWikiTable(DeadCells);

            // Windows, OS X and Linux are the game's own folder. The Microsoft Store row has three paths.
            Assert.Equal(new[] { "Microsoft Store", "Steam" }, result.Keys);
            Assert.Equal("%LOCALAPPDATA%\\Packages\\MotionTwin.DeadCellsWin10_rtjy889c6zgtg\\LocalCache\\Local\\MotionTwin\\DeadCells", result["Microsoft Store"]);
            Assert.Equal("<Steam-folder>\\userdata\\<user-id>\\588650\\remote\\user_*.dat", result["Steam"]);
        }

        #endregion

        [Fact]
        public void ParseWikiTable_SamePlatformTwice_KeepsTheFirst()
        {
            // Adding the second one to the table used to throw.
            string wikiText = "{{Game data/saves|Windows|{{p|appdata}}\\Game\\}}\n{{Game data/saves|Windows|{{p|localappdata}}\\Game\\}}";

            Dictionary<string, string> result = PCGameWiki.parseWikiTable(wikiText);

            Assert.Equal("%APPDATA%\\Game\\", Assert.Single(result).Value);
        }

        [Fact]
        public void ParseWikiTable_RowThatIsNeverClosed_IsLeftOutAndTheNextIsStillRead()
        {
            string wikiText = "{{Game data/saves|Windows|{{p|appdata}}\\Game\\\n{{Game data/saves|Steam|{{p|steam}}\\userdata\\{{p|uid}}\\1\\remote\\}}";

            Dictionary<string, string> result = PCGameWiki.parseWikiTable(wikiText);

            Assert.Equal("<Steam-folder>\\userdata\\<user-id>\\1\\remote\\", Assert.Single(result).Value);
        }

        [Theory]
        [InlineData("{{Game data/saves|Windows}}")]
        [InlineData("{{Game data/saves|Windows|}}")]
        [InlineData("{{Game data/saves|Windows|   }}")]
        [InlineData("{{Game data/saves|")]
        [InlineData("{{Game data/saves|Windows|{{p|appdata}}\\Game")]
        public void ParseWikiTable_RowWithoutAPath_IsLeftOut(string wikiText)
        {
            Assert.Empty(PCGameWiki.parseWikiTable(wikiText));
        }

        [Fact]
        public void ParseWikiTable_FirstPathCannotBeRead_TakesTheNextOne()
        {
            string wikiText = "{{Game data/saves|Windows|{{p|game}}\\Saves\\|{{p|userprofile\\Documents}}\\Game\\Saves\\}}";

            Dictionary<string, string> result = PCGameWiki.parseWikiTable(wikiText);

            Assert.Equal("%USERPROFILE%\\Documents\\Game\\Saves\\", result["Windows"]);
        }
    }
}
