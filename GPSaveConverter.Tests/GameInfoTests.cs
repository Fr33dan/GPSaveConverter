using System.Collections.Generic;
using Xunit;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class GameInfoTests
    {
        [Fact]
        public void ToString_WithName_ReturnsName()
        {
            var info = new GameInfo { Name = "Hades", PackageName = "HadesXbox" };

            Assert.Equal("Hades", info.ToString());
        }

        [Fact]
        public void ToString_NullName_ReturnsPackageName()
        {
            var info = new GameInfo { PackageName = "HadesXbox" };

            Assert.Equal("HadesXbox", info.ToString());
        }

        [Fact]
        public void ApplyDeserializedInfo_CopiesBaseLocation()
        {
            var target = new GameInfo();
            var source = new GameInfo { BaseNonXboxSaveLocation = "%APPDATA%\\Hades" };

            target.ApplyDeserializedInfo(source);

            Assert.Equal("%APPDATA%\\Hades", target.BaseNonXboxSaveLocation);
        }

        [Fact]
        public void ApplyDeserializedInfo_CopiesWGSProfileSuffix()
        {
            var target = new GameInfo();
            var source = new GameInfo { WGSProfileSuffix = "suffix123" };

            target.ApplyDeserializedInfo(source);

            Assert.Equal("suffix123", target.WGSProfileSuffix);
        }

        [Fact]
        public void ApplyDeserializedInfo_AddsNewTranslations()
        {
            var target = new GameInfo();
            var translation = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new string[0]
            };
            var source = new GameInfo();
            source.FileTranslations.Add(translation);

            target.ApplyDeserializedInfo(source);

            Assert.Single(target.FileTranslations);
        }

        [Fact]
        public void ApplyDeserializedInfo_SkipsDuplicateTranslations()
        {
            var translation = new FileTranslation
            {
                NonXboxFilename = "save.dat",
                XboxFileID = "blob1",
                ContainerName1 = "c1",
                ContainerName2 = "c2",
                NamedRegexGroups = new string[0]
            };
            var target = new GameInfo();
            target.FileTranslations.Add(translation);

            var source = new GameInfo();
            source.FileTranslations.Add(translation);

            target.ApplyDeserializedInfo(source);

            Assert.Single(target.FileTranslations);
        }

        [Fact]
        public void ApplyDeserializedInfo_SetsTargetProfileTypes()
        {
            var target = new GameInfo();
            var source = new GameInfo
            {
                TargetProfileTypes = new[] { NonXboxProfile.ProfileType.Steam, NonXboxProfile.ProfileType.Xbox }
            };

            target.ApplyDeserializedInfo(source);

            Assert.Equal(2, target.TargetProfileTypes.Length);
            Assert.Equal(NonXboxProfile.ProfileType.Steam, target.TargetProfileTypes[0]);
            Assert.Equal(NonXboxProfile.ProfileType.Xbox, target.TargetProfileTypes[1]);
            Assert.Equal(2, target.TargetProfiles.Length);
        }

        // Forza Horizon 5 in the game library: a Steam profile, and below it the Xbox profile.
        private const string TwoProfileLocation = "<Steam-folder>\\userdata\\<user-id>\\1551360\\remote\\<user-id2_XboxInt>\\";

        private static GameInfo GameWithTwoProfiles(string location)
        {
            var game = new GameInfo();
            game.ApplyDeserializedInfo(new GameInfo
            {
                BaseNonXboxSaveLocation = location,
                TargetProfileTypes = new[] { NonXboxProfile.ProfileType.Steam, NonXboxProfile.ProfileType.Xbox }
            });
            return game;
        }

        [Fact]
        public void WaitingForProfile_NoneOfTheLocationsProfilesPicked_IsWaiting()
        {
            GameInfo game = GameWithTwoProfiles(TwoProfileLocation);

            Assert.True(game.WaitingForProfile);
        }

        [Fact]
        public void WaitingForProfile_OnlyTheFirstPicked_IsStillWaiting()
        {
            GameInfo game = GameWithTwoProfiles(TwoProfileLocation);
            game.TargetProfiles[0] = new NonXboxProfile("12345678", 0, NonXboxProfile.ProfileType.Steam);

            Assert.True(game.WaitingForProfile);
        }

        [Fact]
        public void WaitingForProfile_EveryProfilePicked_IsNotWaiting()
        {
            GameInfo game = GameWithTwoProfiles(TwoProfileLocation);
            game.TargetProfiles[0] = new NonXboxProfile("12345678", 0, NonXboxProfile.ProfileType.Steam);
            game.TargetProfiles[1] = new NonXboxProfile("901F0976E2B74", 1, NonXboxProfile.ProfileType.Xbox);

            Assert.False(game.WaitingForProfile);
        }

        [Fact]
        public void WaitingForProfile_LocationHasNoPlaceForTheLibrarysProfiles_IsNotWaiting()
        {
            // Issues #5 and #133. The folder was picked by hand, and the library entry of the game
            // still names two profiles. There is nowhere to pick them and nothing they would change.
            GameInfo game = GameWithTwoProfiles("D:\\SteamLibrary\\steamapps\\common\\ForzaHorizon5\\");

            Assert.False(game.WaitingForProfile);
        }

        [Fact]
        public void WaitingForProfile_LocationHasAPlaceForTheFirstProfileOnly_WaitsForThatOne()
        {
            GameInfo game = GameWithTwoProfiles("C:\\Saves\\<user-id>\\");

            Assert.True(game.WaitingForProfile);

            game.TargetProfiles[0] = new NonXboxProfile("12345678", 0, NonXboxProfile.ProfileType.Steam);

            Assert.False(game.WaitingForProfile);
        }

        [Fact]
        public void WaitingForProfile_GameWithoutProfiles_IsNotWaiting()
        {
            var game = new GameInfo { BaseNonXboxSaveLocation = "%APPDATA%\\Hades\\" };

            Assert.False(game.WaitingForProfile);
        }

        [Fact]
        public void UsePickedSaveLocation_TakesTheFolderAsItIsAndDropsTheProfiles()
        {
            GameInfo game = GameWithTwoProfiles(TwoProfileLocation);

            game.UsePickedSaveLocation("D:\\Saves\\Forza");

            Assert.Equal("D:\\Saves\\Forza\\", game.BaseNonXboxSaveLocation);
            Assert.Null(game.TargetProfiles);
            Assert.False(game.WaitingForProfile);
        }
    }
}
