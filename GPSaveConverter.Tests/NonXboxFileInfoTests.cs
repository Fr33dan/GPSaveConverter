using Xunit;

namespace GPSaveConverter.Tests
{
    public class NonXboxFileInfoTests
    {
        [Fact]
        public void Equals_SamePathAndRelativePath_ReturnsTrue()
        {
            var a = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };
            var b = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };

            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentFilePath_ReturnsFalse()
        {
            var a = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };
            var b = new NonXboxFileInfo { FilePath = "D:\\Other\\game.dat", RelativePath = "game.dat" };

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentRelativePath_ReturnsFalse()
        {
            var a = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };
            var b = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "other.dat" };

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentType_ReturnsFalse()
        {
            var a = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };

            Assert.False(a.Equals("not a NonXboxFileInfo"));
        }

        [Fact]
        public void GetHashCode_SameProperties_ReturnsSameHash()
        {
            var a = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };
            var b = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentProperties_ReturnsDifferentHash()
        {
            var a = new NonXboxFileInfo { FilePath = "C:\\Saves\\game.dat", RelativePath = "game.dat" };
            var b = new NonXboxFileInfo { FilePath = "D:\\Other\\save.dat", RelativePath = "save.dat" };

            Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
