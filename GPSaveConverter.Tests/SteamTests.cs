using Xunit;
using GPSaveConverter.Library;

namespace GPSaveConverter.Tests
{
    public class SteamTests
    {
        [Theory]
        [InlineData("0", 0x0110000100000000UL)]
        [InlineData("1", 0x0110000100000001UL)]
        [InlineData("123456789", 0x0110000100000000UL | 123456789UL)]
        public void GetSteamID64_ConvertsCorrectly(string steam3ID, ulong expected)
        {
            ulong result = Steam.GetSteamID64(steam3ID);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetSteamID64_Zero_ReturnsBaseValue()
        {
            ulong result = Steam.GetSteamID64("0");

            Assert.Equal(76561197960265728UL, result);
        }
    }
}
