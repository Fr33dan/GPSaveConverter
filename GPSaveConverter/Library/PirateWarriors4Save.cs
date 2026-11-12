using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GPSaveConverter.Library
{
    internal static class PirateWarriors4Save
    {
        internal const string PackageName = "NAMCOBANDAIGamesInc.ONEPIECEPIRATEWARRIORS4Windows_gdy2aq6ez762w";

        internal static byte[] FromXbox(string containerName, string displayName, byte[] payload)
        {
            bool systemData = containerName == "SystemData";
            if ((!systemData && !Regex.IsMatch(containerName, @"\ASlotData[0-4]\z"))
                || payload.Length != (systemData ? 0x2800 : 0x271000))
            {
                throw new InvalidDataException("Unexpected Pirate Warriors 4 Windows save type or size.");
            }

            byte[] seed = new byte[2];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(seed);
            }

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0); // Checksum, filled after assembling the plaintext.
                writer.Write(seed);
                if (!systemData)
                {
                    WriteSlotHeader(writer, displayName);
                }
                writer.Write(payload);
                writer.Flush();
                byte[] result = stream.ToArray();

                // The worldwide Steam build stores a 16-bit sum of the plaintext words.
                uint sum = 0;
                for (int i = 4; i < result.Length; i += 2)
                {
                    sum += (uint)(result[i] | (result[i + 1] << 8));
                }
                result[0] = (byte)sum;
                result[1] = (byte)(sum >> 8);

                uint state = (uint)(seed[0] | (seed[1] << 8));
                for (int i = 4; i < result.Length; i += 4)
                {
                    state = unchecked(state * 0x5B1A7851u + 0xCE4Eu);
                    for (int b = 0; b < 4; b++)
                    {
                        result[i + b] ^= (byte)(state >> (8 * b));
                    }
                }
                return result;
            }
        }

        private static void WriteSlotHeader(BinaryWriter writer, string displayName)
        {
            // WGS prefixes the slot menu statistics with a container sequence number.
            Match match = Regex.Match(displayName ?? string.Empty,
                @"\A[0-9]{7} (?<menu>([0-9]{3}) ([0-9]{2}) ([0-9]{3}) ([0-9]{3}) ([0-9]{3}))\z");
            if (!match.Success)
            {
                throw new InvalidDataException("Unrecognized Pirate Warriors 4 Windows slot metadata.");
            }

            string menu = match.Groups["menu"].Value;
            byte[] ascii = Encoding.ASCII.GetBytes(menu);
            byte[] unicode = Encoding.Unicode.GetBytes(menu);
            writer.Write(ascii);
            writer.Write(new byte[0x200 - ascii.Length]);
            writer.Write(unicode);
            writer.Write(new byte[0x400 - unicode.Length]);
            writer.Write(1u); // Occupied slot.
            foreach (string value in menu.Split(' '))
            {
                writer.Write(uint.Parse(value, CultureInfo.InvariantCulture));
            }
        }
    }
}
