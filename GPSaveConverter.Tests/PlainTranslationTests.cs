using System.Collections.Generic;
using System.Linq;
using GPSaveConverter.Library;
using Xunit;
using XboxFile = GPSaveConverter.Library.TranslationSimulator.XboxFile;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// A translation in plain words, as the translation editor shows and edits it.
    /// </summary>
    public class PlainTranslationTests
    {
        private static FileTranslation SlotTranslation()
        {
            return new FileTranslation
            {
                NamedRegexGroups = new[] { "(?<FileSlot>[0-9]+)" },
                ContainerName1 = "SaveGame",
                ContainerName2 = "",
                XboxFileID = "SaveSlot${FileSlot}",
                NonXboxFilename = "saveFile${FileSlot}.sav"
            };
        }

        private static PlainTranslation Words(string container1, string container2, string blob, string file)
        {
            return new PlainTranslation { ContainerName1 = container1, ContainerName2 = container2, BlobName = blob, FileName = file };
        }

        [Fact]
        public void From_TheTranslationThePlusButtonAdds_ShowsItsPlaceholderEverywhere()
        {
            PlainTranslation plain = PlainTranslation.From(FileTranslation.getDefaultInstance());

            Assert.Equal("{FileName}", plain.ContainerName1);
            Assert.Equal("{FileName}", plain.ContainerName2);
            Assert.Equal("{FileName}", plain.BlobName);
            Assert.Equal("{FileName}", plain.FileName);
            Assert.Equal(new[] { "FileName" }, plain.OwnPlaceholders);
        }

        [Fact]
        public void From_ALibraryStyleTranslation_ShowsItsNamesWithItsOwnPlaceholder()
        {
            PlainTranslation plain = PlainTranslation.From(SlotTranslation());

            Assert.Equal("SaveGame", plain.ContainerName1);
            Assert.Equal("", plain.ContainerName2);
            Assert.Equal("SaveSlot{FileSlot}", plain.BlobName);
            Assert.Equal("saveFile{FileSlot}.sav", plain.FileName);
        }

        [Fact]
        public void From_EscapedCharacters_AreShownAsTheyAre()
        {
            FileTranslation translation = new FileTranslation
            {
                NamedRegexGroups = new string[0],
                ContainerName1 = @"Disgaea 4 Complete\+",
                ContainerName2 = "",
                XboxFileID = @"slot\(1\)",
                NonXboxFilename = @"SLOT_0\\CompleteSave"
            };

            PlainTranslation plain = PlainTranslation.From(translation);

            Assert.Equal("Disgaea 4 Complete+", plain.ContainerName1);
            Assert.Equal("slot(1)", plain.BlobName);
            Assert.Equal(@"SLOT_0\CompleteSave", plain.FileName);
        }

        [Fact]
        public void From_TheXboxProfile_IsShownAsProfile()
        {
            FileTranslation translation = new FileTranslation
            {
                NamedRegexGroups = new[] { "(?<FileExtension>.+)" },
                ContainerName1 = "User_${XboxProfileID}",
                ContainerName2 = "User_${XboxProfileID}",
                XboxFileID = "${FileExtension}",
                NonXboxFilename = "User_${XboxProfileID}.${FileExtension}"
            };

            PlainTranslation plain = PlainTranslation.From(translation);

            Assert.Equal("User_{profile}", plain.ContainerName1);
            Assert.Equal("User_{profile}.{FileExtension}", plain.FileName);
        }

        [Theory]
        [InlineData("Save.*", "", "data", "save.dat")]
        [InlineData("SaveGame", "", "Slot[0-9]", "save.dat")]
        [InlineData("SaveGame", "", "data", @"\d+.dat")]
        [InlineData("SaveGame", "", "data", "save(1|2).dat")]
        [InlineData("SaveGame", "", "data", "save${XboxProfileID_Int}.dat")]
        [InlineData("SaveGame", "", "data", "save${NotAGroup}.dat")]
        [InlineData("SaveGame", "", "data", @"save\{1\}.dat")]
        public void From_ANameThatIsAPattern_CannotBeShown(string container1, string container2, string blob, string file)
        {
            FileTranslation translation = new FileTranslation
            {
                NamedRegexGroups = new[] { "(?<FileSlot>[0-9]+)" },
                ContainerName1 = container1,
                ContainerName2 = container2,
                XboxFileID = blob,
                NonXboxFilename = file
            };

            Assert.Null(PlainTranslation.From(translation));
        }

        [Theory]
        [InlineData("([0-9]+)")]
        [InlineData("(?<profile>[0-9]+)")]
        [InlineData("(?<FileSlot>[0-9]+")]
        public void From_AGroupWithoutAUsableName_CannotBeShown(string group)
        {
            FileTranslation translation = SlotTranslation();
            translation.NamedRegexGroups = new[] { group };

            Assert.Null(PlainTranslation.From(translation));
        }

        [Fact]
        public void ToTranslation_TheBuiltInPlaceholders_BecomeGroups()
        {
            string problem;

            FileTranslation translation = Words("SaveGame", "", "SaveSlot{number}", @"{name}\saveFile{number}.sav").ToTranslation(out problem);

            Assert.Null(problem);
            Assert.Equal(new[] { "(?<number>[0-9]+)", @"(?<name>[^\\/]+)" }, translation.NamedRegexGroups);
            Assert.Equal("SaveSlot${number}", translation.XboxFileID);
            Assert.Equal(@"${name}\\saveFile${number}.sav", translation.NonXboxFilename);
            Assert.Null(translation.FindProblem());
        }

        [Fact]
        public void ToTranslation_RealCharacters_AreWrittenSoThatTheyMatchThemselves()
        {
            string problem;

            FileTranslation translation = Words("Disgaea 4 Complete+", "", "slot(1)", @"SLOT_0\CompleteSave").ToTranslation(out problem);

            Assert.Equal(@"Disgaea 4 Complete\+", translation.ContainerName1);
            Assert.Equal(@"slot\(1\)", translation.XboxFileID);
            Assert.Equal(@"SLOT_0\\CompleteSave", translation.NonXboxFilename);
            Assert.Empty(translation.NamedRegexGroups);
        }

        [Fact]
        public void ToTranslation_Profile_IsTheXboxProfileID()
        {
            string problem;

            FileTranslation translation = Words("User_{profile}", "", "data", "User_{profile}.sav").ToTranslation(out problem);

            Assert.Equal("User_${XboxProfileID}", translation.ContainerName1);
            Assert.Equal("User_${XboxProfileID}.sav", translation.NonXboxFilename);
            Assert.Empty(translation.NamedRegexGroups);
        }

        [Fact]
        public void ToTranslation_UnchangedWords_GiveTheSameTranslationBack()
        {
            string problem;
            FileTranslation slot = SlotTranslation();
            FileTranslation plusButton = FileTranslation.getDefaultInstance();

            Assert.Equal(slot, PlainTranslation.From(slot).ToTranslation(out problem));
            Assert.Equal(plusButton, PlainTranslation.From(plusButton).ToTranslation(out problem));
        }

        [Fact]
        public void ToTranslation_AChangedName_KeepsTheTranslationsOwnPlaceholder()
        {
            PlainTranslation plain = PlainTranslation.From(FileTranslation.getDefaultInstance());
            plain.ContainerName1 = "SaveGame";
            plain.ContainerName2 = "";
            string problem;

            FileTranslation translation = plain.ToTranslation(out problem);

            Assert.Equal("SaveGame", translation.ContainerName1);
            Assert.Equal("${FileName}", translation.XboxFileID);
            Assert.Equal(FileTranslation.getDefaultInstance().NamedRegexGroups, translation.NamedRegexGroups);
        }

        [Theory]
        [InlineData("", "", "data", "save.dat", "Give the Xbox container's name.")]
        [InlineData("SaveGame", "", " ", "save.dat", "Give the Xbox blob's name.")]
        [InlineData("SaveGame", "", "data", "", "Give the file's name on this PC.")]
        [InlineData("SaveGame", "", "Slot{slot}", "save{slot}.dat", "{slot} is not a placeholder. Use {number}, {name}, {profile}.")]
        [InlineData("SaveGame", "", "Slot{number", "save.dat", "Use { and } only around a placeholder, such as {number}.")]
        [InlineData("SaveGame", "", "Slot{number}_{number}", "save.dat", "{number} is in \"Slot{number}_{number}\" twice. A placeholder can be used once in each name.")]
        public void ToTranslation_WordsThatCannotBeUsed_SayWhy(string container1, string container2, string blob, string file, string expected)
        {
            string problem;

            Assert.Null(Words(container1, container2, blob, file).ToTranslation(out problem));
            Assert.Equal(expected, problem);
        }

        [Fact]
        public void PreviewOf_ShowsPairsAndTheFilesItWouldMake()
        {
            string problem;
            FileTranslation translation = Words("SaveGame", "", "SaveSlot{number}", "saveFile{number}.sav").ToTranslation(out problem);
            List<XboxFile> xboxFiles = new List<XboxFile> { new XboxFile("SaveGame", "", "SaveSlot0"), new XboxFile("SaveGame", "", "SaveSlot3") };
            List<string> nonXboxFiles = new List<string> { "saveFile0.sav", "saveFile7.sav", "notes.txt" };

            PlainTranslation.Preview preview = PlainTranslation.PreviewOf(translation, xboxFiles, nonXboxFiles, TranslationSimulator.XboxProfileID);

            Assert.Equal(new[]
            {
                "SaveGame / SaveSlot0   <->   saveFile0.sav",
                "SaveGame / SaveSlot3   ->   saveFile3.sav   (new on this PC)",
                "saveFile7.sav   ->   SaveGame / SaveSlot7   (new on Xbox)"
            }, preview.Lines);
            Assert.Equal(2, preview.XboxFilesCovered);
            Assert.Equal(2, preview.NonXboxFilesCovered);
        }

        [Fact]
        public void PreviewOf_AFileWithNoContainer_WouldNeedANewOne()
        {
            string problem;
            FileTranslation translation = Words("{name}", "{name}", "data", "{name}.sav").ToTranslation(out problem);
            List<XboxFile> xboxFiles = new List<XboxFile> { new XboxFile("slot1", "slot1", "data") };
            List<string> nonXboxFiles = new List<string> { "slot1.sav", "slot2.sav" };

            PlainTranslation.Preview preview = PlainTranslation.PreviewOf(translation, xboxFiles, nonXboxFiles, TranslationSimulator.XboxProfileID);

            Assert.Equal(new[]
            {
                "slot1 / data   <->   slot1.sav",
                "slot2.sav   ->   a new Xbox container"
            }, preview.Lines);
        }

        [Fact]
        public void PreviewOf_APlaceholderOnOneSideOnly_SaysWhichFilesCannotBeNamed()
        {
            string problem;
            FileTranslation translation = Words("Slot", "", "data", "save{number}.sav").ToTranslation(out problem);
            List<XboxFile> xboxFiles = new List<XboxFile> { new XboxFile("Slot", "", "data") };
            List<string> nonXboxFiles = new List<string> { "notes.txt" };

            PlainTranslation.Preview preview = PlainTranslation.PreviewOf(translation, xboxFiles, nonXboxFiles, TranslationSimulator.XboxProfileID);

            Assert.Equal(new[] { "Slot / data:   no name on this PC can be worked out from it" }, preview.Lines);
            Assert.Equal(0, preview.XboxFilesCovered);
        }

        [Theory]
        [InlineData("SaveGame", "", "SaveSlot0", "SaveGame / SaveSlot0")]
        [InlineData("SaveGame", "SaveGame", "SaveSlot0", "SaveGame / SaveSlot0")]
        [InlineData("Slot", "Backup", "data", "Slot / Backup / data")]
        public void XboxName_GivesTheSecondContainerNameOnlyWhenItSaysSomething(string container1, string container2, string blob, string expected)
        {
            Assert.Equal(expected, PlainTranslation.XboxName(new XboxFile(container1, container2, blob)));
        }
    }
}
