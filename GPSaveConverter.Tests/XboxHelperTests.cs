using Xunit;
using GPSaveConverter.Xbox;

namespace GPSaveConverter.Tests
{
    public class XboxHelperTests
    {
        private const string Wgs = "C:\\Users\\me\\AppData\\Local\\Packages\\Microsoft.624F8B84B80_8wekyb3d8bbwe\\SystemAppData\\wgs\\";
        private const string Profile = Wgs + "000901FF7B4820F1_0000000000000000000000007900C3C7";

        [Fact]
        public void HoldsSaves_OnlyAProfileFolder_CountsAsASave()
        {
            // Issue #79: Forza Horizon 5 had been uninstalled and its save was still on disk, in the
            // one folder left. The game was not listed, because two folders were asked for.
            Assert.True(XboxHelper.HoldsSaves(new[] { Profile }));
        }

        [Fact]
        public void HoldsSaves_AProfileFolderAndAnother_CountsAsASave()
        {
            Assert.True(XboxHelper.HoldsSaves(new[] { Profile, Wgs + "t" }));
        }

        [Fact]
        public void HoldsSaves_TwoFoldersOfAnyKind_StillCountsAsItAlwaysHas()
        {
            Assert.True(XboxHelper.HoldsSaves(new[] { Wgs + "a", Wgs + "b" }));
        }

        [Fact]
        public void HoldsSaves_OneFolderThatIsNoProfile_DoesNotCount()
        {
            Assert.False(XboxHelper.HoldsSaves(new[] { Wgs + "t" }));
        }

        [Fact]
        public void HoldsSaves_NoFolders_DoesNotCount()
        {
            Assert.False(XboxHelper.HoldsSaves(new string[0]));
        }

        [Theory]
        [InlineData(Profile, true)]
        [InlineData(Profile + "\\", true)]
        [InlineData(Wgs + "t", false)]
        [InlineData(Wgs + "t\\", false)]
        public void IsProfileFolder_GoesByTheFoldersOwnName(string folder, bool expected)
        {
            // The package name above it has an underscore in it as well, and must not count.
            Assert.Equal(expected, XboxHelper.IsProfileFolder(folder));
        }
    }
}
