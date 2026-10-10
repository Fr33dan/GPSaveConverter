using System;
using System.Collections.Generic;

namespace GPSaveConverter
{
    /// <summary>
    /// What to do after a file fails to copy.
    /// </summary>
    internal enum AfterError
    {
        /// <summary>Stop the transfer.</summary>
        Abort,

        /// <summary>Try the same file again.</summary>
        Retry,

        /// <summary>Leave this file and go on to the next one.</summary>
        Skip
    }

    internal static class TransferLoop
    {
        /// <summary>
        /// Copies each file in turn and asks what to do whenever one fails.
        /// </summary>
        /// <param name="copy">Copies one file. Throws if it cannot.</param>
        /// <param name="onError">Called with the file and the failure. Its answer decides what happens next.</param>
        /// <returns>False if the transfer was aborted.</returns>
        internal static bool Run<T>(IEnumerable<T> files, Action<T> copy, Func<T, Exception, AfterError> onError)
        {
            foreach (T file in files)
            {
                bool retry;
                do
                {
                    // Cleared on every attempt. A retry that works must end the loop.
                    retry = false;
                    try
                    {
                        copy(file);
                    }
                    catch (Exception e)
                    {
                        AfterError choice = onError(file, e);
                        if (choice == AfterError.Abort)
                        {
                            return false;
                        }
                        retry = choice == AfterError.Retry;
                    }
                } while (retry);
            }
            return true;
        }
    }
}
