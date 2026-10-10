using Xunit;

namespace GPSaveConverter.Tests
{
    public class NonXboxProfileTests
    {
        [Fact]
        public void ExpandSaveLocation_ReplacesUserIdMarker()
        {
            var profile = new NonXboxProfile("TestUser123", 0, NonXboxProfile.ProfileType.Steam);

            string result = profile.ExpandSaveLocation("C:\\Saves\\<user-id>\\game.dat");

            Assert.Equal("C:\\Saves\\TestUser123\\game.dat", result);
        }

        [Fact]
        public void ExpandSaveLocation_NoMarker_ReturnsUnchanged()
        {
            var profile = new NonXboxProfile("TestUser123", 0, NonXboxProfile.ProfileType.Steam);

            string result = profile.ExpandSaveLocation("C:\\Saves\\game.dat");

            Assert.Equal("C:\\Saves\\game.dat", result);
        }

        [Fact]
        public void ExpandSaveLocation_SecondProfile_UsesIndexedMarker()
        {
            // ProfileIndex 1 → marker is <user-id2>
            var profile = new NonXboxProfile("Player2", 1, NonXboxProfile.ProfileType.Steam);

            string result = profile.ExpandSaveLocation("C:\\Saves\\<user-id2>\\game.dat");

            Assert.Equal("C:\\Saves\\Player2\\game.dat", result);
        }

        [Fact]
        public void ExpandSaveLocation_XboxProfile_ReplacesXboxIntMarker()
        {
            // Hex "A" = decimal 10
            var profile = new NonXboxProfile("A", 0, NonXboxProfile.ProfileType.Xbox);

            string result = profile.ExpandSaveLocation("C:\\Saves\\<user-id_XboxInt>\\game.dat");

            Assert.Equal("C:\\Saves\\10\\game.dat", result);
        }

        [Fact]
        public void ExpandSaveLocation_SteamProfile_DoesNotReplaceXboxIntMarker()
        {
            var profile = new NonXboxProfile("A", 0, NonXboxProfile.ProfileType.Steam);

            string result = profile.ExpandSaveLocation("C:\\Saves\\<user-id_XboxInt>\\game.dat");

            // Steam profiles should NOT replace the _XboxInt marker
            Assert.Equal("C:\\Saves\\<user-id_XboxInt>\\game.dat", result);
        }

        [Theory]
        [InlineData(0, "C:\\Saves\\<user-id>\\", true)]
        [InlineData(0, "C:\\Saves\\<user-id_XboxInt>\\", true)]
        [InlineData(0, "C:\\Saves\\<user-id_SteamID64>\\", true)]
        [InlineData(0, "C:\\Saves\\<user-id>\\remote\\<user-id2_XboxInt>\\", true)]
        [InlineData(0, "C:\\Saves\\", false)]
        [InlineData(0, null, false)]
        // The second profile's marker starts the way the first one's does.
        [InlineData(0, "C:\\Saves\\<user-id2>\\", false)]
        [InlineData(0, "C:\\Saves\\<user-id2_XboxInt>\\", false)]
        [InlineData(1, "C:\\Saves\\<user-id2>\\", true)]
        [InlineData(1, "C:\\Saves\\<user-id>\\remote\\<user-id2_XboxInt>\\", true)]
        [InlineData(1, "C:\\Saves\\<user-id>\\", false)]
        [InlineData(1, "C:\\Saves\\<user-id_XboxInt>\\", false)]
        public void HasPlaceIn_GoesByTheProfilesOwnMarker(int index, string location, bool expected)
        {
            var profile = new NonXboxProfile(index, NonXboxProfile.ProfileType.Steam);

            Assert.Equal(expected, profile.HasPlaceIn(location));
        }
    }
}
