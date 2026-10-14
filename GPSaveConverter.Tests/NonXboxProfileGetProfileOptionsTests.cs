using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter.Tests
{
    public class NonXboxProfileGetProfileOptionsTests
    {
        private readonly IFileSystem _fileSystem;

        public NonXboxProfileGetProfileOptionsTests()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            NonXboxProfile.FileSystem = _fileSystem;
            // Keep the Steam profile lookup away from the real registry
            NonXboxProfile.Registry = Substitute.For<IRegistry>();
        }

        [Fact]
        public async Task GetProfileOptions_DirectoryWithProfiles_ReturnsProfiles()
        {
            var profile = new NonXboxProfile(0, NonXboxProfile.ProfileType.Steam);

            _fileSystem.DirectoryExists("C:\\Saves\\").Returns(true);
            _fileSystem.GetDirectories("C:\\Saves\\").Returns(new[] { "C:\\Saves\\12345", "C:\\Saves\\67890" });
            // Each expanded path must also exist
            _fileSystem.DirectoryExists(Arg.Is<string>(s => s.StartsWith("C:\\Saves\\") && s != "C:\\Saves\\")).Returns(true);

            NonXboxProfile[] result = await profile.getProfileOptions("C:\\Saves\\<user-id>\\game.dat");

            Assert.Equal(2, result.Length);
            Assert.Equal("12345", result[0].UserID);
            Assert.Equal("67890", result[1].UserID);
        }

        [Fact]
        public async Task GetProfileOptions_DirectoryDoesNotExist_ReturnsEmpty()
        {
            var profile = new NonXboxProfile(0, NonXboxProfile.ProfileType.Steam);

            _fileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

            NonXboxProfile[] result = await profile.getProfileOptions("C:\\Saves\\<user-id>\\game.dat");

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetProfileOptions_SteamID64Marker_ListsTheFolderAndExpandsIntoIt()
        {
            // As in the game library's entry for Life is Strange: True Colors, where the folder is named
            // with the account's SteamID64. The marker was never replaced, so no profile was ever listed.
            var profile = new NonXboxProfile(0, NonXboxProfile.ProfileType.Steam);
            const string location = "C:\\Saves\\Steam\\<user-id_SteamID64>\\";

            _fileSystem.DirectoryExists("C:\\Saves\\Steam\\").Returns(true);
            _fileSystem.GetDirectories("C:\\Saves\\Steam\\").Returns(new[] { "C:\\Saves\\Steam\\76561198083722517" });
            // Only the real folder exists, not a path that still has the marker in it.
            _fileSystem.DirectoryExists("C:\\Saves\\Steam\\76561198083722517\\").Returns(true);

            NonXboxProfile[] result = await profile.getProfileOptions(location);

            Assert.Single(result);
            Assert.Equal("76561198083722517", result[0].UserID);
            Assert.Equal("C:\\Saves\\Steam\\76561198083722517\\", result[0].ExpandSaveLocation(location));
        }

        [Fact]
        public async Task GetProfileOptions_XboxProfile_ConvertsIntToHex()
        {
            var profile = new NonXboxProfile(0, NonXboxProfile.ProfileType.Xbox);

            _fileSystem.DirectoryExists("C:\\Saves\\").Returns(true);
            _fileSystem.GetDirectories("C:\\Saves\\").Returns(new[] { "C:\\Saves\\255" });
            _fileSystem.DirectoryExists(Arg.Is<string>(s => s != "C:\\Saves\\")).Returns(true);

            NonXboxProfile[] result = await profile.getProfileOptions("C:\\Saves\\<user-id_XboxInt>\\game.dat");

            Assert.Single(result);
            // 255 decimal = FF hex
            Assert.Equal("FF", result[0].UserID);
        }
    }
}
