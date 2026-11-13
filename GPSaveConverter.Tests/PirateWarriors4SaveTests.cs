using System;
using System.IO;
using System.Text;
using GPSaveConverter.Library;
using Xunit;

namespace GPSaveConverter.Tests
{
    public class PirateWarriors4SaveTests
    {
        [Fact]
        public void SlotDataPreservesProgressAndWritesMenuMetadata()
        {
            byte[] progress = new byte[2560000];
            progress[0] = 0x1A;
            progress[progress.Length - 1] = 0xAB;
            byte[] plain = DecodeAndVerify(
                PirateWarriors4Save.FromXbox("SlotData0", "0000007 012 34 056 078 090", progress),
                2561564);

            const string menu = "012 34 056 078 090\0";
            Assert.Equal(menu, Encoding.ASCII.GetString(plain, 0, menu.Length));
            Assert.Equal(menu, Encoding.Unicode.GetString(plain, 0x200, menu.Length * 2));
            using (var reader = new BinaryReader(new MemoryStream(plain)))
            {
                reader.BaseStream.Position = 0x600;
                foreach (int expected in new[] { 1, 12, 34, 56, 78, 90 })
                {
                    Assert.Equal(expected, reader.ReadInt32());
                }
                Assert.Equal(progress, reader.ReadBytes(progress.Length));
            }
        }

        [Fact]
        public void SystemDataHasNoSlotHeader()
        {
            byte[] payload = new byte[10240];
            payload[6] = 0xA5;
            Assert.Equal(payload, DecodeAndVerify(
                PirateWarriors4Save.FromXbox("SystemData", null, payload), 10244));
        }

        [Fact]
        public void RejectsUnknownSlotMetadataInsteadOfWritingAnInvalidSave()
        {
            Assert.Throws<InvalidDataException>(() =>
                PirateWarriors4Save.FromXbox("SlotData0", "unknown", new byte[2560000]));
        }

        private static byte[] DecodeAndVerify(byte[] encoded, int size)
        {
            Assert.Equal(size, encoded.Length);
            byte[] plain = new byte[size - 4];
            uint state = BitConverter.ToUInt16(encoded, 2);
            for (int offset = 0; offset < plain.Length; offset += 4)
            {
                state = unchecked(state * 0x5B1A7851u + 0xCE4Eu);
                Array.Copy(BitConverter.GetBytes(BitConverter.ToUInt32(encoded, offset + 4) ^ state),
                    0, plain, offset, 4);
            }

            ushort checksum = 0;
            for (int offset = 0; offset < plain.Length; offset += 2)
            {
                checksum = unchecked((ushort)(checksum + BitConverter.ToUInt16(plain, offset)));
            }
            Assert.Equal(BitConverter.ToUInt16(encoded, 0), checksum);
            return plain;
        }
    }
}
