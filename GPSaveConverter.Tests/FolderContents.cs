using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GPSaveConverter.Tests
{
    internal static class FolderContents
    {
        /// <summary>
        /// Reads everything below a folder so that two states of it can be compared.
        /// </summary>
        /// <returns>
        /// Each file's bytes under its relative path, as text with one character per byte so that nothing
        /// is lost. Each folder as its relative path plus "\", with no text.
        /// </returns>
        internal static SortedDictionary<string, string> Read(string folder)
        {
            string root = folder.TrimEnd('\\');
            SortedDictionary<string, string> contents = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string directory in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                contents.Add(directory.Substring(root.Length + 1) + "\\", null);
            }
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                contents.Add(file.Substring(root.Length + 1), Encoding.GetEncoding("iso-8859-1").GetString(File.ReadAllBytes(file)));
            }
            return contents;
        }
    }
}
