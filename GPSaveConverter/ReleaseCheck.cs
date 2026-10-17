using System;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter
{
    /// <summary>
    /// Asks GitHub which release is the newest, so that the main window can say when this copy is
    /// out of date. Drafts and pre-releases do not count: GitHub leaves them out of its answer.
    /// </summary>
    internal static class ReleaseCheck
    {
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();

        /// <summary>Where GitHub says, as JSON, which release is the newest.</summary>
        internal const string LatestReleaseAddress = "https://api.github.com/repos/Fr33dan/GPSaveConverter/releases/latest";

        /// <summary>The page the newest release is downloaded from.</summary>
        internal const string DownloadPage = "https://github.com/Fr33dan/GPSaveConverter/releases/latest";

        /// <summary>
        /// The tag of the newest release, such as v.0.4.17, if it is newer than this copy.
        /// </summary>
        /// <param name="thisVersion">This copy's file version, as <see cref="ErrorReport.Version"/> gives it.</param>
        /// <returns>Null if this copy is the newest, and also if GitHub cannot be reached or gives an answer that is not understood.</returns>
        internal static string NewerRelease(IHttpClient http, string thisVersion)
        {
            Version current = VersionOf(thisVersion);
            if (current == null)
            {
                return null;
            }

            try
            {
                JsonNode answer = JsonNode.Parse(http.DownloadString(LatestReleaseAddress));
                string tag = (string)answer?["tag_name"];
                Version latest = VersionOf(tag);
                return latest != null && latest > current ? tag : null;
            }
            catch (Exception e)
            {
                // Offline, or GitHub turned the question away. It is asked again at the next start.
                logger.Warn(e, "Unable to check for a newer release");
                return null;
            }
        }

        /// <summary>
        /// Reads the version in a release tag (v.0.4.17, v.0.4.5.4, v0.2.0) or a file version
        /// (0.4.17.0). A part left off counts as 0, so v.0.4.17 and 0.4.17.0 are the same.
        /// </summary>
        /// <returns>Null if it is not a version.</returns>
        internal static Version VersionOf(string text)
        {
            Match number = Regex.Match(text ?? string.Empty, @"^(?:v\.?)?([0-9]+(?:\.[0-9]+){1,3})\z");
            Version parsed;
            if (!number.Success || !Version.TryParse(number.Groups[1].Value, out parsed))
            {
                return null;
            }
            return new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
        }
    }
}
