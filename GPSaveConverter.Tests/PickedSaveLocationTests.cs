using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// A save folder picked by hand has to stay usable when the tool is started again. The library is
    /// stored when the window closes and read back at the next start, and these tests go through both.
    /// </summary>
    [Collection("Application statics")]
    public class PickedSaveLocationTests
    {
        private const string Package = "Microsoft.624F8B84B80_8wekyb3d8bbwe";

        // The entry Forza Horizon 5 has in the game library.
        private const string Library = "{ \"Version\": \"2026-01-01\", \"GameInfo\": [ { \"PackageName\": \"" + Package + "\", "
            + "\"BaseNonXboxSaveLocation\": \"<Steam-folder>\\\\userdata\\\\<user-id>\\\\1551360\\\\remote\\\\<user-id2_XboxInt>\\\\\", "
            + "\"TargetProfileTypes\": [ \"Steam\", \"Xbox\" ] } ] }";

        /// <summary>Starts the library the way the application does, from what the settings hold.</summary>
        private static void Start(string storedLibrary)
        {
            // Nothing here may reach the real settings, the network or PowerShell.
            ISettingsProvider settings = Substitute.For<ISettingsProvider>();
            settings.UserGameLibrary = storedLibrary;
            settings.DefaultGameLibrary = storedLibrary;
            settings.AllowWebDataFetch = false;
            GameLibrary.Settings = settings;
            GameLibrary.HttpClient = Substitute.For<IHttpClient>();
            GameLibrary.ScriptRunner = Substitute.For<IScriptRunner>();
            GameLibrary.Initialize().GetAwaiter().GetResult();
        }

        /// <summary>What selecting the game in the package list does to it.</summary>
        private static async Task<GameInfo> Select()
        {
            GameInfo game = GameLibrary.getGameInfo(Package);
            await GameLibrary.PopulateNonUWPInformation(game);
            return game;
        }

        [Fact]
        public async Task PickedFolder_AfterTheToolIsStartedAgain_StillWaitsForNoProfile()
        {
            Start(Library);
            GameInfo game = await Select();
            Assert.True(game.WaitingForProfile);

            game.UsePickedSaveLocation("D:\\Saves\\Forza");
            Assert.False(game.WaitingForProfile);

            // Closing the window stores the library, and the next start reads it back.
            Start(GameLibrary.GetLibraryJson());
            GameInfo afterRestart = await Select();

            Assert.NotSame(game, afterRestart);
            Assert.Equal("D:\\Saves\\Forza\\", afterRestart.BaseNonXboxSaveLocation);
            // Issues #5 and #133: the library's two profiles are back, and the folder has no place for them.
            Assert.Equal(2, afterRestart.TargetProfiles.Length);
            Assert.False(afterRestart.WaitingForProfile);
        }

        [Fact]
        public async Task LibraryLocation_AfterTheToolIsStartedAgain_WaitsForItsProfilesAsBefore()
        {
            Start(Library);
            await Select();

            Start(GameLibrary.GetLibraryJson());
            GameInfo afterRestart = await Select();

            Assert.Contains("<user-id>", afterRestart.BaseNonXboxSaveLocation);
            Assert.True(afterRestart.WaitingForProfile);
        }
    }
}
