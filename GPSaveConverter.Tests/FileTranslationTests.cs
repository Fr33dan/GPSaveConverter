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

        [Fact]
        public void FindNonXboxFile_PrefersTheExactNameOverOneThatContainsIt()
        {
            string found = FileTranslation.FindNonXboxFile(new[] { "autoprofile.sav", "profile.sav" }, "profile.sav", "profile.sav");

            Assert.Equal("profile.sav", found);
        }

        [Fact]
        public void FindNonXboxFile_IgnoresLetterCaseAndRepeatedSeparators()
        {
            string found = FileTranslation.FindNonXboxFile(new[] { "Saves\\Level.sav" }, "saves\\level.sav", "saves\\\\level.sav");

            Assert.Equal("Saves\\Level.sav", found);
        }

        [Fact]
        public void FindNonXboxFile_NoExactName_FallsBackToTheNameAnywhereInAPath()
        {
            // What earlier versions did for every lookup. Kept so saved translations that lean on it still work.
            string found = FileTranslation.FindNonXboxFile(new[] { "9FD2\\Level01.sav" }, "Level01.sav", "Level01.sav");

            Assert.Equal("9FD2\\Level01.sav", found);
        }

        [Fact]
        public void FindNonXboxFile_PathThatIsNotAValidPattern_FindsNothingInsteadOfThrowing()
        {
            string found = FileTranslation.FindNonXboxFile(new[] { "other.sav" }, "Saves\\Level.sav", "Saves\\Level.sav");

            Assert.Null(found);
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

        private static FileTranslation ContainerNamed(string containerName1, string containerName2)
        {
            return new FileTranslation
            {
                NonXboxFilename = "${Slot}\\\\${File}",
                XboxFileID = "${File}",
                ContainerName1 = containerName1,
                ContainerName2 = containerName2,
                NamedRegexGroups = new[] { "(?<Slot>[\\w\\-]+)", "(?<File>[\\w\\-.]+)", "(?<Region>[\\w]+)" }
            };
        }

        [Theory]
        [InlineData("${Slot}", "${Slot}", "GAME-AUTOSAVE1", "GAME-AUTOSAVE1")]
        [InlineData("${Slot}", "", "GAME-AUTOSAVE1", "")]
        [InlineData("Save_${Slot}.dat", "User_${XboxProfileID}", "Save_GAME-AUTOSAVE1.dat", "User_900000000ABCD")]
        // Written as a pattern, so a plus sign or a backslash in the name has a backslash in front of it.
        [InlineData("Complete\\+ ${Slot}", "a\\\\b", "Complete+ GAME-AUTOSAVE1", "a\\b")]
        public void NewContainerNames_TemplateThatSpellsOutAName_GivesThatName(string containerName1, string containerName2, string expected1, string expected2)
        {
            string problem;

            string[] names = ContainerNamed(containerName1, containerName2).NewContainerNames("GAME-AUTOSAVE1\\game.details", "000900000000ABCD", out problem);

            Assert.Equal(new[] { expected1, expected2 }, names);
            Assert.Null(problem);
        }

        [Theory]
        // Each finds containers that exist. None says what a new one is called.
        [InlineData("Save.*")]
        [InlineData("Slot[0-9]")]
        [InlineData("(Auto|Manual)Save")]
        [InlineData("Save\\d")]
        [InlineData("Save+")]
        [InlineData("Save_${Slot}?")]
        public void NewContainerNames_TemplateThatIsAPatternForMany_GivesNoName(string containerName)
        {
            string problem;

            string[] first = ContainerNamed(containerName, "${Slot}").NewContainerNames("GAME-AUTOSAVE1\\game.details", "000900000000ABCD", out problem);
            Assert.Null(first);
            Assert.Contains(containerName, problem);

            string[] second = ContainerNamed("${Slot}", containerName).NewContainerNames("GAME-AUTOSAVE1\\game.details", "000900000000ABCD", out problem);
            Assert.Null(second);
            Assert.Contains(containerName, problem);
        }

        [Fact]
        public void NewContainerNames_NameThePathGivesNoValueFor_GivesNoName()
        {
            string problem;

            string[] names = ContainerNamed("${Slot}_${Region}", "").NewContainerNames("GAME-AUTOSAVE1\\game.details", "000900000000ABCD", out problem);

            Assert.Null(names);
            Assert.Contains("${Slot}_${Region}", problem);
        }
    }
}
