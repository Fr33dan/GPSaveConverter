using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GPSaveConverter.Interfaces;

namespace GPSaveConverter
{
    /// <summary>
    /// Shows an error in a window that says what went wrong and has details to copy into an issue.
    /// Without it, an error nothing handles gets the .NET "unhandled exception" dialog.
    /// </summary>
    internal static class ErrorReport
    {
        private static readonly NLog.Logger logger = LogHelper.getClassLogger();

        internal static IRegistry Registry { get; set; } = new DefaultRegistry();

        internal const string IssuesPage = "https://github.com/Fr33dan/GPSaveConverter/issues";
        internal const string UnexpectedProblem = "The tool ran into a problem it did not expect.";

        private static bool showing;

        /// <summary>
        /// Sends every error that nothing else handles to <see cref="Show"/>. Call it before the first
        /// window is created.
        /// </summary>
        internal static void CatchUnhandledErrors()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => Show(null, UnexpectedProblem, e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => ShowFromAnyThread(e.ExceptionObject as Exception);
        }

        /// <param name="summary">One sentence on what could not be done.</param>
        internal static void Show(IWin32Window owner, string summary, Exception error)
        {
            logger.Error(error, summary);

            // One window at a time. Some errors repeat for as long as a window is on screen.
            if (showing)
            {
                return;
            }
            showing = true;
            try
            {
                string details = Describe(summary, error, Version(), ActiveGamePackage(), WindowsBuild());
                using (ErrorForm form = new ErrorForm(summary, error.Message, details))
                {
                    form.ShowDialog(owner);
                }
            }
            finally
            {
                showing = false;
            }
        }

        /// <summary>
        /// Puts together the text a user copies into an issue.
        /// </summary>
        internal static string Describe(string summary, Exception error, string version, string gamePackage, string windowsBuild)
        {
            StringBuilder details = new StringBuilder();
            details.AppendLine("Xbox Save File Converter " + version);
            details.AppendLine("Windows build: " + (windowsBuild ?? "unknown"));
            details.AppendLine("Game: " + (gamePackage ?? "none selected"));
            details.AppendLine();
            details.AppendLine(summary);
            details.Append(error);
            return details.ToString();
        }

        private static void ShowFromAnyThread(Exception error)
        {
            if (error == null)
            {
                return;
            }

            // A window and the clipboard need a thread of their own kind, and this may be any thread.
            Thread thread = new Thread(() => Show(null, UnexpectedProblem, error));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// The release this is, as the tag names it: the file version, not the assembly version, which
        /// stays the same from release to release.
        /// </summary>
        private static string Version()
        {
            object[] attributes = typeof(ErrorReport).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false);
            return attributes.Length == 0 ? "unknown" : ((System.Reflection.AssemblyFileVersionAttribute)attributes[0]).Version;
        }

        private static string ActiveGamePackage()
        {
            try
            {
                SaveFileConverterForm mainWindow = Application.OpenForms.OfType<SaveFileConverterForm>().FirstOrDefault();
                return mainWindow == null || mainWindow.ActiveGame == null ? null : mainWindow.ActiveGame.PackageName;
            }
            catch (InvalidOperationException)
            {
                // The list of windows changed while it was being read from another thread.
                return null;
            }
        }

        private static string WindowsBuild()
        {
            try
            {
                // Not Environment.OSVersion: without a manifest it reports Windows 8 on anything newer.
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild", null) as string;
            }
            catch (Exception e) when (e is System.Security.SecurityException || e is System.IO.IOException || e is UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
