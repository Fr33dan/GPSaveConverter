using System;
using System.Collections.Generic;
using Xunit;

namespace GPSaveConverter.Tests
{
    public class TransferLoopTests
    {
        private static readonly string[] Files = { "a.sav", "b.sav", "c.sav" };

        /// <summary>Stops a test that would otherwise never end.</summary>
        private const int TooManyAttempts = 20;

        [Fact]
        public void Run_NothingFails_CopiesEachFileOnce()
        {
            List<string> copied = new List<string>();

            bool finished = TransferLoop.Run(Files, copied.Add, (file, e) => throw new InvalidOperationException("No error expected."));

            Assert.True(finished);
            Assert.Equal(Files, copied);
        }

        [Fact]
        public void Run_RetryThatWorks_GoesOnToTheNextFile()
        {
            // The window's own loop kept the "Retry" answer after the retry had worked, and so copied
            // the same file again without end.
            List<string> attempts = new List<string>();
            bool failedOnce = false;

            bool finished = TransferLoop.Run(Files,
                file =>
                {
                    attempts.Add(file);
                    Assert.True(attempts.Count < TooManyAttempts, "The same file is being copied over and over.");
                    if (file == "a.sav" && !failedOnce)
                    {
                        failedOnce = true;
                        throw new InvalidOperationException("The file is in use.");
                    }
                },
                (file, e) => AfterError.Retry);

            Assert.True(finished);
            Assert.Equal(new[] { "a.sav", "a.sav", "b.sav", "c.sav" }, attempts);
        }

        [Fact]
        public void Run_RetryThatKeepsFailing_AsksAgainEachTime()
        {
            List<string> attempts = new List<string>();
            int asked = 0;

            bool finished = TransferLoop.Run(Files,
                file =>
                {
                    attempts.Add(file);
                    if (file == "b.sav")
                    {
                        throw new InvalidOperationException("The file is in use.");
                    }
                },
                (file, e) => ++asked < 3 ? AfterError.Retry : AfterError.Skip);

            Assert.True(finished);
            Assert.Equal(3, asked);
            Assert.Equal(new[] { "a.sav", "b.sav", "b.sav", "b.sav", "c.sav" }, attempts);
        }

        [Fact]
        public void Run_Skip_LeavesTheFileAndCopiesTheRest()
        {
            List<string> copied = new List<string>();

            bool finished = TransferLoop.Run(Files,
                file =>
                {
                    if (file == "b.sav")
                    {
                        throw new InvalidOperationException("No translation.");
                    }
                    copied.Add(file);
                },
                (file, e) => AfterError.Skip);

            Assert.True(finished);
            Assert.Equal(new[] { "a.sav", "c.sav" }, copied);
        }

        [Fact]
        public void Run_Abort_StopsBeforeTheRemainingFiles()
        {
            List<string> attempts = new List<string>();

            bool finished = TransferLoop.Run(Files,
                file =>
                {
                    attempts.Add(file);
                    if (file == "b.sav")
                    {
                        throw new InvalidOperationException("No translation.");
                    }
                },
                (file, e) => AfterError.Abort);

            Assert.False(finished);
            Assert.Equal(new[] { "a.sav", "b.sav" }, attempts);
        }

        [Fact]
        public void Run_AFailure_IsPassedOnWithItsFile()
        {
            string reportedFile = null;
            Exception reported = null;
            Exception failure = new InvalidOperationException("The file is in use.");

            TransferLoop.Run(Files,
                file =>
                {
                    if (file == "c.sav")
                    {
                        throw failure;
                    }
                },
                (file, e) =>
                {
                    reportedFile = file;
                    reported = e;
                    return AfterError.Skip;
                });

            Assert.Equal("c.sav", reportedFile);
            Assert.Same(failure, reported);
        }
    }
}
