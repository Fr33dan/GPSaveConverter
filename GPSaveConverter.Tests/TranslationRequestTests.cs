using System;
using System.Collections.Generic;
using Xunit;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// The GitHub page that File > Request a Translation opens.
    /// </summary>
    public class TranslationRequestTests
    {
        private const string Tables = "Game Name:Test Game\r\n## Xbox Files:\r\n| SaveGame |  | SaveSlot0 |\r\n";

        [Fact]
        public void IssueAddress_OpensTheFormWithItsFieldsFilledIn()
        {
            bool tablesIncluded;

            string address = TranslationRequest.IssueAddress("Test Game: Remastered", "0.4.17.0", Tables, out tablesIncluded);

            Assert.True(tablesIncluded);
            Assert.StartsWith("https://github.com/Fr33dan/GPSaveConverter/issues/new?template=translation-request.yml&", address);
            Assert.Contains("&title=Translation%20request%3A%20Test%20Game%3A%20Remastered", address);
            Assert.Contains("&game=Test%20Game%3A%20Remastered", address);
            Assert.Contains("&version=0.4.17.0", address);
            Assert.Contains("&tables=" + Uri.EscapeDataString(Tables), address);
        }

        [Fact]
        public void IssueAddress_TablesTooLongForTheAddress_AreLeftOut()
        {
            bool tablesIncluded;
            string longTables = new string('x', TranslationRequest.LongestAddress);

            string address = TranslationRequest.IssueAddress("Test Game", "0.4.17.0", longTables, out tablesIncluded);

            Assert.False(tablesIncluded);
            Assert.DoesNotContain("&tables=", address);
            Assert.Contains("&game=Test%20Game", address);
        }

        private static readonly KeyValuePair<string, string>[] Folders =
        {
            new KeyValuePair<string, string>("%LOCALAPPDATA%", @"C:\Users\Jo\AppData\Local"),
            new KeyValuePair<string, string>("%APPDATA%", @"C:\Users\Jo\AppData\Roaming"),
            new KeyValuePair<string, string>("%USERPROFILE%", @"C:\Users\Jo")
        };

        [Theory]
        [InlineData(@"Non-Xbox save location: C:\Users\Jo\AppData\Local\Game\Saved\", @"Non-Xbox save location: %LOCALAPPDATA%\Game\Saved\")]
        [InlineData(@"Non-Xbox save location: C:\Users\Jo\AppData\Roaming\SEGA\", @"Non-Xbox save location: %APPDATA%\SEGA\")]
        [InlineData(@"Non-Xbox save location: C:\Users\Jo\Documents\My Games\", @"Non-Xbox save location: %USERPROFILE%\Documents\My Games\")]
        [InlineData(@"Non-Xbox save location: c:\users\JO\Desktop", @"Non-Xbox save location: %USERPROFILE%\Desktop")]
        [InlineData("Non-Xbox save location: C:\\Users\\Jo\r\n", "Non-Xbox save location: %USERPROFILE%\r\n")]
        public void HideUserFolders_WritesTheUsersFoldersAsTheirVariables(string text, string expected)
        {
            Assert.Equal(expected, TranslationRequest.HideUserFolders(text, Folders));
        }

        [Theory]
        [InlineData(@"Non-Xbox save location: C:\Users\Joe\Saves\")]
        [InlineData(@"Non-Xbox save location: D:\Games\Steam\userdata\8056884\234650\remote\")]
        [InlineData(@"Non-Xbox save location: <Steam-folder>\userdata\<user-id>\1113000\remote\")]
        public void HideUserFolders_LeavesOtherPathsAlone(string text)
        {
            Assert.Equal(text, TranslationRequest.HideUserFolders(text, Folders));
        }
    }
}
