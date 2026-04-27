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
    }
}
