using System.Text.Json;
using Xunit;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// The application downloads GameLibrary.json straight from the master branch,
    /// so a mistake in that file reaches every user. These tests are the gate.
    /// </summary>
    public class GameLibraryJsonTests
    {
        private const string ValidTranslation =
            "{ \"ContainerName1\": \"SaveGame\", \"ContainerName2\": \"\", \"NamedRegexGroups\": [ \"(?<FileSlot>[0-9]+)\" ]," +
            " \"XboxFileID\": \"SaveSlot${FileSlot}\", \"NonXboxFilename\": \"saveFile${FileSlot}.sav\" }";

        private static StoredGameLibrary Library(string version, params string[] games)
        {
            return JsonSerializer.Deserialize<StoredGameLibrary>("{ \"Version\": \"" + version + "\", \"GameInfo\": [ " + string.Join(", ", games) + " ] }");
        }

        private static string Game(string packageName, params string[] translations)
        {
            return "{ \"PackageName\": \"" + packageName + "\", \"FileTranslations\": [ " + string.Join(", ", translations) + " ] }";
        }

        [Fact]
        public void ShippedLibrary_HasNoProblems()
        {
            StoredGameLibrary library = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Resources.GameLibrary);

            // On failure the message names the game and the mistake.
            string problem = library.FindProblem();
            Assert.True(problem == null, problem);
        }

        #region The translations that are being tested

        // GameLibrary.Preview.json sits beside the game library and is downloaded by every copy of
        // the app that has the option for it turned on. These are the gate for that file.

        private static StoredGameLibrary PreviewLibrary()
        {
            string file = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "GameLibrary.Preview.json");
            return JsonSerializer.Deserialize<StoredGameLibrary>(System.IO.File.ReadAllText(file));
        }

        [Fact]
        public void PreviewLibrary_HasNoProblems()
        {
            string problem = PreviewLibrary().FindProblem(true);
            Assert.True(problem == null, problem);
        }

        [Fact]
        public void PreviewLibrary_IsWhereTheAppLooksForIt()
        {
            // The file is copied into the tests from this path in the repository.
            Assert.EndsWith("/GPSaveConverter/master/GPSaveConverter/Resources/GameLibrary.Preview.json", GameLibrary.PreviewLibraryURL);
        }

        [Fact]
        public void PreviewLibrary_EveryTranslationIsOneThatWasChecked()
        {
            // What a tester is given has to be what RequestedTranslationTests checked against the
            // file names posted in the issue. Those tests read the files in this folder.
            string fixtures = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "RequestedTranslations");
            System.Collections.Generic.List<GameInfo> checkedGames = new System.Collections.Generic.List<GameInfo>();
            foreach (string file in System.IO.Directory.GetFiles(fixtures, "*.json"))
            {
                checkedGames.Add(JsonSerializer.Deserialize<GameInfo>(System.IO.File.ReadAllText(file)));
            }

            foreach (GameInfo game in PreviewLibrary().GameInfo)
            {
                GameInfo wasChecked = checkedGames.Find(g => g.PackageName == game.PackageName);
                Assert.True(wasChecked != null, game.PackageName + " has no file in Fixtures\\RequestedTranslations.");
                Assert.True(System.Linq.Enumerable.SequenceEqual(wasChecked.FileTranslations, game.FileTranslations), game.PackageName + ": the translations differ from the ones that were checked.");
            }
        }

        [Fact]
        public void PreviewLibrary_HoldsNothingTheGameLibraryAlreadyHas()
        {
            // A translation that is confirmed moves to the game library. Left here as well, it would
            // go on being tried first for everyone with the option on.
            StoredGameLibrary shipped = JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Resources.GameLibrary);

            foreach (GameInfo game in PreviewLibrary().GameInfo)
            {
                foreach (GameInfo shippedGame in shipped.GameInfo)
                {
                    if (shippedGame.PackageName != game.PackageName) continue;

                    foreach (FileTranslation translation in game.FileTranslations)
                    {
                        Assert.False(shippedGame.FileTranslations.Contains(translation), game.PackageName + ": \"" + translation.NonXboxFilename + "\" is in the game library already.");
                    }
                }
            }
        }

        [Fact]
        public void FindProblem_NoGames_IsAllowedForThePreviewLibrary()
        {
            // It lists none when every translation in it has been confirmed or dropped.
            Assert.Null(Library("2026-01-31").FindProblem(true));
        }

        #endregion

        [Fact]
        public void FindProblem_ValidLibrary_ReturnsNull()
        {
            StoredGameLibrary library = Library("2026-01-31", Game("Publisher.Game_abc", ValidTranslation));

            Assert.Null(library.FindProblem());
        }

        [Theory]
        [InlineData("")]
        [InlineData("31-01-2026")]
        [InlineData("2026-1-31")]
        [InlineData("latest")]
        public void FindProblem_VersionNotADate_IsReported(string version)
        {
            StoredGameLibrary library = Library(version, Game("Publisher.Game_abc", ValidTranslation));

            Assert.Contains("Version", library.FindProblem());
        }

        [Fact]
        public void FindProblem_NoGames_IsReported()
        {
            StoredGameLibrary library = Library("2026-01-31");

            Assert.Contains("no games", library.FindProblem());
        }

        [Fact]
        public void FindProblem_DuplicatePackageName_IsReported()
        {
            StoredGameLibrary library = Library("2026-01-31", Game("Publisher.Game_abc", ValidTranslation), Game("publisher.game_ABC", ValidTranslation));

            Assert.Contains("more than once", library.FindProblem());
        }

        [Fact]
        public void FindProblem_MissingPackageName_IsReported()
        {
            StoredGameLibrary library = Library("2026-01-31", "{ \"FileTranslations\": [ " + ValidTranslation + " ] }");

            Assert.Contains("PackageName", library.FindProblem());
        }

        [Fact]
        public void FindProblem_NamesTheGameWithTheBadTranslation()
        {
            string noBlobID = "{ \"ContainerName1\": \"a\", \"ContainerName2\": \"a\", \"NamedRegexGroups\": [], \"NonXboxFilename\": \"a\" }";
            StoredGameLibrary library = Library("2026-01-31", Game("Publisher.Good_abc", ValidTranslation), Game("Publisher.Bad_abc", noBlobID));

            string problem = library.FindProblem();

            Assert.StartsWith("Publisher.Bad_abc", problem);
            Assert.Contains("XboxFileID", problem);
        }

        [Fact]
        public void Translation_InvalidGroupPattern_IsReported()
        {
            FileTranslation translation = FileTranslation.getDefaultInstance();
            translation.NamedRegexGroups = new[] { "(?<FileName>[a-z" };

            Assert.Contains("not a valid pattern", translation.FindProblem());
        }

        [Theory]
        [InlineData("")]
        [InlineData("[0-9]+")]
        public void Translation_GroupPatternWithoutAName_IsAllowed(string groupPattern)
        {
            // The translation editor leaves an empty entry behind when no groups are needed.
            FileTranslation translation = FileTranslation.getDefaultInstance();
            translation.NamedRegexGroups = new[] { translation.NamedRegexGroups[0], groupPattern };

            Assert.Null(translation.FindProblem());
        }

        [Fact]
        public void Translation_OnlyTheLastNameInAGroupPatternCanBeUsed()
        {
            // replaceRegex substitutes the last named group in a pattern and ignores any others.
            FileTranslation translation = FileTranslation.getDefaultInstance();
            translation.NamedRegexGroups = new[] { "(?<First>[0-9]+)_(?<FileName>[0-9]+)" };
            translation.XboxFileID = "${First}";

            Assert.Contains("${First}", translation.FindProblem());
        }

        [Fact]
        public void Translation_SubstitutionWithoutGroup_IsReported()
        {
            FileTranslation translation = FileTranslation.getDefaultInstance();
            translation.XboxFileID = "Slot${SlotNumber}";

            Assert.Contains("${SlotNumber}", translation.FindProblem());
        }

        [Fact]
        public void Translation_BuiltInProfileSubstitutions_AreAllowed()
        {
            FileTranslation translation = FileTranslation.getDefaultInstance();
            translation.NonXboxFilename = "${XboxProfileID}\\${XboxProfileID_Int}\\\\${FileName}";

            Assert.Null(translation.FindProblem());
        }

        [Fact]
        public void Translation_ValueThatIsNotAValidPattern_IsReported()
        {
            FileTranslation translation = FileTranslation.getDefaultInstance();
            translation.ContainerName1 = "Save(${FileName}";

            Assert.Contains("not a valid pattern", translation.FindProblem());
        }

        [Fact]
        public void Translation_DefaultInstance_HasNoProblems()
        {
            Assert.Null(FileTranslation.getDefaultInstance().FindProblem());
        }
    }
}
