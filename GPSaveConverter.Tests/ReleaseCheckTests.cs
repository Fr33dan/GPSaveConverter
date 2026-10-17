using System;
using System.Net;
using GPSaveConverter.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GPSaveConverter.Tests
{
    /// <summary>
    /// The question to GitHub behind the link to a newer release at the right of the menu bar.
    /// </summary>
    public class ReleaseCheckTests
    {
        private readonly IHttpClient http = Substitute.For<IHttpClient>();

        /// <summary>GitHub's answer, cut down to the fields that matter here.</summary>
        private static string Answer(string tag)
        {
            return "{ \"html_url\": \"https://github.com/Fr33dan/GPSaveConverter/releases/tag/" + tag + "\", \"tag_name\": \"" + tag + "\", \"name\": \"" + tag + "\", \"draft\": false, \"prerelease\": false }";
        }

        [Theory]
        [InlineData("v.0.4.17", "0.4.16.0")]
        [InlineData("v.0.4.16.1", "0.4.16.0")]
        [InlineData("v.0.5.0", "0.4.16.0")]
        [InlineData("v.1.0", "0.4.16.0")]
        public void NewerRelease_ATagNewerThanThisCopy_IsGiven(string tag, string thisVersion)
        {
            http.DownloadString(ReleaseCheck.LatestReleaseAddress).Returns(Answer(tag));

            Assert.Equal(tag, ReleaseCheck.NewerRelease(http, thisVersion));
        }

        [Theory]
        [InlineData("v.0.4.16", "0.4.16.0")]
        [InlineData("v.0.4.15", "0.4.16.0")]
        // 5 is less than 16, though as text "5" comes after "16".
        [InlineData("v.0.4.5.4", "0.4.16.0")]
        [InlineData("v0.2.0", "0.4.16.0")]
        public void NewerRelease_NoTagNewerThanThisCopy_IsNull(string tag, string thisVersion)
        {
            http.DownloadString(ReleaseCheck.LatestReleaseAddress).Returns(Answer(tag));

            Assert.Null(ReleaseCheck.NewerRelease(http, thisVersion));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not JSON")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("{ \"message\": \"API rate limit exceeded\" }")]
        [InlineData("{ \"tag_name\": 17 }")]
        [InlineData("{ \"tag_name\": \"nightly\" }")]
        [InlineData("{ \"tag_name\": \"v.0.4.17-beta\" }")]
        public void NewerRelease_AnAnswerNotUnderstood_IsNull(string answer)
        {
            http.DownloadString(ReleaseCheck.LatestReleaseAddress).Returns(answer);

            Assert.Null(ReleaseCheck.NewerRelease(http, "0.4.16.0"));
        }

        [Fact]
        public void NewerRelease_Offline_IsNull()
        {
            http.DownloadString(Arg.Any<string>()).Throws(new WebException("The remote name could not be resolved"));

            Assert.Null(ReleaseCheck.NewerRelease(http, "0.4.16.0"));
        }

        [Fact]
        public void NewerRelease_ThisCopysVersionUnknown_AsksNothing()
        {
            Assert.Null(ReleaseCheck.NewerRelease(http, "unknown"));

            http.DidNotReceive().DownloadString(Arg.Any<string>());
        }

        [Theory]
        [InlineData("v.0.4.17", "0.4.17.0")]
        [InlineData("v.0.4.5.4", "0.4.5.4")]
        [InlineData("v0.2.0", "0.2.0.0")]
        [InlineData("0.4.16.0", "0.4.16.0")]
        public void VersionOf_ReadsTagsAndFileVersions(string text, string expected)
        {
            Assert.Equal(new Version(expected), ReleaseCheck.VersionOf(text));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("v.")]
        [InlineData("v.4")]
        [InlineData("v.0.4.17-beta")]
        [InlineData(" v.0.4.17")]
        [InlineData("v.0.4.17.1.2")]
        [InlineData("unknown")]
        public void VersionOf_NotAVersion_IsNull(string text)
        {
            Assert.Null(ReleaseCheck.VersionOf(text));
        }
    }
}
