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
    }
}
