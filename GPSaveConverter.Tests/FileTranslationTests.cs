using System.Text.RegularExpressions;
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

        private const string XboxProfileID = "000900000000ABCD";

        private static string XboxFileIDFor(string xboxFileID, string nonXboxFilename, string path, bool asPattern, out bool complete)
        {
            var translation = new FileTranslation
            {
                NonXboxFilename = nonXboxFilename,
                XboxFileID = xboxFileID,
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new[] { "(?<FileName>[\\w\\-+(). ]+)", "(?<Slot>[0-9]+)", "(?<Region>[A-Z]+)" }
            };
            Match match = Regex.Match(path, translation.NonXboxFilenameRegex);
            Assert.True(match.Success);

            return translation.FillXboxFileID(match, XboxProfileID, asPattern, out complete);
        }

        [Theory]
        [InlineData("Data", "${FileName}", "anything.sav", "Data")]
        [InlineData("${FileName}", "${FileName}", "peru_123abc.dat", "peru_123abc.dat")]
        [InlineData("save\\\\SLOT_${Slot}/${FileName}", "SLOT_${Slot}\\\\${FileName}", "SLOT_0\\CompleteSave", "save\\SLOT_0/CompleteSave")]
        [InlineData("${Slot}\\.map", "${Slot}.map", "3.map", "3.map")]
        public void FillXboxFileID_GivesTheIDAsItIsStored(string xboxFileID, string nonXboxFilename, string path, string expected)
        {
            bool complete;

            string result = XboxFileIDFor(xboxFileID, nonXboxFilename, path, false, out complete);

            Assert.Equal(expected, result);
            Assert.True(complete);
        }

        [Theory]
        [InlineData("save\\name.dat")]
        [InlineData("config\\achievements.cfg")]
        [InlineData("data\\table")]
        [InlineData("save\\SLOT_0/CompleteSave")]
        [InlineData("config\\user_settings.cfg")]
        public void FillXboxFileID_BackslashTypedAsTheToolShowsIt_IsKeptAsWritten(string xboxFileID)
        {
            // Read as escapes these would be a line break, a bell, a tab, or not valid at all.
            bool complete;

            string result = XboxFileIDFor(xboxFileID, "${FileName}", "save.dat", false, out complete);

            Assert.Equal(xboxFileID, result);
        }

        [Fact]
        public void FillXboxFileID_AsPattern_MatchesOnlyThatID()
        {
            bool complete;

            string pattern = FileTranslation.ExactRegex(XboxFileIDFor("${FileName}", "${FileName}", "a+b (1).sav", true, out complete));

            Assert.Matches(pattern, "a+b (1).sav");
            Assert.DoesNotMatch(pattern, "aab (1).sav");
            Assert.DoesNotMatch(pattern, "a+b (1)xsav");
        }

        [Fact]
        public void FillXboxFileID_ProfileSubstitutions_ComeFromTheProfile()
        {
            bool complete;

            string result = XboxFileIDFor("User_${XboxProfileID}_${XboxProfileID_Int}", "${FileName}", "save.dat", false, out complete);

            Assert.Equal("User_900000000ABCD_2533274790439885", result);
            Assert.True(complete);
        }

        [Fact]
        public void FillXboxFileID_NameThePathGivesNoValueFor_IsLeftInPlace()
        {
            bool complete;

            string result = XboxFileIDFor("save_${Region}", "${FileName}", "save.dat", false, out complete);

            Assert.Equal("save_${Region}", result);
            Assert.False(complete);
        }
    }
}
