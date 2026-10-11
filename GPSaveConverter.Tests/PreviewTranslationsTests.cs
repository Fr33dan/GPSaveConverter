using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;
using GPSaveConverter.Xbox;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// Translations that are still being tested are published in a second library file. A copy of
    /// the tool with the option for them turned on downloads it and tries those translations first.
    /// They sit on top of the user's library and never become part of it.
    /// </summary>
    [Collection("Application statics")]
    public class PreviewTranslationsTests : IDisposable
    {
        private const string Package = FakeXboxSave.PackageName;

        // The translation someone wrote for themselves, which sends the file to a container that does not exist.
        private const string UsersOwn = "{ \"NonXboxFilename\": \"slot${Slot}.sav\", \"XboxFileID\": \"Data\", \"ContainerName1\": \"Wrong${Slot}\", \"ContainerName2\": \"Wrong${Slot}\", \"NamedRegexGroups\": [ \"(?<Slot>[0-9]+)\" ] }";

        // The one being tested, which sends it to the container the save has.
        private const string BeingTested = "{ \"NonXboxFilename\": \"slot${Slot}.sav\", \"XboxFileID\": \"Data\", \"ContainerName1\": \"Save${Slot}\", \"ContainerName2\": \"Save${Slot}\", \"NamedRegexGroups\": [ \"(?<Slot>[0-9]+)\" ] }";

        // A second go at it, after the first turned out wrong.
        private const string BeingTestedSecondGo = "{ \"NonXboxFilename\": \"slot${Slot}.sav\", \"XboxFileID\": \"Data\", \"ContainerName1\": \"Save${Slot}\", \"ContainerName2\": \"\", \"NamedRegexGroups\": [ \"(?<Slot>[0-9]+)\" ] }";

        private readonly FakeXboxSave save = new FakeXboxSave();
        private readonly ISettingsProvider settings = Substitute.For<ISettingsProvider>();
        private readonly IHttpClient http = Substitute.For<IHttpClient>();

        public PreviewTranslationsTests()
        {
            save.WithXboxFile("Save1", "Save1", "Data", "xbox slot 1")
                .WithNonXboxFile("slot1.sav", "steam slot 1")
                .Build();

            settings.PreviewGameLibrary = string.Empty;
            settings.AllowWebDataFetch = true;

            // Nothing else here. The package list is not touched until Start has put the stand-ins
            // in place: its first use reads the library, and would start the real one.
        }

        public void Dispose()
        {
            XboxPackageList.Environment = new DefaultEnvironment();
            GameLibrary.nonXboxFiles.Clear();
            save.Dispose();
        }

        private static string Library(params string[] games)
        {
            return "{ \"Version\": \"2026-01-01\", \"GameInfo\": [ " + string.Join(", ", games) + " ] }";
        }

        private string Game(string translations, string location = null)
        {
            return "{ \"PackageName\": \"" + Package + "\", "
                + (location == null ? string.Empty : "\"BaseNonXboxSaveLocation\": " + System.Text.Json.JsonSerializer.Serialize(location) + ", ")
                + "\"FileTranslations\": [ " + translations + " ] }";
        }

        private void PreviewLibraryIs(string library)
        {
            http.DownloadString(GameLibrary.PreviewLibraryURL).Returns(library);
        }

        /// <summary>Starts the library the way the application does, from what the settings hold.</summary>
        private void Start(string usersLibrary)
        {
            settings.UserGameLibrary = usersLibrary;
            settings.DefaultGameLibrary = usersLibrary;
            http.DownloadString(Arg.Is<string>(url => url.EndsWith("/GameLibrary.json"))).Returns(usersLibrary);

            GameLibrary.Settings = settings;
            GameLibrary.HttpClient = http;
            GameLibrary.ScriptRunner = Substitute.For<IScriptRunner>();
            GameLibrary.Environment = new DefaultEnvironment();
            GameLibrary.Registry = Substitute.For<IRegistry>();
            GameLibrary.Initialize().GetAwaiter().GetResult();

            IEnvironment environment = Substitute.For<IEnvironment>();
            environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Returns(save.LocalAppData);
            XboxPackageList.Environment = environment;
        }

        /// <summary>What selecting the game in the package list does to it.</summary>
        private static async Task<GameInfo> Select()
        {
            GameInfo game = GameLibrary.getGameInfo(Package);
            await GameLibrary.PopulateNonUWPInformation(game);
            return game;
        }

        /// <summary>The Xbox container a click on "slot1.sav" would highlight, or null if none.</summary>
        private static async Task<string> ContainerFor(GameInfo game)
        {
            await game.refreshNonXboxSaveFiles();
            XboxContainerIndex index = new XboxContainerIndex(game, FakeXboxSave.ProfileID);
            index.getFileList();

            XboxFileInfo match = game.getXboxFileVersion(index, GameLibrary.nonXboxFiles.Single(f => f.RelativePath == "slot1.sav"));
            return match == null ? null : match.ContainerName1;
        }

        [Fact]
        public async Task OptionOff_NothingIsDownloadedAndNothingIsAdded()
        {
            settings.UsePreviewTranslations = false;
            PreviewLibraryIs(Library(Game(BeingTested)));

            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            GameInfo game = await Select();

            http.DidNotReceive().DownloadString(GameLibrary.PreviewLibraryURL);
            Assert.Empty(game.PreviewTranslations);
            Assert.Null(await ContainerFor(game));
        }

        [Fact]
        public async Task OptionOn_TheTranslationBeingTestedIsTriedBeforeTheUsersOwn()
        {
            // The user's own attempt matches the file as well. It used to have to be removed by hand,
            // because the first translation that matches is the one used.
            settings.UsePreviewTranslations = true;
            PreviewLibraryIs(Library(Game(BeingTested)));

            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            GameInfo game = await Select();

            Assert.Single(game.PreviewTranslations);
            Assert.Single(game.FileTranslations);
            Assert.Equal("Save1", await ContainerFor(game));
        }

        [Fact]
        public async Task OptionOn_NothingOfItBecomesPartOfTheUsersLibrary()
        {
            settings.UsePreviewTranslations = true;
            PreviewLibraryIs(Library(Game(BeingTested)));
            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            await Select();

            // Closing the window stores the library.
            string stored = GameLibrary.GetLibraryJson();
            Assert.Contains("Wrong${Slot}", stored);
            Assert.DoesNotContain("Save${Slot}", stored);

            // With the option turned off, the next start has only what the user had.
            settings.UsePreviewTranslations = false;
            Start(stored);
            GameInfo game = await Select();

            Assert.Empty(game.PreviewTranslations);
            Assert.Single(game.FileTranslations);
            Assert.Null(await ContainerFor(game));
        }

        [Fact]
        public async Task TranslationBeingTestedIsChanged_TheNewOneTakesThePlaceOfTheOld()
        {
            // Merged into a library, the second go would have been added after the first, and the
            // first would still have been the one used.
            settings.UsePreviewTranslations = true;
            PreviewLibraryIs(Library(Game(BeingTested)));
            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            await Select();

            PreviewLibraryIs(Library(Game(BeingTestedSecondGo)));
            Start(GameLibrary.GetLibraryJson());
            GameInfo game = await Select();

            FileTranslation inForce = Assert.Single(game.PreviewTranslations);
            Assert.Equal(string.Empty, inForce.ContainerName2);
        }

        [Fact]
        public async Task TranslationBeingTestedIsWithdrawn_ItIsGoneAtTheNextStart()
        {
            settings.UsePreviewTranslations = true;
            PreviewLibraryIs(Library(Game(BeingTested)));
            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            await Select();

            // The file lists no games once every translation in it was confirmed or dropped.
            PreviewLibraryIs(Library());
            Start(GameLibrary.GetLibraryJson());
            GameInfo game = await Select();

            Assert.Empty(game.PreviewTranslations);
            Assert.Null(await ContainerFor(game));
        }

        [Fact]
        public async Task DownloadFails_TheCopyFromLastTimeIsUsed()
        {
            settings.UsePreviewTranslations = true;
            PreviewLibraryIs(Library(Game(BeingTested)));
            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            await Select();

            http.DownloadString(GameLibrary.PreviewLibraryURL).Throws(new System.Net.WebException("The remote name could not be resolved"));
            Start(GameLibrary.GetLibraryJson());
            GameInfo game = await Select();

            Assert.Equal("Save1", await ContainerFor(game));
        }

        [Theory]
        [InlineData("<html>Not Found</html>")]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("{ \"Version\": \"soon\", \"GameInfo\": [] }")]
        [InlineData("{ \"Version\": \"2026-01-01\", \"GameInfo\": [ { \"PackageName\": \"A\", \"FileTranslations\": [ { \"NonXboxFilename\": \"${Missing}\", \"XboxFileID\": \"a\", \"ContainerName1\": \"a\", \"ContainerName2\": \"a\", \"NamedRegexGroups\": [] } ] } ] }")]
        public async Task DownloadIsNotAUsableLibrary_IsIgnoredAndTheCopyFromLastTimeIsUsed(string download)
        {
            settings.UsePreviewTranslations = true;
            PreviewLibraryIs(Library(Game(BeingTested)));
            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            await Select();
            string kept = settings.PreviewGameLibrary;

            PreviewLibraryIs(download);
            Start(GameLibrary.GetLibraryJson());
            GameInfo game = await Select();

            Assert.Equal(kept, settings.PreviewGameLibrary);
            Assert.Equal("Save1", await ContainerFor(game));
        }

        [Fact]
        public async Task OptionTurnedOnAndOffWhileRunning_AGameAlreadySelectedGetsAndLosesThem()
        {
            settings.UsePreviewTranslations = false;
            PreviewLibraryIs(Library(Game(BeingTested)));
            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            GameInfo game = await Select();
            Assert.Null(await ContainerFor(game));

            // What saving the preferences does.
            settings.UsePreviewTranslations = true;
            GameLibrary.RefreshPreviewTranslations();
            Assert.Equal("Save1", await ContainerFor(game));

            settings.UsePreviewTranslations = false;
            GameLibrary.RefreshPreviewTranslations();
            Assert.Null(await ContainerFor(game));
        }

        [Fact]
        public async Task InternetAccessNotAllowed_NothingIsDownloaded()
        {
            settings.UsePreviewTranslations = true;
            settings.AllowWebDataFetch = false;
            PreviewLibraryIs(Library(Game(BeingTested)));

            Start(Library(Game(UsersOwn, save.NonXboxFolder)));
            GameInfo game = await Select();

            http.DidNotReceive().DownloadString(Arg.Any<string>());
            Assert.Empty(game.PreviewTranslations);
        }

        [Fact]
        public async Task SaveLocationGivenWithATranslationBeingTested_IsUsedOnlyWhenTheGameHasNone()
        {
            settings.UsePreviewTranslations = true;
            PreviewLibraryIs(Library(Game(BeingTested, "%APPDATA%\\Game\\Saves\\")));

            // No location of the user's own: the one given with the translation is taken.
            Start(Library(Game(UsersOwn)));
            Assert.Equal("%APPDATA%\\Game\\Saves\\", (await Select()).BaseNonXboxSaveLocation);

            // A location of the user's own stays.
            Start(Library(Game(UsersOwn, "D:\\My saves\\")));
            Assert.Equal("D:\\My saves\\", (await Select()).BaseNonXboxSaveLocation);
        }
    }
}
