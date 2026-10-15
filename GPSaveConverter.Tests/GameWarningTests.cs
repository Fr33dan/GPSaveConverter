using System.Linq;
using System.Text.Json;
using Xunit;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// The game library's warnings about games whose saves do not carry over.
    /// </summary>
    public class GameWarningTests
    {
        private const string Package = "Publisher.Game_abc";

        private static readonly GameWarning EitherWay = new GameWarning { PackageName = Package, Text = "Either way." };
        private static readonly GameWarning FromXboxOnly = new GameWarning { PackageName = Package, Direction = GameWarning.FromXbox, Text = "From Xbox only." };
        private static readonly GameWarning ToXboxOnly = new GameWarning { PackageName = Package, Direction = GameWarning.ToXbox, Text = "To Xbox only." };

        [Fact]
        public void AppliesTo_WarningWithNoDirection_AppliesBothWays()
        {
            Assert.True(EitherWay.AppliesTo(Package, true));
            Assert.True(EitherWay.AppliesTo(Package, false));
        }

        [Fact]
        public void AppliesTo_WarningAboutOneDirection_AppliesThatWayOnly()
        {
            Assert.True(FromXboxOnly.AppliesTo(Package, false));
            Assert.False(FromXboxOnly.AppliesTo(Package, true));
            Assert.True(ToXboxOnly.AppliesTo(Package, true));
            Assert.False(ToXboxOnly.AppliesTo(Package, false));
        }

        [Fact]
        public void AppliesTo_OtherGame_DoesNotApply()
        {
            Assert.False(EitherWay.AppliesTo("Publisher.Other_abc", true));
            Assert.True(EitherWay.AppliesTo("publisher.GAME_abc", true));
        }

        [Fact]
        public void WithDirection_SaysWhichWay()
        {
            Assert.Equal("Either way.", EitherWay.WithDirection());
            Assert.Equal("Copying from Xbox: From Xbox only.", FromXboxOnly.WithDirection());
            Assert.Equal("Copying to Xbox: To Xbox only.", ToXboxOnly.WithDirection());
        }

        [Fact]
        public void FindProblem_CompleteWarnings_ReturnNull()
        {
            Assert.Null(EitherWay.FindProblem());
            Assert.Null(FromXboxOnly.FindProblem());
            Assert.Null(ToXboxOnly.FindProblem());
        }

        [Theory]
        [InlineData(null, "Text.", null, "PackageName")]
        [InlineData(Package, "", null, "Text")]
        [InlineData(Package, "Text.", "Both", "Direction")]
        public void FindProblem_Mistake_IsReported(string packageName, string text, string direction, string expected)
        {
            Assert.Contains(expected, new GameWarning { PackageName = packageName, Text = text, Direction = direction }.FindProblem());
        }

        [Fact]
        public void Library_WarningsFor_GivesTheGamesOwnInOrder()
        {
            StoredGameLibrary library = JsonSerializer.Deserialize<StoredGameLibrary>("{ \"Version\": \"2026-01-31\", \"GameInfo\": [], \"Warnings\": [ "
                + "{ \"PackageName\": \"" + Package + "\", \"Text\": \"First.\" }, { \"PackageName\": \"Publisher.Other_abc\", \"Text\": \"Other.\" }, "
                + "{ \"PackageName\": \"" + Package + "\", \"Direction\": \"ToXbox\", \"Text\": \"Second.\" } ] }");

            Assert.Equal(new[] { "First.", "Second." }, library.WarningsFor(Package).Select(w => w.Text));
            Assert.Empty(library.WarningsFor("Publisher.None_abc"));
        }

        [Fact]
        public void Library_WithoutWarnings_HasNoneForAnyGame()
        {
            StoredGameLibrary library = JsonSerializer.Deserialize<StoredGameLibrary>("{ \"Version\": \"2026-01-31\", \"GameInfo\": [] }");

            Assert.Empty(library.WarningsFor(Package));
        }

        [Fact]
        public void Library_FindProblem_ReportsAWarningWithAMistake()
        {
            StoredGameLibrary library = JsonSerializer.Deserialize<StoredGameLibrary>("{ \"Version\": \"2026-01-31\", \"GameInfo\": [], \"Warnings\": [ { \"PackageName\": \"" + Package + "\" } ] }");

            Assert.Contains("has no Text", library.FindProblem(true));
        }

        private static StoredGameLibrary Shipped()
        {
            return JsonSerializer.Deserialize<StoredGameLibrary>(GPSaveConverter.Properties.Resources.GameLibrary);
        }

        [Fact]
        public void GameLibrary_EachGameIsWarnedAboutOnceForEachDirection()
        {
            var repeated = Shipped().Warnings.GroupBy(w => w.PackageName.ToLowerInvariant() + "|" + w.Direction).Where(g => g.Count() > 1).Select(g => g.Key);

            Assert.Empty(repeated);
        }

        [Fact]
        public void GameLibrary_EachWarningNamesItsGameAndAnIssue()
        {
            foreach (GameWarning warning in Shipped().Warnings)
            {
                Assert.False(string.IsNullOrEmpty(warning.Name), warning.PackageName + " has no Name.");
                Assert.True(warning.Text.Contains("#"), warning.Name + ": the text names no issue.");
            }
        }

        [Fact]
        public void GameLibrary_DoomEternal_IsWarnedAboutFromXboxOnly()
        {
            // A container made for a Steam save was loaded by the Xbox version in a trial on 2026-10-10.
            // The other way, the Steam version called the copy corrupt (#120).
            GameWarning doom = Assert.Single(Shipped().WarningsFor("BethesdaSoftworks.DOOMEternal-PC_3275kfvn8vcwc"));

            Assert.Equal(GameWarning.FromXbox, doom.Direction);
        }
    }
}
