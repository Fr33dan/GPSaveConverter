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
        }

        [Fact]
        public async Task GetProfileOptions_DirectoryWithProfiles_ReturnsProfiles()
        {
            var profile = new NonXboxProfile(0, NonXboxProfile.ProfileType.Steam);

            _fileSystem.DirectoryExists("C:\\Saves\\").Returns(true);
            _fileSystem.GetDirectories("C:\\Saves\\").Returns(new[] { "C:\\Saves\\12345", "C:\\Saves\\67890" });
            // Each expanded path must also exist
            _fileSystem.DirectoryExists(Arg.Is<string>(s => s.StartsWith("C:\\Saves\\") && s != "C:\\Saves\\")).Returns(true);

            // Mock HttpClient to avoid real network calls during FetchProfileInformation
            var httpClient = Substitute.For<IHttpClient>();
            httpClient.DownloadStringAsync(Arg.Any<string>()).Returns(Task.FromResult("{}"));
            NonXboxProfile.HttpClient = httpClient;

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
        public async Task GetProfileOptions_XboxProfile_ConvertsIntToHex()
        {
            var profile = new NonXboxProfile(0, NonXboxProfile.ProfileType.Xbox);

            _fileSystem.DirectoryExists("C:\\Saves\\").Returns(true);
            _fileSystem.GetDirectories("C:\\Saves\\").Returns(new[] { "C:\\Saves\\255" });
            _fileSystem.DirectoryExists(Arg.Is<string>(s => s != "C:\\Saves\\")).Returns(true);

            var httpClient = Substitute.For<IHttpClient>();
            httpClient.DownloadStringAsync(Arg.Any<string>()).Returns(Task.FromResult("{}"));
            NonXboxProfile.HttpClient = httpClient;

            NonXboxProfile[] result = await profile.getProfileOptions("C:\\Saves\\<user-id_XboxInt>\\game.dat");

            Assert.Single(result);
            // 255 decimal = FF hex
            Assert.Equal("FF", result[0].UserID);
        }
    }
}
