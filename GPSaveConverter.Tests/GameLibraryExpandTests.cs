using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class GameLibraryExpandTests
    {
        private readonly IEnvironment _environment;
        private readonly IRegistry _registry;

        public GameLibraryExpandTests()
        {
            _environment = Substitute.For<IEnvironment>();
            _registry = Substitute.For<IRegistry>();
            GameLibrary.Environment = _environment;
            GameLibrary.Registry = _registry;
        }

        [Fact]
        public void ExpandSaveFileLocation_ExpandsEnvironmentVariables()
        {
            _environment.ExpandEnvironmentVariables("%APPDATA%\\Game")
                .Returns("C:\\Users\\Test\\AppData\\Roaming\\Game");

            string result = GameLibrary.ExpandSaveFileLocation("%APPDATA%\\Game");

            Assert.Equal("C:\\Users\\Test\\AppData\\Roaming\\Game", result);
        }

        [Fact]
        public void ExpandSaveFileLocation_SteamMarker_ReplacesWithRegistryValue()
        {
            _environment.ExpandEnvironmentVariables(Arg.Any<string>())
                .Returns(x => x.Arg<string>());
            _registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null)
                .Returns("D:\\Steam");

            string result = GameLibrary.ExpandSaveFileLocation("<Steam-folder>\\saves\\game.dat");

            Assert.Equal("D:\\Steam\\saves\\game.dat", result);
        }

        [Fact]
        public void ExpandSaveFileLocation_SteamMarker_NullRegistry_ReturnsNull()
        {
            _environment.ExpandEnvironmentVariables(Arg.Any<string>())
                .Returns(x => x.Arg<string>());
            _registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null)
                .Returns(null);

            string result = GameLibrary.ExpandSaveFileLocation("<Steam-folder>\\saves\\game.dat");

            Assert.Null(result);
        }

        [Fact]
        public void ExpandSaveFileLocation_NoSteamMarker_DoesNotQueryRegistry()
        {
            _environment.ExpandEnvironmentVariables("C:\\Saves\\game.dat")
                .Returns("C:\\Saves\\game.dat");

            string result = GameLibrary.ExpandSaveFileLocation("C:\\Saves\\game.dat");

            Assert.Equal("C:\\Saves\\game.dat", result);
            _registry.DidNotReceive().GetValue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>());
        }
    }
}
