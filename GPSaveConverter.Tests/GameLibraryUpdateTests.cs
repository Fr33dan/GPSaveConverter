using System.Net;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class GameLibraryUpdateTests
    {
        private readonly ISettingsProvider _settings;
        private readonly IHttpClient _httpClient;

        public GameLibraryUpdateTests()
        {
            _settings = Substitute.For<ISettingsProvider>();
            _settings.DefaultGameLibrary = Library("2026-01-31", "Publisher.Game_abc");
            _httpClient = Substitute.For<IHttpClient>();
            GameLibrary.Settings = _settings;
            GameLibrary.HttpClient = _httpClient;
        }

        private static string Library(string version, params string[] packageNames)
        {
            string games = string.Join(", ", System.Array.ConvertAll(packageNames, p => "{ \"PackageName\": \"" + p + "\" }"));
            return "{ \"Version\": \"" + version + "\", \"GameInfo\": [ " + games + " ] }";
        }

        [Fact]
        public void UpdateDefaultLibrary_NewerLibrary_IsStored()
        {
            string newer = Library("2026-02-01", "Publisher.Game_abc", "Publisher.Other_abc");
            _httpClient.DownloadString(Arg.Any<string>()).Returns(newer);

            bool updated = GameLibrary.UpdateDefaultLibrary();

            Assert.True(updated);
            Assert.Equal(newer, _settings.DefaultGameLibrary);
            Assert.Equal("2026-02-01", GameLibrary.Default.Version);
            _settings.Received(1).Save();
        }

        [Theory]
        [InlineData("2026-01-31")]
        [InlineData("2025-12-01")]
        public void UpdateDefaultLibrary_SameOrOlderLibrary_IsNotStored(string version)
        {
            _httpClient.DownloadString(Arg.Any<string>()).Returns(Library(version, "Publisher.Other_abc"));

            bool updated = GameLibrary.UpdateDefaultLibrary();

            Assert.False(updated);
            Assert.Equal("2026-01-31", GameLibrary.Default.Version);
            _settings.DidNotReceive().Save();
        }

        [Fact]
        public void UpdateDefaultLibrary_DownloadFails_KeepsStoredLibrary()
        {
            _httpClient.DownloadString(Arg.Any<string>()).Throws(new WebException("The remote name could not be resolved"));

            bool updated = GameLibrary.UpdateDefaultLibrary();

            Assert.False(updated);
            Assert.Equal("2026-01-31", GameLibrary.Default.Version);
            _settings.DidNotReceive().Save();
        }

        [Theory]
        [InlineData("<html>Not Found</html>")]
        [InlineData("")]
        [InlineData("null")]
        public void UpdateDefaultLibrary_DownloadIsNotALibrary_KeepsStoredLibrary(string download)
        {
            _httpClient.DownloadString(Arg.Any<string>()).Returns(download);

            bool updated = GameLibrary.UpdateDefaultLibrary();

            Assert.False(updated);
            Assert.Equal("2026-01-31", GameLibrary.Default.Version);
            _settings.DidNotReceive().Save();
        }

        [Fact]
        public void UpdateDefaultLibrary_NewerLibraryWithAProblem_KeepsStoredLibrary()
        {
            // Loading a library with a repeated package name would fail at startup.
            _httpClient.DownloadString(Arg.Any<string>()).Returns(Library("2026-02-01", "Publisher.Game_abc", "Publisher.Game_abc"));

            bool updated = GameLibrary.UpdateDefaultLibrary();

            Assert.False(updated);
            Assert.Equal("2026-01-31", GameLibrary.Default.Version);
            _settings.DidNotReceive().Save();
        }
    }
}
