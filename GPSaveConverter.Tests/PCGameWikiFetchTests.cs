using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class PCGameWikiFetchTests
    {
        private readonly IHttpClient _httpClient;
        private readonly PCGameWiki _wiki;

        public PCGameWikiFetchTests()
        {
            _httpClient = Substitute.For<IHttpClient>();
            _wiki = new PCGameWiki(_httpClient);
        }

        [Fact]
        public async Task FetchSaveLocation_ValidPage_SetsBaseLocation()
        {
            var gameInfo = new GameInfo { Name = "Hades" };

            // Step 1: Query API returns page ID
            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=query")))
                .Returns(Task.FromResult("{\"query\":{\"pages\":[{\"pageid\":12345}]}}"));

            // Step 2: sections returns "Save game data location" section
            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=parse") && u.Contains("prop=sections")))
                .Returns(Task.FromResult("{\"parse\":{\"sections\":[{\"line\":\"Save game data location\",\"index\":\"3\"}]}}"));

            // Step 3: wikitext with save data
            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=parse") && u.Contains("prop=wikitext")))
                .Returns(Task.FromResult("{\"parse\":{\"wikitext\":\"{{Game data/saves|Windows|{{p|appdata}}\\\\Hades\\\\Saves}}\"}}"));

            await _wiki.FetchSaveLocation(gameInfo);

            // With the separator at the end, as the library and the folder picker write a location.
            Assert.Equal("%APPDATA%\\Hades\\Saves\\", gameInfo.BaseNonXboxSaveLocation);
            Assert.Null(gameInfo.TargetProfileTypes);
        }

        [Fact]
        public async Task FetchSaveLocation_MissingPage_DoesNotSetLocation()
        {
            var gameInfo = new GameInfo { Name = "NonExistentGame" };

            // Query API returns missing page
            _httpClient.DownloadStringAsync(Arg.Any<string>())
                .Returns(Task.FromResult("{\"query\":{\"pages\":[{\"missing\":true}]}}"));

            await _wiki.FetchSaveLocation(gameInfo);

            Assert.Null(gameInfo.BaseNonXboxSaveLocation);
        }

        [Fact]
        public async Task FetchSaveLocation_NoSaveSection_DoesNotSetLocation()
        {
            var gameInfo = new GameInfo { Name = "Hades" };

            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=query")))
                .Returns(Task.FromResult("{\"query\":{\"pages\":[{\"pageid\":12345}]}}"));

            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=parse")))
                .Returns(Task.FromResult("{\"parse\":{\"sections\":[{\"line\":\"Gameplay\",\"index\":\"1\"}]}}"));

            await _wiki.FetchSaveLocation(gameInfo);

            Assert.Null(gameInfo.BaseNonXboxSaveLocation);
        }

        [Fact]
        public async Task FetchSaveLocation_SteamOnly_SetsProfileType()
        {
            var gameInfo = new GameInfo { Name = "SteamGame" };

            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=query")))
                .Returns(Task.FromResult("{\"query\":{\"pages\":[{\"pageid\":99}]}}"));

            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=parse") && u.Contains("prop=sections")))
                .Returns(Task.FromResult("{\"parse\":{\"sections\":[{\"line\":\"Save game data location\",\"index\":\"2\"}]}}"));

            // Only Steam entry, no Windows entry
            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=parse") && u.Contains("prop=wikitext")))
                .Returns(Task.FromResult("{\"parse\":{\"wikitext\":\"{{Game data/saves|Steam|{{p|steam}}\\\\userdata\\\\{{p|uid}}\\\\1240440\\\\remote}}\"}}"));

            await _wiki.FetchSaveLocation(gameInfo);

            Assert.Equal("<Steam-folder>\\userdata\\<user-id>\\1240440\\remote\\", gameInfo.BaseNonXboxSaveLocation);
            Assert.NotNull(gameInfo.TargetProfileTypes);
            Assert.Single(gameInfo.TargetProfileTypes);
            Assert.Equal(NonXboxProfile.ProfileType.Steam, gameInfo.TargetProfileTypes[0]);
            Assert.Single(gameInfo.TargetProfiles);
        }

        private void PageWithSaveSection(string wikitext)
        {
            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=query")))
                .Returns(Task.FromResult("{\"query\":{\"pages\":[{\"pageid\":99}]}}"));

            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=parse") && u.Contains("prop=sections")))
                .Returns(Task.FromResult("{\"parse\":{\"sections\":[{\"line\":\"Save game data location\",\"index\":\"2\"}]}}"));

            string json = System.Text.Json.JsonSerializer.Serialize(wikitext);
            _httpClient.DownloadStringAsync(Arg.Is<string>(u => u.Contains("action=parse") && u.Contains("prop=wikitext")))
                .Returns(Task.FromResult("{\"parse\":{\"wikitext\":" + json + "}}"));
        }

        [Fact]
        public async Task FetchSaveLocation_SteamRowWithNoUserFolderInIt_SetsNoProfile()
        {
            // A profile the location has no place for could never be picked, and a transfer is refused
            // until every profile has been. This is the Grounded page.
            var gameInfo = new GameInfo { Name = "Grounded" };
            PageWithSaveSection("{{Game data|\n{{Game data/saves|Microsoft Store|{{P|LOCALAPPDATA}}\\Packages\\Microsoft.Maine_8wekyb3d8bbwe\\SystemAppData\\wgs}}\n{{Game data/saves|Steam|{{P|USERPROFILE}}\\Saved Games\\Grounded}}\n}}");

            await _wiki.FetchSaveLocation(gameInfo);

            Assert.Equal("%USERPROFILE%\\Saved Games\\Grounded\\", gameInfo.BaseNonXboxSaveLocation);
            Assert.Null(gameInfo.TargetProfileTypes);
            Assert.Null(gameInfo.TargetProfiles);
        }

        [Fact]
        public async Task FetchSaveLocation_WindowsRowWithAUserFolderInIt_SetsAProfileToPick()
        {
            // The Palworld page, the game of issue #77. Without a profile to pick, the folder list for
            // "<user-id>" came up empty.
            var gameInfo = new GameInfo { Name = "Palworld" };
            PageWithSaveSection("===Save game data location===\n{{Game data|\n{{Game data/saves|Windows|{{p|localappdata}}\\Pal\\Saved\\SaveGames\\{{P|uid}}\\}}\n{{Game data/saves|Microsoft Store|{{p|localappdata}}\\Packages\\PocketpairInc.Palworld_ad4psfrxyesvt\\SystemAppData\\wgs\\{{P|uid}}\\}}\n}}");

            await _wiki.FetchSaveLocation(gameInfo);

            Assert.Equal("%LOCALAPPDATA%\\Pal\\Saved\\SaveGames\\<user-id>\\", gameInfo.BaseNonXboxSaveLocation);
            Assert.Equal(NonXboxProfile.ProfileType.Steam, Assert.Single(gameInfo.TargetProfileTypes));
            Assert.Single(gameInfo.TargetProfiles);
        }

        [Theory]
        [InlineData("{{Game data|\n{{Game data/saves|Windows|{{p|appdata}}\\Game\\Saves")]
        [InlineData("{{Game data/saves|Windows}}")]
        [InlineData("{{Game data/saves|Windows|a}}{{Game data/saves|Windows|b}}")]
        [InlineData("{{Game data/saves|")]
        [InlineData("}}{{}}|||{{")]
        [InlineData("")]
        public async Task FetchSaveLocation_SectionThatIsNotWhatIsExpected_NeverThrows(string wikitext)
        {
            // Issue #77: a page the parser could not read stopped the game from being selected.
            var gameInfo = new GameInfo { Name = "Palworld" };
            PageWithSaveSection(wikitext);

            await _wiki.FetchSaveLocation(gameInfo);
        }

        [Fact]
        public async Task FetchSaveLocation_AnswerThatIsNotAPage_DoesNotSetLocation()
        {
            // What the wiki sends when it is asked too often.
            var gameInfo = new GameInfo { Name = "Hades" };
            _httpClient.DownloadStringAsync(Arg.Any<string>())
                .Returns(Task.FromResult("{\"error\":{\"code\":\"ratelimited\",\"info\":\"You've exceeded your rate limit.\"}}"));

            await _wiki.FetchSaveLocation(gameInfo);

            Assert.Null(gameInfo.BaseNonXboxSaveLocation);
        }

        [Fact]
        public async Task FetchSaveLocation_NameWithAnAmpersand_AsksForThatTitle()
        {
            // Unescaped, everything after the "&" was read as another parameter of the request.
            var gameInfo = new GameInfo { Name = "Ratchet & Clank: Rift Apart" };
            _httpClient.DownloadStringAsync(Arg.Any<string>())
                .Returns(Task.FromResult("{\"query\":{\"pages\":[{\"missing\":true}]}}"));

            await _wiki.FetchSaveLocation(gameInfo);

            await _httpClient.Received().DownloadStringAsync(Arg.Is<string>(u => u.Contains("&titles=Ratchet%20%26%20Clank%3A%20Rift%20Apart&")));
        }

        [Fact]
        public async Task FetchSaveLocation_HttpException_DoesNotThrow()
        {
            var gameInfo = new GameInfo { Name = "Hades" };

            _httpClient.DownloadStringAsync(Arg.Any<string>())
                .Returns<string>(x => { throw new System.Net.Http.HttpRequestException("Network error"); });

            await _wiki.FetchSaveLocation(gameInfo);

            Assert.Null(gameInfo.BaseNonXboxSaveLocation);
        }
    }
}
