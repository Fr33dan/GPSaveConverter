using Xunit;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class FileTranslationTests
    {
        [Theory]
        [InlineData("test", "^test$")]
        [InlineData("", "^$")]
        [InlineData("file.txt", "^file.txt$")]
        [InlineData("^already$", "^^already$$")]
        public void ExactRegex_WrapsWithAnchors(string input, string expected)
        {
            string result = FileTranslation.ExactRegex(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Equals_SameProperties_ReturnsTrue()
        {
            var a = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "container1",
                ContainerName2 = "container2",
                NamedRegexGroups = new[] { "(?<FileName>[\\w]+)" }
            };
            var b = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "container1",
                ContainerName2 = "container2",
                NamedRegexGroups = new[] { "(?<FileName>[\\w]+)" }
            };

            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentNonXboxFilename_ReturnsFalse()
        {
            var a = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new string[0]
            };
            var b = new FileTranslation
            {
                NonXboxFilename = "other.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new string[0]
            };

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentRegexGroupCount_ReturnsFalse()
        {
            var a = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new[] { "(?<A>[\\w]+)", "(?<B>[\\w]+)" }
            };
            var b = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new[] { "(?<A>[\\w]+)" }
            };

            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            var a = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new string[0]
            };

            Assert.False(a.Equals(null));
        }

        [Fact]
        public void Equals_NonFileTranslation_ReturnsFalse()
        {
            var a = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new string[0]
            };

            Assert.False(a.Equals("not a FileTranslation"));
        }
    }
}
