using NSubstitute;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class SteamProfileLookupTests
    {
        private const string SteamKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam";
        private const string SteamKey32On64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam";

        // Steam3 ID 123456789 is SteamID64 76561198083722517.
        private const string LoginUsers =
            "\"users\"\n" +
            "{\n" +
            "\t\"76561198000000001\"\n" +
            "\t{\n" +
            "\t\t\"AccountName\"\t\t\"firstaccount\"\n" +
            "\t\t\"PersonaName\"\t\t\"First Player\"\n" +
            "\t\t\"MostRecent\"\t\t\"0\"\n" +
            "\t}\n" +
            "\t\"76561198083722517\"\n" +
            "\t{\n" +
            "\t\t\"AccountName\"\t\t\"secondaccount\"\n" +
            "\t\t\"PersonaName\"\t\t\"Second Player\"\n" +
            "\t\t\"MostRecent\"\t\t\"1\"\n" +
            "\t}\n" +
            "}\n";

        private readonly IFileSystem _fileSystem;
        private readonly IRegistry _registry;

        public SteamProfileLookupTests()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            _registry = Substitute.For<IRegistry>();
        }

        [Fact]
        public void GetInstallPath_ReadsRegistry()
        {
            _registry.GetValue(SteamKey, "InstallPath", null).Returns("D:\\Steam");

            Assert.Equal("D:\\Steam", Steam.GetInstallPath(_registry));
        }

        [Fact]
        public void GetInstallPath_FallsBackTo32BitRegistryView()
        {
            _registry.GetValue(SteamKey, "InstallPath", null).Returns(null);
            _registry.GetValue(SteamKey32On64, "InstallPath", null).Returns("D:\\Steam");

            Assert.Equal("D:\\Steam", Steam.GetInstallPath(_registry));
        }

        [Fact]
        public void GetInstallPath_SteamNotInstalled_ReturnsNull()
        {
            _registry.GetValue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>()).Returns(null);

            Assert.Null(Steam.GetInstallPath(_registry));
        }

        [Theory]
        [InlineData(76561198000000001UL, "First Player")]
        [InlineData(76561198083722517UL, "Second Player")]
        public void ParsePersonaName_FindsNameForUser(ulong steamID64, string expected)
        {
            Assert.Equal(expected, Steam.ParsePersonaName(LoginUsers, steamID64));
        }

        [Fact]
        public void ParsePersonaName_UnknownUser_ReturnsNull()
        {
            Assert.Null(Steam.ParsePersonaName(LoginUsers, 76561198999999999UL));
        }

        [Fact]
        public void ParsePersonaName_EmptyName_ReturnsNull()
        {
            string loginUsers = "\"users\"\n{\n\t\"76561198000000001\"\n\t{\n\t\t\"PersonaName\"\t\t\"\"\n\t}\n}\n";

            Assert.Null(Steam.ParsePersonaName(loginUsers, 76561198000000001UL));
        }

        [Theory]
        [InlineData("Say \\\"hi\\\"", "Say \"hi\"")]
        [InlineData("back\\\\slash", "back\\slash")]
        [InlineData("{clan} Player", "{clan} Player")]
        public void ParsePersonaName_HandlesEscapesAndBraces(string storedName, string expected)
        {
            string loginUsers = "\"users\"\n{\n\t\"76561198000000001\"\n\t{\n\t\t\"AccountName\"\t\t\"account\"\n\t\t\"PersonaName\"\t\t\"" + storedName + "\"\n\t}\n}\n";

            Assert.Equal(expected, Steam.ParsePersonaName(loginUsers, 76561198000000001UL));
        }

        [Fact]
        public void GetUserInformation_Steam3Profile_SetsNameAndIconFromSteamFiles()
        {
            _registry.GetValue(SteamKey, "InstallPath", null).Returns("D:\\Steam");
            _fileSystem.FileExists("D:\\Steam\\config\\loginusers.vdf").Returns(true);
            _fileSystem.ReadAllText("D:\\Steam\\config\\loginusers.vdf").Returns(LoginUsers);
            _fileSystem.FileExists("D:\\Steam\\config\\avatarcache\\76561198083722517.png").Returns(true);
            var profile = new NonXboxProfile("123456789", 0, NonXboxProfile.ProfileType.Steam);

            new Steam(_fileSystem, _registry).GetUserInformation(profile);

            Assert.Equal("Second Player", profile.UserName);
            Assert.Equal("D:\\Steam\\config\\avatarcache\\76561198083722517.png", profile.UserIconLocation);
        }

        [Fact]
        public void GetUserInformation_SteamID64Profile_UsesIDAsIs()
        {
            _registry.GetValue(SteamKey, "InstallPath", null).Returns("D:\\Steam");
            _fileSystem.FileExists("D:\\Steam\\config\\loginusers.vdf").Returns(true);
            _fileSystem.ReadAllText("D:\\Steam\\config\\loginusers.vdf").Returns(LoginUsers);
            var profile = new NonXboxProfile("76561198000000001", 0, NonXboxProfile.ProfileType.Steam);
            profile.IDType = NonXboxProfile.UserIDType.steamID64;

            new Steam(_fileSystem, _registry).GetUserInformation(profile);

            Assert.Equal("First Player", profile.UserName);
            Assert.Null(profile.UserIconLocation);
        }

        [Fact]
        public void GetUserInformation_UserNotKnownToSteam_KeepsIDAsName()
        {
            _registry.GetValue(SteamKey, "InstallPath", null).Returns("D:\\Steam");
            _fileSystem.FileExists("D:\\Steam\\config\\loginusers.vdf").Returns(true);
            _fileSystem.ReadAllText("D:\\Steam\\config\\loginusers.vdf").Returns(LoginUsers);
            var profile = new NonXboxProfile("42", 0, NonXboxProfile.ProfileType.Steam);

            new Steam(_fileSystem, _registry).GetUserInformation(profile);

            Assert.Equal("42", profile.UserName);
            Assert.Null(profile.UserIconLocation);
        }

        [Fact]
        public void GetUserInformation_SteamNotInstalled_KeepsIDAsNameWithoutReadingFiles()
        {
            _registry.GetValue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>()).Returns(null);
            var profile = new NonXboxProfile("123456789", 0, NonXboxProfile.ProfileType.Steam);

            new Steam(_fileSystem, _registry).GetUserInformation(profile);

            Assert.Equal("123456789", profile.UserName);
            _fileSystem.DidNotReceive().ReadAllText(Arg.Any<string>());
        }

        [Fact]
        public void GetUserInformation_FolderNameIsNotAnID_DoesNotThrow()
        {
            _registry.GetValue(SteamKey, "InstallPath", null).Returns("D:\\Steam");
            var profile = new NonXboxProfile("not-a-number", 0, NonXboxProfile.ProfileType.Steam);

            new Steam(_fileSystem, _registry).GetUserInformation(profile);

            Assert.Equal("not-a-number", profile.UserName);
        }

        [Fact]
        public void LoadIcon_UnreadableFile_ReturnsNullAndClearsLocation()
        {
            _fileSystem.ReadAllBytes("D:\\Steam\\config\\avatarcache\\1.png").Returns(new byte[] { 1, 2, 3 });
            var profile = new NonXboxProfile("1", 0, NonXboxProfile.ProfileType.Steam);
            profile.UserIconLocation = "D:\\Steam\\config\\avatarcache\\1.png";

            System.Drawing.Bitmap icon = new Steam(_fileSystem, _registry).LoadIcon(profile);

            Assert.Null(icon);
            Assert.Null(profile.UserIconLocation);
        }
    }
}
