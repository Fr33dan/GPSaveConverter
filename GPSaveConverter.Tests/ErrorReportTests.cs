using System;
using Xunit;

namespace GPSaveConverter.Tests
{
    public class ErrorReportTests
    {
        private static Exception Thrown(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                return e;
            }
            throw new InvalidOperationException("Nothing was thrown.");
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ReadIndex()
        {
            throw new ArgumentOutOfRangeException("length", "Length cannot be less than zero.");
        }

        [Fact]
        public void Describe_SaysWhichVersionGameAndWindows()
        {
            string details = ErrorReport.Describe("The Xbox save could not be read.", Thrown(ReadIndex), "0.4.12.0", "Microsoft.SunriseBaseGame_8wekyb3d8bbwe", "26200");

            Assert.StartsWith("Xbox Save File Converter 0.4.12.0" + Environment.NewLine, details);
            Assert.Contains("Windows build: 26200" + Environment.NewLine, details);
            Assert.Contains("Game: Microsoft.SunriseBaseGame_8wekyb3d8bbwe" + Environment.NewLine, details);
        }

        [Fact]
        public void Describe_GivesTheSummaryThenTheErrorWithWhereItHappened()
        {
            string details = ErrorReport.Describe("The Xbox save could not be read.", Thrown(ReadIndex), "0.4.12.0", null, null);

            int summary = details.IndexOf("The Xbox save could not be read.");
            int error = details.IndexOf("System.ArgumentOutOfRangeException: Length cannot be less than zero.");
            Assert.True(summary >= 0 && error > summary, details);
            // The stack trace is what makes a report useful.
            Assert.Contains("ErrorReportTests.ReadIndex()", details);
        }

        [Fact]
        public void Describe_IncludesTheErrorBehindTheError()
        {
            Exception cause = Thrown(ReadIndex);
            Exception error = new InvalidOperationException("Error trying to get installed apps on your PC", cause);

            string details = ErrorReport.Describe(ErrorReport.UnexpectedProblem, error, "0.4.12.0", null, null);

            Assert.Contains("System.InvalidOperationException: Error trying to get installed apps on your PC", details);
            Assert.Contains("System.ArgumentOutOfRangeException: Length cannot be less than zero.", details);
        }

        [Fact]
        public void Describe_NothingKnownAboutGameOrWindows_SaysSo()
        {
            string details = ErrorReport.Describe(ErrorReport.UnexpectedProblem, new InvalidOperationException("Oops"), "0.4.12.0", null, null);

            Assert.Contains("Game: none selected" + Environment.NewLine, details);
            Assert.Contains("Windows build: unknown" + Environment.NewLine, details);
        }
    }
}
