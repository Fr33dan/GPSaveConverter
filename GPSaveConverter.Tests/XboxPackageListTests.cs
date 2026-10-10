using System;
using System.IO;
using System.Linq;
using NSubstitute;
using Xunit;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;
using GPSaveConverter.Xbox;

namespace GPSaveConverter.Tests
{
    [Collection("Application statics")]
    public class XboxPackageListTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "gpsc", Guid.NewGuid().ToString("N").Substring(0, 8));

        public XboxPackageListTests()
        {
            // The list asks the game library about each package it finds, so the library gets its
            // stand-ins first. Nothing here may reach the real settings, the network or PowerShell.
            ISettingsProvider settings = Substitute.For<ISettingsProvider>();
            settings.UserGameLibrary = "{ \"Version\": \"2026-01-01\", \"GameInfo\": [] }";
            settings.DefaultGameLibrary = settings.UserGameLibrary;
            settings.AllowWebDataFetch = false;
            GameLibrary.Settings = settings;
            GameLibrary.HttpClient = Substitute.For<IHttpClient>();
            GameLibrary.ScriptRunner = Substitute.For<IScriptRunner>();
            GameLibrary.Initialize().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            XboxPackageList.Environment = new DefaultEnvironment();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        /// <summary>Makes a folder to stand in for %LOCALAPPDATA%, with a save for each package named.</summary>
        private string LocalAppDataWith(string name, params string[] packagesWithASave)
        {
            string localAppData = Path.Combine(root, name);
            Directory.CreateDirectory(Path.Combine(localAppData, "Packages"));
            foreach (string package in packagesWithASave)
            {
                Directory.CreateDirectory(Path.Combine(localAppData, "Packages", package, "SystemAppData", "wgs", "0009000000000001_0000000000000000000000001234ABCD"));
            }
            return localAppData;
        }

        private static void PointAt(string localAppData)
        {
            IEnvironment environment = Substitute.For<IEnvironment>();
            environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Returns(localAppData);
            XboxPackageList.Environment = environment;
        }

        [Fact]
        public void GetList_ListsTheGamesWithASaveInThePackagesFolderItIsPointedAt()
        {
            string localAppData = LocalAppDataWith("one", "Publisher.GameOne_abc", "Publisher.GameTwo_abc");
            // Installed, never saved: no profile folder under "wgs".
            Directory.CreateDirectory(Path.Combine(localAppData, "Packages", "Publisher.NoSave_abc", "SystemAppData", "wgs"));
            // Not a game at all.
            Directory.CreateDirectory(Path.Combine(localAppData, "Packages", "Microsoft.WindowsCalculator_abc", "LocalState"));
            PointAt(localAppData);

            GameInfo[] list = XboxPackageList.GetList();

            Assert.Equal(new[] { "Publisher.GameOne_abc", "Publisher.GameTwo_abc" }, list.Select(game => game.PackageName).OrderBy(name => name));
        }

        [Fact]
        public void GetList_ReadsTheFolderEachTimeItIsAsked()
        {
            // The list used to be read once, the first time the class was touched by anything. A
            // test that only swapped in a stand-in set that off, against the real Packages folder,
            // and whatever was found there started the game library loading on its own.
            PointAt(LocalAppDataWith("first", "Publisher.GameOne_abc"));
            Assert.Equal("Publisher.GameOne_abc", Assert.Single(XboxPackageList.GetList()).PackageName);

            PointAt(LocalAppDataWith("second", "Publisher.GameTwo_abc"));
            Assert.Equal("Publisher.GameTwo_abc", Assert.Single(XboxPackageList.GetList()).PackageName);
        }
    }
}
