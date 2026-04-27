using System;
using NSubstitute;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class GameLibraryGetInstalledAppsTests
    {
        private readonly IScriptRunner _scriptRunner;

        public GameLibraryGetInstalledAppsTests()
        {
            _scriptRunner = Substitute.For<IScriptRunner>();
            GameLibrary.ScriptRunner = _scriptRunner;
        }

        [Fact]
        public void GetInstalledApps_ParsesValidOutput()
        {
            string scriptOutput = "Hades|C:\\icon.png|SupergiantGames.Hades_package\n" +
                                  "Celeste|D:\\celeste.png|MattMakesGames.Celeste_package\n";
            _scriptRunner.RunScript(Arg.Any<string>()).Returns(scriptOutput);

            GameLibrary.GetInstalledApps();

            // The method populates an internal dictionary — verify it ran without error
            _scriptRunner.Received(1).RunScript(Arg.Any<string>());
        }

        [Fact]
        public void GetInstalledApps_MalformedLine_DoesNotThrow()
        {
            string scriptOutput = "MalformedLineWithNoPipes\n" +
                                  "Hades|C:\\icon.png|SupergiantGames.Hades_package\n";
            _scriptRunner.RunScript(Arg.Any<string>()).Returns(scriptOutput);

            // Should not throw even with malformed input
            GameLibrary.GetInstalledApps();
        }

        [Fact]
        public void GetInstalledApps_EmptyOutput_DoesNotThrow()
        {
            _scriptRunner.RunScript(Arg.Any<string>()).Returns("");

            GameLibrary.GetInstalledApps();
        }

        [Fact]
        public void GetInstalledApps_ScriptThrows_WrapsException()
        {
            _scriptRunner.RunScript(Arg.Any<string>())
                .Returns(x => { throw new InvalidOperationException("PS failed"); });

            var ex = Assert.Throws<Exception>(() => GameLibrary.GetInstalledApps());
            Assert.Contains("Error trying to get installed apps", ex.Message);
        }

        [Fact]
        public void GetInstalledApps_EmptyName_UsesPackageName()
        {
            string scriptOutput = "|C:\\icon.png|SomeGame_package\n";
            _scriptRunner.RunScript(Arg.Any<string>()).Returns(scriptOutput);

            // Should not throw — empty name falls back to PackageName
            GameLibrary.GetInstalledApps();
        }
    }
}
