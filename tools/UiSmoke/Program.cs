using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GPSaveConverter;
using GPSaveConverter.Interfaces;
using GPSaveConverter.Library;
using GPSaveConverter.SaveBackups;
using GPSaveConverter.Tests;
using GPSaveConverter.Xbox;

namespace GPSaveConverter.UiSmoke
{
    internal class MemorySettings : ISettingsProvider
    {
        public bool ShowFileTranslations { get; set; }
        public bool FirstRun { get; set; }
        public bool AllowWebDataFetch { get; set; }
        public string DefaultGameLibrary { get; set; } = string.Empty;
        public string UserGameLibrary { get; set; } = string.Empty;
        public NLog.LogLevel FileLogLevel { get; set; } = NLog.LogLevel.Debug;
        public bool BackupBeforeTransfer { get; set; } = true;
        public int BackupsToKeep { get; set; } = 10;
        public bool UsePreviewTranslations { get; set; }
        public string PreviewGameLibrary { get; set; } = string.Empty;
        public void Save() { }
        public void Reset() { }
    }

    internal class FakeEnvironment : IEnvironment
    {
        private readonly string localAppData;
        public FakeEnvironment(string localAppData) { this.localAppData = localAppData; }
        public string ExpandEnvironmentVariables(string name) { return Environment.ExpandEnvironmentVariables(name); }
        public string GetFolderPath(Environment.SpecialFolder folder)
        {
            return folder == Environment.SpecialFolder.LocalApplicationData ? localAppData : Environment.GetFolderPath(folder);
        }
    }

    internal class NoScripts : IScriptRunner
    {
        public string RunScript(string scriptText) { return string.Empty; }
    }

    /// <summary>
    /// Stands in for the network. It serves the translations that are being tested and, once the
    /// script sets it, GitHub's answer about the newest release. Any other address is a mistake in
    /// the test: nothing here may reach the real network.
    /// </summary>
    internal class StandInNetwork : IHttpClient
    {
        public string Library = string.Empty;

        /// <summary>GitHub's answer about the newest release. Null until a step asks for it.</summary>
        public string LatestRelease = null;

        public string DownloadString(string address)
        {
            if (address == GameLibrary.PreviewLibraryURL)
            {
                return Library;
            }
            if (address == ReleaseCheck.LatestReleaseAddress && LatestRelease != null)
            {
                return LatestRelease;
            }
            throw new InvalidOperationException("The window test asked the network for " + address);
        }

        public Task<string> DownloadStringAsync(string address)
        {
            return Task.FromResult(DownloadString(address));
        }

        public Task<byte[]> DownloadDataAsync(string address)
        {
            throw new InvalidOperationException("The window test asked the network for " + address);
        }
    }

    internal static class Native
    {
        public delegate bool WindowCallback(IntPtr window, IntPtr parameter);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateDesktop(string name, IntPtr device, IntPtr devmode, int flags, uint access, IntPtr security);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseDesktop(IntPtr desktop);

        [DllImport("user32.dll")]
        public static extern IntPtr GetProcessWindowStation();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetUserObjectInformation(IntPtr userObject, int index, StringBuilder information, int length, out int needed);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(uint thread, WindowCallback callback, IntPtr parameter);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr parent, WindowCallback callback, IntPtr parameter);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr window, StringBuilder name, int max);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr window, StringBuilder text, int max);

        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(IntPtr window);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess, hThread;
            public int dwProcessId, dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcess(string application, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
            bool inheritHandles, uint creationFlags, IntPtr environment, string currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

        [DllImport("kernel32.dll")]
        public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

        [DllImport("kernel32.dll")]
        public static extern bool TerminateProcess(IntPtr process, uint exitCode);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);

        public static string ClassOf(IntPtr window)
        {
            StringBuilder name = new StringBuilder(256);
            GetClassName(window, name, name.Capacity);
            return name.ToString();
        }

        public static string TextOf(IntPtr window)
        {
            StringBuilder text = new StringBuilder(4096);
            GetWindowText(window, text, text.Capacity);
            return text.ToString();
        }

        /// <summary>The window station this process belongs to. "WinSta0" for someone at the keyboard.</summary>
        public static string WindowStationName()
        {
            const int UOI_NAME = 2;
            StringBuilder name = new StringBuilder(256);
            int needed;
            return GetUserObjectInformation(GetProcessWindowStation(), UOI_NAME, name, name.Capacity * 2, out needed) ? name.ToString() : "WinSta0";
        }
    }

    /// <summary>
    /// The buttons of a message box, by the number Windows gives each. The numbers are the same in
    /// every language, which the words on the buttons are not.
    /// </summary>
    internal enum Answer
    {
        OK = 1,
        Cancel = 2,
        Abort = 3,
        Retry = 4,
        Ignore = 5,
        Yes = 6,
        No = 7
    }

    /// <summary>
    /// Answers every message box the application shows, and keeps what each one said.
    /// </summary>
    internal class DialogResponder
    {
        private const uint WM_COMMAND = 0x0111;

        public readonly List<string> Transcript = new List<string>();

        /// <summary>The button to press for a message box with a given title. Any other gets Yes, or OK if there is no Yes.</summary>
        public readonly Dictionary<string, Answer> AnswerByTitle = new Dictionary<string, Answer>();

        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 40 };
        private readonly HashSet<IntPtr> answered = new HashSet<IntPtr>();
        private readonly uint thread = Native.GetCurrentThreadId();

        /// <summary>How many of the application's error windows the script is about to cause on purpose.</summary>
        public int ErrorWindowsExpected;

        /// <summary>How many error windows have come up, wanted or not.</summary>
        public int ErrorWindowsSeen;

        /// <summary>Error windows nobody asked for. Each one is a failed run.</summary>
        public int ErrorWindowsUnexpected;

        private readonly HashSet<ErrorForm> errorWindows = new HashSet<ErrorForm>();

        public DialogResponder()
        {
            timer.Tick += (sender, e) =>
            {
                Native.EnumThreadWindows(thread, Look, IntPtr.Zero);
                LookForErrorWindows();
            };
            timer.Start();
        }

        /// <summary>
        /// The application's own error window is a form, not a message box. It is read and closed here.
        /// Its Copy button is left alone: the clipboard is shared with the desktop the user is on.
        /// </summary>
        private void LookForErrorWindows()
        {
            foreach (ErrorForm window in Application.OpenForms.OfType<ErrorForm>().ToList())
            {
                if (!window.Visible || !errorWindows.Add(window))
                {
                    continue;
                }

                bool expected = ErrorWindowsExpected > 0;
                if (expected) ErrorWindowsExpected--; else ErrorWindowsUnexpected++;
                ErrorWindowsSeen++;

                Func<string, string> text = name => ((Control)typeof(ErrorForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window)).Text;
                Transcript.Add("--- [" + window.Text + "]  the application's error window" + (expected ? string.Empty : "  NOT EXPECTED") + Environment.NewLine
                    + text("summaryLabel") + Environment.NewLine + text("messageLabel") + Environment.NewLine + text("hintLabel") + Environment.NewLine
                    + "    | " + text("detailsTextBox").Replace(Environment.NewLine, Environment.NewLine + "    | "));
                window.BeginInvoke((Action)window.Close);
            }
        }

        public void Stop() { timer.Stop(); }

        /// <summary>True while a message box is still on screen.</summary>
        public bool AnyOpen()
        {
            bool open = false;
            Native.EnumThreadWindows(thread, (window, p) =>
            {
                if (Native.IsWindowVisible(window) && Native.ClassOf(window) == "#32770") open = true;
                return true;
            }, IntPtr.Zero);
            return open;
        }

        private bool Look(IntPtr window, IntPtr parameter)
        {
            if (Native.IsWindowVisible(window) && Native.ClassOf(window) == "#32770" && answered.Add(window))
            {
                string title = Native.TextOf(window);
                List<string> texts = new List<string>();
                Dictionary<Answer, IntPtr> buttons = new Dictionary<Answer, IntPtr>();
                Dictionary<Answer, string> captions = new Dictionary<Answer, string>();
                Native.EnumChildWindows(window, (child, p) =>
                {
                    string childClass = Native.ClassOf(child);
                    string text = Native.TextOf(child);
                    if (childClass == "Button")
                    {
                        Answer answer = (Answer)Native.GetDlgCtrlID(child);
                        buttons[answer] = child;
                        captions[answer] = text.Replace("&", string.Empty);
                    }
                    else if (childClass == "Static" && text.Length > 0)
                    {
                        texts.Add(text);
                    }
                    return true;
                }, IntPtr.Zero);

                Answer wanted;
                if (!AnswerByTitle.TryGetValue(title, out wanted))
                {
                    // A box with one button gives it the number of Cancel, whatever it says, so that Esc works.
                    wanted = buttons.ContainsKey(Answer.Yes) ? Answer.Yes : buttons.ContainsKey(Answer.OK) ? Answer.OK : buttons.Keys.First();
                }
                if (!buttons.ContainsKey(wanted))
                {
                    throw new InvalidOperationException("The message box \"" + title + "\" has no " + wanted + " button.");
                }

                Transcript.Add("--- [" + title + "]  buttons: " + string.Join(" / ", captions.Values) + "  -> pressed " + captions[wanted] + Environment.NewLine + string.Join(Environment.NewLine, texts));
                Native.PostMessage(window, WM_COMMAND, (IntPtr)(int)wanted, buttons[wanted]);
            }
            return true;
        }
    }

    internal static class Program
    {
        private static readonly StringBuilder report = new StringBuilder();
        private static int failures;

        private static void Say(string line)
        {
            report.AppendLine(line);
        }

        private static void Check(bool condition, string what)
        {
            Say((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) failures++;
        }

        private static void CheckEqual<T>(T expected, T actual, string what)
        {
            bool equal = EqualityComparer<T>.Default.Equals(expected, actual);
            Say((equal ? "  ok    " : "  FAIL  ") + what + (equal ? string.Empty : "   expected <" + expected + "> but was <" + actual + ">"));
            if (!equal) failures++;
        }

        private static void CheckSame(SortedDictionary<string, string> expected, SortedDictionary<string, string> actual, string what)
        {
            bool equal = expected.Count == actual.Count && expected.All(e => actual.ContainsKey(e.Key) && actual[e.Key] == e.Value);
            Say((equal ? "  ok    " : "  FAIL  ") + what);
            if (!equal)
            {
                failures++;
                Say("          expected: " + string.Join(", ", expected.Keys));
                Say("          actual:   " + string.Join(", ", actual.Keys));
            }
        }

        private static int Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--child")
            {
                int code = RunOnStaThread();
                File.WriteAllText(args[1], report.ToString());
                return code;
            }
            if (args.Length >= 2 && args[0] == "--wiki")
            {
                return Wiki(args[1]);
            }
            if (args.Contains("--speed"))
            {
                return Speed();
            }
            if (!args.Contains("--visible"))
            {
                int? hiddenRun = RunOnHiddenDesktop();
                if (hiddenRun != null)
                {
                    return hiddenRun.Value;
                }
                Console.WriteLine("Running on the current desktop instead.");
            }

            int visibleRun = RunOnStaThread();
            Console.Write(report.ToString());
            return visibleRun;
        }

        /// <summary>
        /// Starts this program again on a desktop of its own, where its windows are never shown and
        /// cannot take the keyboard from whoever is using the machine.
        /// </summary>
        /// <returns>The exit code of that run, or null if a desktop could not be set up for it.</returns>
        private static int? RunOnHiddenDesktop()
        {
            const uint GENERIC_ALL = 0x10000000;
            string desktopName = "gpsc-ui-smoke-" + Process.GetCurrentProcess().Id;
            IntPtr desktop = Native.CreateDesktop(desktopName, IntPtr.Zero, IntPtr.Zero, 0, GENERIC_ALL, IntPtr.Zero);
            if (desktop == IntPtr.Zero)
            {
                Console.WriteLine("Could not create a desktop: error " + Marshal.GetLastWin32Error() + ".");
                return null;
            }

            string reportFile = Path.Combine(Path.GetTempPath(), "gpsc-ui-smoke-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                Native.STARTUPINFO startup = new Native.STARTUPINFO();
                startup.cb = Marshal.SizeOf(typeof(Native.STARTUPINFO));
                startup.lpDesktop = Native.WindowStationName() + "\\" + desktopName;

                string exe = Assembly.GetExecutingAssembly().Location;
                StringBuilder commandLine = new StringBuilder("\"" + exe + "\" --child \"" + reportFile + "\"");
                Native.PROCESS_INFORMATION process;
                if (!Native.CreateProcess(exe, commandLine, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref startup, out process))
                {
                    Console.WriteLine("Could not start on the desktop " + startup.lpDesktop + ": error " + Marshal.GetLastWin32Error() + ".");
                    return null;
                }

                uint exitCode = 3;
                if (Native.WaitForSingleObject(process.hProcess, 120000) != 0)
                {
                    Console.WriteLine("The run did not finish in two minutes and was stopped.");
                    Native.TerminateProcess(process.hProcess, 3);
                }
                else
                {
                    Native.GetExitCodeProcess(process.hProcess, out exitCode);
                }
                Native.CloseHandle(process.hThread);
                Native.CloseHandle(process.hProcess);

                Console.Write(File.Exists(reportFile) ? File.ReadAllText(reportFile) : "No report was written. The run ended with code " + exitCode + "." + Environment.NewLine);
                if (exitCode == NoWindows)
                {
                    // A desktop of its own was not somewhere windows could be opened. The one in use may be.
                    return null;
                }
                return (int)exitCode;
            }
            finally
            {
                if (File.Exists(reportFile)) File.Delete(reportFile);
                Native.CloseDesktop(desktop);
            }
        }

        /// <summary>Runs the wiki table parser over saved sections of real pages.</summary>
        private static int Wiki(string folder)
        {
            int failed = 0;
            foreach (string file in Directory.GetFiles(folder, "*.txt").OrderBy(f => f))
            {
                string text = File.ReadAllText(file);
                int firstLineEnd = text.IndexOf((char)10);
                string title = text.Substring(7, firstLineEnd - 7);
                string wikitext = text.Substring(firstLineEnd + 1);
                try
                {
                    bool usesProfile;
                    string location = PCGameWiki.chooseSaveLocation(wikitext, out usesProfile);
                    Console.WriteLine(title + ": " + (location ?? "(nothing)") + (usesProfile ? "   [profile to pick]" : string.Empty));
                }
                catch (Exception e)
                {
                    failed++;
                    Console.WriteLine(title + ": THROWS " + e.GetType().Name + ": " + e.Message.Replace(Environment.NewLine, " "));
                }
            }
            Console.WriteLine(failed + " page(s) made the parser throw");
            return failed == 0 ? 0 : 1;
        }

        /// <summary>How long backups and restores take when there are many files, or large ones.</summary>
        private static int Speed()
        {
            string root = Path.Combine(Path.GetTempPath(), "gpsc", "speed" + Guid.NewGuid().ToString("N").Substring(0, 4));
            try
            {
                string saves = Path.Combine(root, "N") + "\\";
                string profile = Path.Combine(root, "Packages", "Test.Game_abc123", "SystemAppData", "wgs", "0009000000000001_0001");
                SaveBackupStore store = new SaveBackupStore(Path.Combine(root, "B"));
                byte[] small = new byte[2048];
                byte[] large = new byte[4 * 1024 * 1024];
                new Random(1).NextBytes(large);

                const int manyFiles = 5000;
                for (int i = 0; i < manyFiles; i++)
                {
                    string file = Path.Combine(saves, "SLOT_" + (i % 50), "file" + i + ".sav");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    if (i % 2 == 0) File.WriteAllBytes(file, small);
                }
                Stopwatch watch = Stopwatch.StartNew();
                SaveBackup journal = store.StartNonXboxBackup("Test.Game_abc123", "Test Game", saves, "copying " + manyFiles + " files from Xbox");
                for (int i = 0; i < manyFiles; i++)
                {
                    string file = Path.Combine(saves, "SLOT_" + (i % 50), "file" + i + ".sav");
                    journal.Preserve(file);
                    File.WriteAllBytes(file, small);
                }
                journal.Complete();
                Console.WriteLine("non-Xbox: kept " + manyFiles + " files (half of them new) in " + watch.ElapsedMilliseconds + " ms, writing included");
                watch.Restart();
                SaveBackup listed = store.List("Test.Game_abc123").Single();
                Console.WriteLine("non-Xbox: read the backup back in " + watch.ElapsedMilliseconds + " ms (" + listed.Files.Count + " files)");
                watch.Restart();
                store.Restore(listed);
                Console.WriteLine("non-Xbox: restored in " + watch.ElapsedMilliseconds + " ms, the backup of what was replaced included; files left: " + Directory.GetFiles(saves, "*", SearchOption.AllDirectories).Length);

                const int largeFiles = 120;
                for (int i = 0; i < largeFiles; i++)
                {
                    string file = Path.Combine(profile, "C" + (i % 12).ToString("D31"), "B" + i.ToString("D31"));
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllBytes(file, large);
                }
                File.WriteAllBytes(Path.Combine(profile, "containers.index"), small);
                watch.Restart();
                SaveBackup snapshot = store.BackUpXboxSave("Test.Game_abc123", "Test Game", profile, "copying 1 file to Xbox");
                Console.WriteLine("Xbox: backed up " + snapshot.Files.Count + " files, " + SaveBackup.FormatSize(snapshot.Size) + ", in " + watch.ElapsedMilliseconds + " ms");
                watch.Restart();
                store.Restore(snapshot);
                Console.WriteLine("Xbox: restored in " + watch.ElapsedMilliseconds + " ms, the backup of what was replaced included");
                return 0;
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        /// <summary>The exit code for "no window could be opened here", as against a check that failed.</summary>
        private const int NoWindows = 4;

        private static int RunOnStaThread()
        {
            int code = 1;
            Thread ui = new Thread(() =>
            {
                try
                {
                    code = RunWindows();
                }
                catch (Exception e)
                {
                    Say("FAILED before the windows were up: " + e);
                    code = NoWindows;
                }
            });
            ui.SetApartmentState(ApartmentState.STA);
            ui.Start();
            ui.Join();
            return code;
        }

        private const string LibraryJson = "{ \"Version\": \"2026-01-01\", \"GameInfo\": [ { \"Name\": \"Test Game\", \"PackageName\": \"" + FakeXboxSave.PackageName + "\", \"BaseNonXboxSaveLocation\": \"{SAVE}\", "
            + "\"FileTranslations\": [ { \"NamedRegexGroups\": [ \"(?<FileSlot>[0-9]+)\" ], \"ContainerName1\": \"SaveGame\", \"ContainerName2\": \"\", \"XboxFileID\": \"SaveSlot${FileSlot}\", \"NonXboxFilename\": \"saveFile${FileSlot}.sav\" } ] } ] }";

        private static int RunWindows()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // An error inside one of the application's own handlers would otherwise put up a window and wait.
            ErrorReport.CatchUnhandledErrors();

            using (FakeXboxSave save = new FakeXboxSave())
            {
                // In order of name, as the Xbox app keeps the containers.
                save.WithContainerNotOnDisk("NotDownloaded", "NotDownloaded")
                    .WithXboxFile("SaveGame", "", "SaveSlot0", "xbox slot 0")
                    .WithXboxFile("SaveGame", "", "SaveSlot3", "xbox slot 3")
                    .WithNonXboxFile("saveFile0.sav", "steam slot 0")
                    .WithNonXboxFile("saveFile7.sav", "steam slot 7")
                    .Build();

                MemorySettings settings = new MemorySettings();
                settings.UserGameLibrary = LibraryJson.Replace("{SAVE}", save.NonXboxFolder.Replace("\\", "\\\\"));
                settings.DefaultGameLibrary = settings.UserGameLibrary;

                // Nothing may reach the real settings, the network, PowerShell or the real save folders.
                SaveFileConverterForm.Settings = settings;
                PreferencesForm.Settings = settings;
                BackupsForm.Settings = settings;
                GameLibrary.Settings = settings;
                GameLibrary.ScriptRunner = new NoScripts();
                GameLibrary.HttpClient = new StandInNetwork();
                XboxPackageList.Environment = new FakeEnvironment(save.LocalAppData);
                SaveBackupStore store = new SaveBackupStore(save.BackupFolder);
                SaveFileConverterForm.Backups = store;

                SaveFileConverterForm form = new SaveFileConverterForm();
                DialogResponder responder = new DialogResponder();
                form.Shown += async (sender, e) =>
                {
                    try
                    {
                        await Script(form, save, settings, store, responder);
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        Say("FAILED: " + ex);
                    }
                    finally
                    {
                        responder.Stop();
                        form.Close();
                    }
                };
                Application.Run(form);
                if (responder.ErrorWindowsUnexpected > 0)
                {
                    failures += responder.ErrorWindowsUnexpected;
                    Say("FAILED: " + responder.ErrorWindowsUnexpected + " error window(s) came up that the script did not cause on purpose.");
                }

                Say(string.Empty);
                Say("=== What the message boxes said ===");
                foreach (string dialog in responder.Transcript)
                {
                    Say(dialog);
                }
                Say(string.Empty);
                Say(failures == 0 ? "RESULT: all checks passed" : "RESULT: " + failures + " check(s) FAILED");
                return failures == 0 ? 0 : 1;
            }
        }

        private static T Field<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().Name, name);
            return (T)field.GetValue(target);
        }

        private static async Task WaitUntil(Func<bool> condition, string what, int timeoutMilliseconds = 20000)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline) throw new TimeoutException("Timed out waiting for: " + what);
                await Task.Delay(40);
            }
        }

        private static async Task Script(SaveFileConverterForm form, FakeXboxSave save, MemorySettings settings, SaveBackupStore store, DialogResponder responder)
        {
            DataGridView packages = Field<DataGridView>(form, "packagesDataGridView");
            DataGridView xboxFiles = Field<DataGridView>(form, "xboxFilesTable");
            DataGridView nonXboxFiles = Field<DataGridView>(form, "nonXboxFilesTable");
            ToolStripStatusLabel status = Field<ToolStripStatusLabel>(form, "infoStatusLabel");
            ToolStripMenuItem backupsMenu = Field<ToolStripMenuItem>(form, "backupsToolStripMenuItem");
            Button allFromXbox = Field<Button>(form, "moveAllFromXboxButton");
            Button allToXbox = Field<Button>(form, "moveAllToXboxButton");
            Button selectionFromXbox = Field<Button>(form, "moveSelectionFromXboxButton");
            string package = FakeXboxSave.PackageName;

            // The application's own start-up has to finish first: it fills the package list.
            await WaitUntil(() => packages.DataSource != null, "the package list");
            CheckEqual(package, string.Join(", ", packages.Rows.Cast<DataGridViewRow>().Select(r => ((GameInfo)r.DataBoundItem).PackageName)), "the package list holds the made-up game, and nothing from this PC");
            Check(!backupsMenu.Enabled, "File > Backups is off until a game is selected");

            Say("== Select the game");
            // An installed game gets its name from its package manifest. This one is not installed.
            GameInfo game = GameLibrary.getGameInfo(package);
            game.Name = "Test Game";
            Select(form, packages, game);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 2, "both file lists");
            Check(backupsMenu.Enabled, "File > Backups is on once a game is selected");
            CheckEqual("1 of the 2 Xbox containers are listed but not on this PC, so their files are not shown.", status.Text, "the status line says a container is not on this PC, and the rest of the save is listed all the same");
            string notDownloaded = save.ReadIndexEntry("NotDownloaded", "NotDownloaded");

            SortedDictionary<string, string> xboxAtStart = FolderContents.Read(save.ProfileFolder);
            SortedDictionary<string, string> filesAtStart = FolderContents.Read(save.NonXboxFolder);

            Say("== Copy everything from Xbox");
            status.Text = string.Empty;
            allFromXbox.PerformClick();
            await WaitUntil(() => status.Text.StartsWith("Transfer complete"), "the transfer from Xbox");
            await WaitUntil(() => nonXboxFiles.RowCount == 3, "the non-Xbox list to show the new file");
            CheckEqual("Transfer complete. To undo it, choose File > Backups.", status.Text, "the status line says how to undo");
            CheckEqual("xbox slot 0", save.ReadNonXboxFile("saveFile0.sav"), "saveFile0.sav was replaced");
            CheckEqual("xbox slot 3", save.ReadNonXboxFile("saveFile3.sav"), "saveFile3.sav was created");
            List<SaveBackup> backups = store.List(package);
            CheckEqual(1, backups.Count, "one backup exists");
            CheckEqual(BackupSide.NonXbox, backups[0].Side, "it is a backup of non-Xbox files");
            CheckEqual("copying 2 files from Xbox", backups[0].MadeBefore, "it says what it was made before");
            Check(!File.Exists(Path.Combine(backups[0].Folder, SaveBackup.JournalName)), "its journal was folded into backup.json");
            Check(form.Enabled, "the window is usable again");

            Say("== Copy to Xbox when the backup cannot be made, and answer No");
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "saveFile0.sav"), "edited on steam");
            SortedDictionary<string, string> xboxBeforeRefusal = FolderContents.Read(save.ProfileFolder);
            Func<string, long?> realFreeSpace = store.FreeSpace;
            store.FreeSpace = folder => 0;
            responder.AnswerByTitle["Backup failed"] = Answer.No;
            status.Text = string.Empty;
            allToXbox.PerformClick();
            await WaitUntil(() => status.Text == "Transfer canceled", "the transfer to be called off");
            CheckSame(xboxBeforeRefusal, FolderContents.Read(save.ProfileFolder), "the Xbox save was not touched");
            CheckEqual(1, store.List(package).Count, "no backup was added");
            Check(form.Enabled, "the window is usable again");
            store.FreeSpace = realFreeSpace;
            responder.AnswerByTitle.Remove("Backup failed");

            Say("== Copy everything to Xbox");
            status.Text = string.Empty;
            allToXbox.PerformClick();
            await WaitUntil(() => status.Text.StartsWith("Transfer complete"), "the transfer to Xbox");
            await WaitUntil(() => xboxFiles.RowCount == 3, "the Xbox list to show the new blob");
            CheckEqual("Transfer complete. To undo it, choose File > Backups.", status.Text, "the status line says how to undo");
            List<KeyValuePair<string, string>> blobs = save.ReadContainer("SaveGame", "");
            CheckEqual("SaveSlot0=edited on steam, SaveSlot3=xbox slot 3, SaveSlot7=steam slot 7", string.Join(", ", blobs.Select(b => b.Key + "=" + b.Value)), "the container holds the Steam files");
            CheckEqual(notDownloaded, save.ReadIndexEntry("NotDownloaded", "NotDownloaded"), "what the index says of the container that is not on this PC was left as it was");
            backups = store.List(package);
            CheckEqual(2, backups.Count, "two backups exist");
            CheckEqual(BackupSide.Xbox, backups[0].Side, "the newest is a backup of the Xbox save");
            CheckEqual("copying 3 files to Xbox", backups[0].MadeBefore, "it says what it was made before");
            CheckSame(xboxBeforeRefusal, FolderContents.Read(backups[0].FilesFolder), "it holds the Xbox save as it was before the transfer");
            Check(form.Enabled, "the window is usable again");

            Say("== File > Backups: restore the Xbox save, then the non-Xbox files");
            Task dialogWork = DriveBackupsDialog(save, store, responder);
            backupsMenu.PerformClick();
            await dialogWork;
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 2, "both file lists to be read again after the restore");
            CheckSame(xboxAtStart, FolderContents.Read(save.ProfileFolder), "the Xbox save is exactly as it was at the start");
            CheckEqual("steam slot 0", save.ReadNonXboxFile("saveFile0.sav"), "saveFile0.sav has its Steam content back");
            CheckEqual(null, save.ReadNonXboxFile("saveFile3.sav"), "saveFile3.sav, which the transfer added, is gone");
            CheckSame(filesAtStart, FolderContents.Read(save.NonXboxFolder), "the non-Xbox folder is exactly as it was at the start");
            CheckEqual(4, store.List(package).Count, "each restore left a backup of what it replaced");
            CheckEqual("SaveSlot0, SaveSlot3", string.Join(", ", xboxFiles.Rows.Cast<DataGridViewRow>().Select(r => ((XboxFileInfo)r.DataBoundItem).FileID)), "the Xbox list shows the restored save");
            CheckEqual("saveFile0.sav, saveFile7.sav", string.Join(", ", nonXboxFiles.Rows.Cast<DataGridViewRow>().Select(r => ((NonXboxFileInfo)r.DataBoundItem).RelativePath)), "the non-Xbox list shows the restored files");

            Say("== Backups turned off in the preferences: one selected file from Xbox");
            settings.BackupBeforeTransfer = false;
            foreach (DataGridViewRow row in xboxFiles.Rows)
            {
                row.Selected = ((XboxFileInfo)row.DataBoundItem).FileID == "SaveSlot3";
            }
            status.Text = string.Empty;
            selectionFromXbox.PerformClick();
            await WaitUntil(() => status.Text.StartsWith("Transfer complete"), "the transfer without a backup");
            CheckEqual("Transfer complete", status.Text, "the status line does not promise an undo");
            CheckEqual("xbox slot 3", save.ReadNonXboxFile("saveFile3.sav"), "saveFile3.sav was created");
            CheckEqual("steam slot 0", save.ReadNonXboxFile("saveFile0.sav"), "the file that was not selected was left alone");
            CheckEqual(4, store.List(package).Count, "no backup was added");
            settings.BackupBeforeTransfer = true;

            Say("== A save location with a place for a profile, before any profile has a folder");
            TabControl profileTabs = Field<TabControl>(form, "tabControl1");
            string pickedByHand = form.ActiveGame.BaseNonXboxSaveLocation;
            // What a game's library entry sets up. The Xbox kind of profile is looked up in no registry and no Steam files.
            form.ActiveGame.TargetProfileTypes = new[] { NonXboxProfile.ProfileType.Xbox };
            form.ActiveGame.TargetProfiles = new[] { new NonXboxProfile(0, NonXboxProfile.ProfileType.Xbox) };
            form.ActiveGame.BaseNonXboxSaveLocation = save.NonXboxProfilesFolder + "<user-id>\\";
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => ProfileList(profileTabs) != null && ProfileList(profileTabs).RowCount == 1 && xboxFiles.RowCount == 2, "the profile list");
            CheckEqual("No non-Xbox profiles found", ((NonXboxProfile)ProfileList(profileTabs).Rows[0].DataBoundItem).UserName, "the profile list says that it found none");
            Check(await IsRefusedForProfile(allFromXbox, status, responder), "a transfer is refused");

            Say("== Two profiles get a folder: one is picked, and the files go into it");
            save.AddNonXboxProfile("1111");
            string secondProfile = save.AddNonXboxProfile("2222");
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => ProfileList(profileTabs).RowCount == 2 && xboxFiles.RowCount == 2, "both profiles in the list");
            Check(ProfileList(profileTabs).Enabled, "the list can be clicked, although it was turned off while it had nothing to show");
            Check(await IsRefusedForProfile(allFromXbox, status, responder), "a transfer is refused until one of them is picked");
            ClickProfile(form, ProfileList(profileTabs), 1);
            await WaitUntil(() => form.ActiveGame.TargetProfiles[0].UserID == "2222" && nonXboxFiles.RowCount == 0, "the second profile to be picked");
            Check(!await IsRefusedForProfile(allFromXbox, status, responder), "with the profile picked, the transfer goes ahead");
            CheckEqual("xbox slot 0, xbox slot 3", string.Join(", ", new[] { "saveFile0.sav", "saveFile3.sav" }.Select(f => File.Exists(secondProfile + f) ? File.ReadAllText(secondProfile + f) : null)), "the files are in the folder of the profile that was picked");
            CheckEqual(0, Directory.GetFileSystemEntries(save.NonXboxProfilesFolder + "1111").Length, "the other profile's folder was left alone");

            Say("== A folder picked by hand in place of the profile");
            await form.useNonXboxSaveLocation(pickedByHand.TrimEnd('\\'));
            CheckEqual(pickedByHand, form.ActiveGame.BaseNonXboxSaveLocation, "the folder is the save location");
            CheckEqual(0, profileTabs.TabPages.Count, "the profile list is taken away, as the folder has no place for a profile");
            CheckEqual("saveFile0.sav, saveFile3.sav, saveFile7.sav", string.Join(", ", nonXboxFiles.Rows.Cast<DataGridViewRow>().Select(r => ((NonXboxFileInfo)r.DataBoundItem).RelativePath)), "the non-Xbox list shows the files in it");

            Say("== The tool closed and started again, with that folder remembered (issue #133)");
            // Closing stores the library. The next start reads it back, and selecting the game applies its entry.
            settings.UserGameLibrary = GameLibrary.GetLibraryJson();
            await GameLibrary.Initialize();
            GameInfo startedAgain = GameLibrary.getGameInfo(package);
            startedAgain.Name = "Test Game";
            Select(form, packages, startedAgain);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "both file lists after the start");
            CheckEqual(pickedByHand, form.ActiveGame.BaseNonXboxSaveLocation, "the folder picked by hand is still the save location");
            Check(form.ActiveGame.TargetProfiles != null && form.ActiveGame.TargetProfiles.Length == 1 && form.ActiveGame.TargetProfiles[0].UserID == null, "the game's entry names a profile again, and none is picked");
            CheckEqual(0, profileTabs.TabPages.Count, "there is no profile list to pick one from");
            Check(!await IsRefusedForProfile(allFromXbox, status, responder), "the transfer goes ahead all the same: the folder has no place for a profile");
            CheckEqual("xbox slot 0", save.ReadNonXboxFile("saveFile0.sav"), "the files are in the folder picked by hand");
            form.ActiveGame.TargetProfileTypes = null;
            form.ActiveGame.TargetProfiles = null;

            Say("== A file the Xbox save has no container for: asked about first, and made only on a yes");
            Button selectionToXbox = Field<Button>(form, "moveSelectionToXboxButton");
            FileTranslation settingsTranslation = new FileTranslation { ContainerName1 = "Profile", ContainerName2 = "Profile", XboxFileID = "settings", NonXboxFilename = "settings\\.ini", NamedRegexGroups = new string[0] };
            form.ActiveGame.FileTranslations.Add(settingsTranslation);
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "settings.ini"), "steam settings");
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 4, "the lists with the new file in them");
            foreach (DataGridViewRow row in nonXboxFiles.Rows)
            {
                row.Selected = ((NonXboxFileInfo)row.DataBoundItem).RelativePath == "settings.ini";
            }
            SortedDictionary<string, string> withoutTheContainer = FolderContents.Read(save.ProfileFolder);
            int backupsBefore = store.List(package).Count;

            responder.AnswerByTitle["Create Xbox containers?"] = Answer.No;
            int boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            selectionToXbox.PerformClick();
            await WaitUntil(() => status.Text.Contains("left out") && !responder.AnyOpen(), "the file to be left out");
            string question = responder.Transcript.Skip(boxesBefore).FirstOrDefault(t => t.Contains("[Create Xbox containers?]")) ?? string.Empty;
            Check(question.Contains("    Profile") && question.Contains("no container for 1 file"), "the question names the container and says how many files need it");
            Check(!question.Contains("just one of the files"), "with one container to make, it does not say to try one first");
            CheckSame(withoutTheContainer, FolderContents.Read(save.ProfileFolder), "answered No: the Xbox save is untouched");
            CheckEqual(backupsBefore, store.List(package).Count, "and no backup was made, as nothing was going to be copied");

            responder.AnswerByTitle["Create Xbox containers?"] = Answer.Yes;
            status.Text = string.Empty;
            selectionToXbox.PerformClick();
            await WaitUntil(() => status.Text.StartsWith("Transfer complete"), "the transfer that makes the container");
            await WaitUntil(() => xboxFiles.RowCount == 3, "the Xbox list to show the new blob");
            responder.AnswerByTitle.Remove("Create Xbox containers?");
            List<FakeXboxSave.IndexEntry> entries = save.ReadIndexEntries();
            CheckEqual("NotDownloaded, Profile, SaveGame", string.Join(", ", entries.Select(e => e.Name1)), "answered Yes: the index lists the new container, at its place in the order of names");
            FakeXboxSave.IndexEntry made = entries.Single(e => e.Name1 == "Profile");
            CheckEqual("state 5, mark '', container.1", "state " + made.SyncState + ", mark '" + made.CloudTag + "', container." + made.Number, "it is marked as made here and never uploaded");
            CheckEqual("settings=steam settings", string.Join(", ", save.ReadContainerFile(made).Select(b => b.FileID + "=" + b.Content)), "it holds the file");
            CheckEqual(2u, save.ReadIndexSyncState(), "the save as a whole is marked as having something to upload");
            Check(xboxFiles.Rows.Cast<DataGridViewRow>().Any(r => ((XboxFileInfo)r.DataBoundItem).ContainerName1 == "Profile"), "the Xbox list shows the new container's file");
            SaveBackup beforeTheContainer = store.List(package)[0];
            CheckSame(withoutTheContainer, FolderContents.Read(beforeTheContainer.FilesFolder), "the backup made first holds the save without the container");

            store.Restore(beforeTheContainer);
            form.ActiveGame.FileTranslations.Remove(settingsTranslation);
            File.Delete(Path.Combine(save.NonXboxFolder, "settings.ini"));
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "both lists after the undo");
            CheckSame(withoutTheContainer, FolderContents.Read(save.ProfileFolder), "restoring that backup takes the container out again");

            Say("== Several containers to make at once: the question says to try one first");
            FileTranslation extraTranslation = new FileTranslation { ContainerName1 = "Extra${Slot}", ContainerName2 = "Extra${Slot}", XboxFileID = "data", NonXboxFilename = "extra${Slot}\\.dat", NamedRegexGroups = new string[] { "(?<Slot>[0-9]+)" } };
            form.ActiveGame.FileTranslations.Add(extraTranslation);
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "extra1.dat"), "steam extra 1");
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "extra2.dat"), "steam extra 2");
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 5, "the lists with the two new files in them");
            foreach (DataGridViewRow row in nonXboxFiles.Rows)
            {
                row.Selected = ((NonXboxFileInfo)row.DataBoundItem).RelativePath.StartsWith("extra");
            }

            responder.AnswerByTitle["Create Xbox containers?"] = Answer.Cancel;
            boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            selectionToXbox.PerformClick();
            await WaitUntil(() => status.Text == "Transfer canceled" && !responder.AnyOpen(), "the transfer to be called off");
            responder.AnswerByTitle.Remove("Create Xbox containers?");
            question = responder.Transcript.Skip(boxesBefore).FirstOrDefault(t => t.Contains("[Create Xbox containers?]")) ?? string.Empty;
            Check(question.Contains("    Extra1") && question.Contains("    Extra2") && question.Contains("these 2 containers have"), "the question names both containers");
            Check(question.Contains("answer No. Then copy just one of the files"), "and says to try one file first");
            CheckSame(withoutTheContainer, FolderContents.Read(save.ProfileFolder), "answered Cancel: the Xbox save is untouched");

            form.ActiveGame.FileTranslations.Remove(extraTranslation);
            File.Delete(Path.Combine(save.NonXboxFolder, "extra1.dat"));
            File.Delete(Path.Combine(save.NonXboxFolder, "extra2.dat"));
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "both lists without the two files");

            Say("== A translation that is still being tested: a tick in Preferences brings it in, and unticking takes it out");
            Label translationsLabel = Field<Label>(form, "fileTranslationsMarkerLabel");
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "quick.dat"), "steam quick save");
            ((StandInNetwork)GameLibrary.HttpClient).Library ="{ \"Version\": \"2026-01-01\", \"GameInfo\": [ { \"PackageName\": \"" + package + "\", \"FileTranslations\": [ "
                + "{ \"NamedRegexGroups\": [], \"ContainerName1\": \"SaveGame\", \"ContainerName2\": \"\", \"XboxFileID\": \"SaveSlot0\", \"NonXboxFilename\": \"quick\\\\.dat\" } ] } ] }";
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 4, "the lists with quick.dat in them");
            CheckEqual(string.Empty, await MatchOnXbox(nonXboxFiles, xboxFiles, "quick.dat"), "before: nothing on the Xbox side matches quick.dat");

            await SavePreferences(form, true);
            CheckEqual(string.Empty, preferencesLayout, "nothing in the Preferences window sticks out of it or overlaps");
            await WaitUntil(() => form.ActiveGame.PreviewTranslations.Count == 1 && xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 4, "the translation being tested");
            Check(status.Text.Contains("still being tested"), "ticked: the status line says a translation being tested is in use: \"" + status.Text + "\"");
            CheckEqual("File Translations: (1 being tested is tried first)", translationsLabel.Text, "and so does the translations panel");
            CheckEqual("SaveSlot0", await MatchOnXbox(nonXboxFiles, xboxFiles, "quick.dat"), "quick.dat is matched to SaveSlot0, with no restart");
            CheckEqual(1, form.ActiveGame.FileTranslations.Count, "the game's own translations are as they were");
            Check(!GameLibrary.GetLibraryJson().Contains("quick"), "nothing of it is in the library that gets stored");

            await SavePreferences(form, false);
            await WaitUntil(() => form.ActiveGame.PreviewTranslations.Count == 0 && xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 4, "the translation being tested to go");
            CheckEqual(string.Empty, await MatchOnXbox(nonXboxFiles, xboxFiles, "quick.dat"), "unticked: quick.dat matches nothing again");
            CheckEqual("File Translations:", translationsLabel.Text, "and the translations panel no longer mentions it");

            File.Delete(Path.Combine(save.NonXboxFolder, "quick.dat"));
            settings.AllowWebDataFetch = false;
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "the lists without quick.dat");

            Say("== A translation with a mistyped pattern, then a click in each file list");
            FileTranslation mistyped = new FileTranslation { ContainerName1 = "SaveGame", ContainerName2 = "", XboxFileID = "${Slot}", NonXboxFilename = "${Slot}", NamedRegexGroups = new[] { "(?<Slot>[0-9" } };
            form.ActiveGame.FileTranslations.Insert(0, mistyped);
            status.Text = string.Empty;
            nonXboxFiles.ClearSelection();
            nonXboxFiles.Rows[0].Selected = true;
            await Task.Delay(150);
            Check(status.Text.StartsWith("The matching file could not be worked out:"), "a click in the non-Xbox list reports it in the status line: \"" + status.Text + "\"");
            status.Text = string.Empty;
            xboxFiles.ClearSelection();
            xboxFiles.Rows[0].Selected = true;
            await Task.Delay(150);
            Check(status.Text.StartsWith("The matching file could not be worked out:"), "a click in the Xbox list reports it in the status line");
            CheckEqual(0, responder.ErrorWindowsSeen, "no window came up for either");
            form.ActiveGame.FileTranslations.Remove(mistyped);

            Say("== Files a transfer leaves out: named, with the reason, before anything is copied (#164)");
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "notes.txt"), "steam notes");
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "saveFile9.sav"), "steam slot 9");
            form.ActiveGame.FilesLeftOut = new List<FileLeftOut>
            {
                new FileLeftOut { NonXboxFilename = "saveFile9\\.sav", Reason = "Left out for this test, Steam side." },
                new FileLeftOut { ContainerName1 = "SaveGame", XboxFileID = "SaveSlot3", Reason = "Left out for this test, Xbox side." }
            };
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 5, "the lists with the two new files in them");
            SortedDictionary<string, string> xboxBeforeLeavingOut = FolderContents.Read(save.ProfileFolder);
            SortedDictionary<string, string> filesBeforeLeavingOut = FolderContents.Read(save.NonXboxFolder);

            responder.AnswerByTitle["Are you sure?"] = Answer.No;
            boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            allToXbox.PerformClick();
            await WaitUntil(() => status.Text == "Transfer canceled" && !responder.AnyOpen(), "the transfer to Xbox to be called off");
            question = responder.Transcript.Skip(boxesBefore).FirstOrDefault(t => t.Contains("[Are you sure?]")) ?? string.Empty;
            Check(question.Contains("2 of the 5 files are left out."), "to Xbox: the question says how many files are left out");
            Check(question.Contains("Left out for this test, Steam side.") && question.Contains("    saveFile9.sav"), "it gives the library's reason for the file the library leaves out");
            Check(question.Contains(GameInfo.NoTranslationReason) && question.Contains("    notes.txt"), "and says that no translation covers the other one");
            Check(!responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Error]")), "no error box came up for either of them");
            CheckSame(xboxBeforeLeavingOut, FolderContents.Read(save.ProfileFolder), "answered No: the Xbox save is untouched");

            boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            allFromXbox.PerformClick();
            await WaitUntil(() => status.Text == "Transfer canceled" && !responder.AnyOpen(), "the transfer from Xbox to be called off");
            responder.AnswerByTitle.Remove("Are you sure?");
            question = responder.Transcript.Skip(boxesBefore).FirstOrDefault(t => t.Contains("[Are you sure?]")) ?? string.Empty;
            Check(question.Contains("1 of the 2 files is left out.") && question.Contains("Left out for this test, Xbox side.") && question.Contains("    SaveGame / SaveSlot3"), "from Xbox: the question names the file the library leaves out, with its reason");
            CheckSame(filesBeforeLeavingOut, FolderContents.Read(save.NonXboxFolder), "answered No: the non-Xbox folder is untouched");

            Say("== Every selected file is left out: said so, and nothing is asked or copied");
            foreach (DataGridViewRow row in nonXboxFiles.Rows)
            {
                row.Selected = ((NonXboxFileInfo)row.DataBoundItem).RelativePath == "notes.txt";
            }
            boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            selectionToXbox.PerformClick();
            await WaitUntil(() => status.Text == "Nothing copied" && !responder.AnyOpen(), "the notice that nothing is copied");
            string notice = responder.Transcript.Skip(boxesBefore).FirstOrDefault(t => t.Contains("[Nothing to copy]")) ?? string.Empty;
            Check(notice.Contains("The file is left out.") && notice.Contains("    notes.txt") && notice.Contains("wiki/File-Translations"), "the notice names the file and says where translations are explained");
            Check(!responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Are you sure?]") || t.Contains("[Error]")), "nothing else came up");
            CheckSame(xboxBeforeLeavingOut, FolderContents.Read(save.ProfileFolder), "the Xbox save is untouched");

            form.ActiveGame.FilesLeftOut = null;
            File.Delete(Path.Combine(save.NonXboxFolder, "notes.txt"));
            File.Delete(Path.Combine(save.NonXboxFolder, "saveFile9.sav"));
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "the lists without the two files");

            Say("== A game the library warns about: told once when it is selected, and before each transfer the warning is about");
            SortedDictionary<string, string> xboxBeforeWarnings = FolderContents.Read(save.ProfileFolder);
            SortedDictionary<string, string> filesBeforeWarnings = FolderContents.Read(save.NonXboxFolder);
            string libraryBefore = settings.DefaultGameLibrary;
            List<GameWarning> madeUpWarnings = new List<GameWarning>
            {
                new GameWarning { Name = "Test Game", PackageName = package, Text = "Made up for this test, either way (#0)." },
                new GameWarning { Name = "Test Game", PackageName = package, Direction = GameWarning.FromXbox, Text = "Made up for this test, from Xbox only (#0)." }
            };
            settings.DefaultGameLibrary = libraryBefore.Substring(0, libraryBefore.LastIndexOf('}')) + ", \"Warnings\": " + System.Text.Json.JsonSerializer.Serialize(madeUpWarnings) + " }";
            // Setting it again makes the library be read again, as a newly downloaded one would be.
            GameLibrary.Settings = settings;

            boxesBefore = responder.Transcript.Count;
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Known problem]")) && !responder.AnyOpen(), "the notice about the game");
            string warned = responder.Transcript.Skip(boxesBefore).First(t => t.Contains("[Known problem]"));
            Check(warned.Contains("a problem with Test Game.") && warned.Contains("Made up for this test, either way (#0).") && warned.Contains("Copying from Xbox: Made up for this test, from Xbox only (#0)."), "the notice gives both warnings, the one about one way with its direction");
            Check(warned.Contains("wiki/Game-Compatibility"), "and says where more is known");
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "both lists");

            boxesBefore = responder.Transcript.Count;
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "both lists after selecting the game again");
            await Task.Delay(300);
            Check(!responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Known problem]")), "selected again: the notice does not come up a second time");

            responder.AnswerByTitle["Are you sure?"] = Answer.No;
            boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            allToXbox.PerformClick();
            await WaitUntil(() => status.Text == "Transfer canceled" && !responder.AnyOpen(), "the transfer to Xbox to be called off");
            question = responder.Transcript.Skip(boxesBefore).FirstOrDefault(t => t.Contains("[Are you sure?]")) ?? string.Empty;
            Check(question.Contains("Players have reported a problem with this:") && question.Contains("either way (#0).") && !question.Contains("from Xbox only"), "to Xbox: the question gives the warning about either way, not the one about the other way");

            boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            allFromXbox.PerformClick();
            await WaitUntil(() => status.Text == "Transfer canceled" && !responder.AnyOpen(), "the transfer from Xbox to be called off");
            responder.AnswerByTitle.Remove("Are you sure?");
            question = responder.Transcript.Skip(boxesBefore).FirstOrDefault(t => t.Contains("[Are you sure?]")) ?? string.Empty;
            Check(question.Contains("either way (#0).") && question.Contains("from Xbox only (#0)."), "from Xbox: the question gives both");
            CheckSame(xboxBeforeWarnings, FolderContents.Read(save.ProfileFolder), "answered No both times: the Xbox save is untouched");
            CheckSame(filesBeforeWarnings, FolderContents.Read(save.NonXboxFolder), "and so is the non-Xbox folder");

            settings.DefaultGameLibrary = libraryBefore;
            GameLibrary.Settings = settings;

            Say("== File > Request a Translation: the GitHub form opens with both file lists, after a question");
            ToolStripMenuItem requestMenu = Field<ToolStripMenuItem>(form, "requestTranslationToolStripMenuItem");
            ToolStripMenuItem copyTablesMenu = Field<ToolStripMenuItem>(form, "copySaveFileTablesToolStripMenuItem");
            // Stand-ins: the clipboard is the user's, and no browser may open.
            List<string> pagesOpened = new List<string>();
            List<string> copied = new List<string>();
            form.OpenWebPage = address => pagesOpened.Add(address);
            form.SetClipboardText = text => copied.Add(text);
            Check(requestMenu.Enabled, "the menu item is on once a game is selected");

            responder.AnswerByTitle["Request a translation"] = Answer.No;
            boxesBefore = responder.Transcript.Count;
            requestMenu.PerformClick();
            await WaitUntil(() => responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Request a translation]")) && !responder.AnyOpen(), "the question before the request");
            CheckEqual(0, pagesOpened.Count, "answered No: no page is opened");
            responder.AnswerByTitle.Remove("Request a translation");

            boxesBefore = responder.Transcript.Count;
            requestMenu.PerformClick();
            await WaitUntil(() => pagesOpened.Count == 1 && !responder.AnyOpen(), "the request page");
            string asked = responder.Transcript.Skip(boxesBefore).First(t => t.Contains("[Request a translation]"));
            Check(asked.Contains("The request is public") && asked.Contains("filled in for you"), "the question says the request is public and comes filled in");
            string page = Uri.UnescapeDataString(pagesOpened[0]);
            Check(pagesOpened[0].StartsWith("https://github.com/Fr33dan/GPSaveConverter/issues/new?template=translation-request.yml&"), "answered Yes: the page is the translation request form");
            Check(page.Contains("&game=Test Game&") && page.Contains("| SaveGame |  | SaveSlot0 |") && page.Contains("| saveFile0.sav |"), "it is filled in with the game and both file lists");
            string hiddenLocation = "Non-Xbox save location: " + TranslationRequest.HideUserFolders(form.ActiveGame.BaseNonXboxSaveLocation, TranslationRequest.UserFolders());
            Check(page.Contains(hiddenLocation), "the save folder is written with the user's own folders hidden: \"" + hiddenLocation + "\"");
            CheckEqual(0, copied.Count, "nothing is put on the clipboard, as the lists fit in the page");

            copyTablesMenu.PerformClick();
            Check(copied.Count == 1 && copied[0].Contains("| saveFile0.sav |") && copied[0].Contains(hiddenLocation), "File > Copy Save File Table(s) copies the same tables, folders hidden");

            Say("== File > Suggest a Translation: nothing when no names agree, and one that pairs the files when they do");
            ToolStripMenuItem suggestMenu = Field<ToolStripMenuItem>(form, "suggestTranslationToolStripMenuItem");
            ListBox translationList = Field<ListBox>(form, "fileTranslationListBox");
            List<FileTranslation> ownTranslations = form.ActiveGame.FileTranslations.ToList();
            form.ActiveGame.FileTranslations.Clear();
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "both lists, with the game's translation set aside");
            Check(suggestMenu.Enabled, "the menu item is on once a game is selected");

            boxesBefore = responder.Transcript.Count;
            suggestMenu.PerformClick();
            await WaitUntil(() => responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Suggest a translation]")) && !responder.AnyOpen(), "the answer to the first suggestion");
            Check(responder.Transcript.Skip(boxesBefore).First(t => t.Contains("[Suggest a translation]")).Contains("None of the usual kinds"), "SaveSlot0 and saveFile0.sav share no name: nothing is suggested");
            CheckEqual(0, form.ActiveGame.FileTranslations.Count, "and nothing is added");

            File.WriteAllText(Path.Combine(save.NonXboxFolder, "SaveSlot0"), "steam copy of slot 0");
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "SaveSlot3"), "steam copy of slot 3");
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 5, "the lists with SaveSlot0 and SaveSlot3 in them");

            responder.AnswerByTitle["Suggest a translation"] = Answer.No;
            boxesBefore = responder.Transcript.Count;
            suggestMenu.PerformClick();
            await WaitUntil(() => responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Suggest a translation]")) && !responder.AnyOpen(), "the suggestion");
            responder.AnswerByTitle.Remove("Suggest a translation");
            string suggested = responder.Transcript.Skip(boxesBefore).First(t => t.Contains("[Suggest a translation]"));
            Check(suggested.Contains("pairs 2 of the 2 Xbox files") && suggested.Contains("SaveGame / SaveSlot0   with   SaveSlot0") && suggested.Contains("SaveGame / SaveSlot3   with   SaveSlot3"), "the question shows the pairs it found");
            Check(suggested.Contains("It also covers 3 files on this PC that the Xbox save has nothing for"), "and says how many other files it covers that have no Xbox copy");
            CheckEqual(0, form.ActiveGame.FileTranslations.Count, "answered No: nothing is added");

            suggestMenu.PerformClick();
            await WaitUntil(() => form.ActiveGame.FileTranslations.Count == 1 && !responder.AnyOpen(), "the suggestion to be added");
            FileTranslation added = form.ActiveGame.FileTranslations[0];
            CheckEqual("${FileName} | SaveGame |  | ${FileName}", added.NonXboxFilename + " | " + added.ContainerName1 + " | " + added.ContainerName2 + " | " + added.XboxFileID, "answered Yes: the translation is added to the game's");
            CheckEqual(1, translationList.Items.Count, "and the translations panel lists it");
            CheckEqual("SaveSlot3", await MatchOnXbox(nonXboxFiles, xboxFiles, "SaveSlot3"), "with it, SaveSlot3 is matched to its Xbox file");

            form.ActiveGame.FileTranslations.Clear();
            form.ActiveGame.FileTranslations.AddRange(ownTranslations);
            File.Delete(Path.Combine(save.NonXboxFolder, "SaveSlot0"));
            File.Delete(Path.Combine(save.NonXboxFolder, "SaveSlot3"));
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "the lists as they were");

            Say("== A newer release: a link to it at the right of the menu bar, and none while this copy is the newest");
            // The window asks once, as it opens. These steps ask again the same way, with internet access allowed.
            ToolStripMenuItem newVersionMenu = Field<ToolStripMenuItem>(form, "newVersionToolStripMenuItem");
            StandInNetwork network = (StandInNetwork)GameLibrary.HttpClient;
            Check(!newVersionMenu.Available, "internet access was not allowed at the start: there is no link");

            settings.AllowWebDataFetch = true;
            network.LatestRelease = "{ \"tag_name\": \"v." + ErrorReport.Version() + "\", \"draft\": false, \"prerelease\": false }";
            await form.checkForNewerRelease();
            Check(!newVersionMenu.Available, "the newest release is this copy: no link");

            network.LatestRelease = "{ \"tag_name\": \"v.99.0\", \"draft\": false, \"prerelease\": false }";
            status.Text = string.Empty;
            await form.checkForNewerRelease();
            Check(newVersionMenu.Available, "a newer release: the link is shown");
            CheckEqual("Update available: v.99.0", newVersionMenu.Text, "it names the release");
            await WaitUntil(() => status.Text.Contains("v.99.0"), "the status line to say v.99.0 is out");
            Check(true, "and the status line says it is out: \"" + status.Text + "\"");
            pagesOpened.Clear();
            newVersionMenu.PerformClick();
            CheckEqual(ReleaseCheck.DownloadPage, pagesOpened.SingleOrDefault(), "a click opens the download page");

            network.LatestRelease = null;
            settings.AllowWebDataFetch = false;

            Say("== File > Open Xbox Save Folder: a copy of the save is opened and copied from, and nothing is written to it (#138)");
            ToolStripMenuItem openFolderMenu = Field<ToolStripMenuItem>(form, "openXboxSaveFolderToolStripMenuItem");
            Label xboxLabel = Field<Label>(form, "xboxFileLabel");
            Button exploreXbox = Field<Button>(form, "viewXboxFilesButton");
            ListBox xboxProfiles = Field<ListBox>(form, "xboxProfileListBox");
            Func<XboxContainerIndex> openSave = () => Field<XboxContainerIndex>(form, "currentContainer");
            Check(openFolderMenu.Enabled, "the menu item is on once the library is loaded");
            // The window's folder picker cannot be worked from here. These steps start where it ends.
            string copies = Path.Combine(Path.GetDirectoryName(save.BackupFolder), "C");
            Directory.CreateDirectory(Path.Combine(copies, "empty"));

            boxesBefore = responder.Transcript.Count;
            await form.openXboxSaveFolder(Path.Combine(copies, "empty"));
            await WaitUntil(() => !responder.AnyOpen(), "the notice about the empty folder");
            Check(responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Open Xbox save folder]") && t.Contains("There is no Xbox save in")), "a folder with no save in it: the user is told what to pick");
            Check(!openSave().IsPickedFolder, "and the Xbox save open is still the Xbox app's");

            string keptCopy = Path.Combine(copies, "kept", Path.GetFileName(save.ProfileFolder));
            CopyFolder(save.ProfileFolder, keptCopy);
            SortedDictionary<string, string> xboxBeforePicking = FolderContents.Read(save.ProfileFolder);
            string steamSlot0 = File.ReadAllText(Path.Combine(save.NonXboxFolder, "saveFile0.sav"));
            await form.openXboxSaveFolder(Path.Combine(copies, "kept"));
            await WaitUntil(() => openSave().IsPickedFolder && xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "the save in the copy");
            Check(openSave().getFileList().All(f => f.getFilePath().StartsWith(keptCopy)), "the folder above the copy was picked: the copy is the Xbox save open");
            CheckEqual(package, form.ActiveGame.PackageName, "the game is the one the copy's index names");
            CheckEqual(FakeXboxSave.ProfileID, xboxProfiles.SelectedItem as string, "the profile is read from the copy's folder name");
            CheckEqual("Xbox Save Files: from a folder you picked, read only", xboxLabel.Text, "the label over the Xbox files says where they are from");
            Check(xboxLabel.Right < exploreXbox.Left, "and ends before the Explore Xbox Files button");

            // Told apart from the Xbox app's own copy of the file by what is in it.
            File.WriteAllText(openSave().getFileList().First(f => f.FileID == "SaveSlot0").getFilePath(), "kept slot 0");
            foreach (DataGridViewRow row in xboxFiles.Rows)
            {
                row.Selected = ((XboxFileInfo)row.DataBoundItem).FileID == "SaveSlot0";
            }
            status.Text = string.Empty;
            selectionFromXbox.PerformClick();
            await WaitUntil(() => status.Text.StartsWith("Transfer complete") && !responder.AnyOpen(), "the copy from the picked save");
            CheckEqual("kept slot 0", File.ReadAllText(Path.Combine(save.NonXboxFolder, "saveFile0.sav")), "copying from Xbox takes the file from the picked save");

            SortedDictionary<string, string> copyBefore = FolderContents.Read(keptCopy);
            boxesBefore = responder.Transcript.Count;
            status.Text = string.Empty;
            allToXbox.PerformClick();
            await WaitUntil(() => status.Text == "Nothing copied" && !responder.AnyOpen(), "the transfer to the picked save to be refused");
            Check(responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Copy to Xbox]") && t.Contains("never to it") && t.Contains("select the game in the list")), "copying to Xbox is refused, and the user is told how to copy to the Xbox app's save");
            Check(!responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Are you sure?]")), "before anything else is asked");
            CheckSame(copyBefore, FolderContents.Read(keptCopy), "the picked save is untouched");
            CheckSame(xboxBeforePicking, FolderContents.Read(save.ProfileFolder), "and so is the Xbox app's save");

            SaveBackup xboxBackup = store.List(package).FirstOrDefault(b => b.Side == BackupSide.Xbox);
            Check(xboxBackup != null, "a backup of the Xbox save is there from the transfers before");
            await form.openXboxSaveFolder(xboxBackup.Folder);
            await WaitUntil(() => openSave().xboxProfileFolder == xboxBackup.FilesFolder, "the save in the backup");
            CheckEqual(FakeXboxSave.ProfileID, xboxProfiles.SelectedItem as string, "a backup the tool made opens too, with the profile its record names");

            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => !openSave().IsPickedFolder && xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3, "the Xbox app's save again");
            CheckEqual("Xbox Save Files:", xboxLabel.Text, "selecting the game in the list goes back to the Xbox app's save");
            File.WriteAllText(Path.Combine(save.NonXboxFolder, "saveFile0.sav"), steamSlot0);

            Say("== Edit... in the translations panel: a translation in plain words, with the pairs it makes shown as the names are typed");
            ToolStripMenuItem showTranslationsMenu = Field<ToolStripMenuItem>(form, "showFileTranslationsToolStripMenuItem");
            Button editTranslation = Field<Button>(form, "editTranslationButton");
            List<FileTranslation> translationsBefore = form.ActiveGame.FileTranslations.ToList();
            FileTranslation slotTranslation = translationsBefore[0];
            showTranslationsMenu.PerformClick();
            await WaitUntil(() => editTranslation.Visible, "the translations panel");
            Check(!editTranslation.Enabled, "Edit... is off while no translation is selected");
            translationList.SelectedIndex = 0;
            Check(editTranslation.Enabled, "and on once one is");

            string editorLayout = null;
            Task editing = DriveTranslationEditor(async editor =>
            {
                editorLayout = LayoutProblems(editor);
                TextBox fileBox = Field<TextBox>(editor, "fileBox");
                ListBox previewList = Field<ListBox>(editor, "previewList");
                Label summary = Field<Label>(editor, "summaryLabel");
                Label problemLabel = Field<Label>(editor, "problemLabel");
                Button ok = Field<Button>(editor, "okButton");
                CheckEqual("SaveGame |  | SaveSlot{FileSlot} | saveFile{FileSlot}.sav",
                    Field<TextBox>(editor, "container1Box").Text + " | " + Field<TextBox>(editor, "container2Box").Text + " | " + Field<TextBox>(editor, "blobBox").Text + " | " + fileBox.Text,
                    "the names are shown as they are written, with the translation's own placeholder");
                Check(Field<Label>(editor, "introLabel").Text.Contains("{FileSlot}"), "and the words above them name that placeholder");
                CheckEqual("SaveGame / SaveSlot0   <->   saveFile0.sav | SaveGame / SaveSlot3   <->   saveFile3.sav | saveFile7.sav   ->   SaveGame / SaveSlot7   (new on Xbox)",
                    string.Join(" | ", previewList.Items.Cast<string>()), "it lists the pairs it makes of the files listed now, and the file it would make on Xbox");
                CheckEqual("It covers 2 of the 2 Xbox files and 3 of the 3 files on this PC.", summary.Text, "and says how many it covers");

                fileBox.Text = "steam{FileSlot}.sav";
                Check(previewList.Items.Cast<string>().Contains("SaveGame / SaveSlot0   ->   steam0.sav   (new on this PC)"), "a changed name changes the list at once");
                CheckEqual("It covers 2 of the 2 Xbox files and 0 of the 3 files on this PC.", summary.Text, "and the count");

                fileBox.Text = "steam{slot}.sav";
                Check(problemLabel.Text.StartsWith("{slot} is not a placeholder.") && !ok.Enabled, "a placeholder it does not know is named, and OK is off: \"" + problemLabel.Text + "\"");

                Field<Button>(editor, "cancelButton").PerformClick();
                await Task.Yield();
            });
            editTranslation.PerformClick();
            await editing;
            CheckEqual(string.Empty, editorLayout, "nothing in the editor sticks out of it or overlaps");
            Check(ReferenceEquals(slotTranslation, form.ActiveGame.FileTranslations[0]) && slotTranslation.NonXboxFilename == "saveFile${FileSlot}.sav", "Cancel: the translation is as it was");

            editing = DriveTranslationEditor(async editor =>
            {
                Field<TextBox>(editor, "fileBox").Text = "steam{FileSlot}.sav";
                Field<Button>(editor, "okButton").PerformClick();
                await Task.Yield();
            });
            translationList.SelectedIndex = 0;
            editTranslation.PerformClick();
            await editing;
            FileTranslation edited = form.ActiveGame.FileTranslations[0];
            CheckEqual("SaveSlot${FileSlot} | steam${FileSlot}.sav | (?<FileSlot>[0-9]+)", edited.XboxFileID + " | " + edited.NonXboxFilename + " | " + string.Join(",", edited.NamedRegexGroups), "OK: the changed translation takes the old one's place, with its placeholder kept");
            Check(ReferenceEquals(edited, translationList.Items[0]) && !form.ActiveGame.FileTranslations.Contains(slotTranslation), "in the game's list and in the panel's");
            CheckEqual(string.Empty, await MatchOnXbox(nonXboxFiles, xboxFiles, "saveFile0.sav"), "saveFile0.sav no longer matches an Xbox file");

            FileTranslation pattern = new FileTranslation { NamedRegexGroups = new string[0], ContainerName1 = "Save.*", ContainerName2 = "", XboxFileID = "data", NonXboxFilename = "save.dat" };
            form.ActiveGame.FileTranslations.Insert(0, pattern);
            translationList.Items.Insert(0, pattern);
            translationList.SelectedIndex = 0;
            boxesBefore = responder.Transcript.Count;
            editTranslation.PerformClick();
            await WaitUntil(() => !responder.AnyOpen(), "the answer about the pattern");
            Check(responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Edit translation]") && t.Contains("plain words cannot show")), "a translation with a pattern in it is not opened, and the user is told why");
            Check(!Application.OpenForms.OfType<TranslationEditorForm>().Any(), "and no editor opens");

            form.ActiveGame.FileTranslations.Clear();
            form.ActiveGame.FileTranslations.AddRange(translationsBefore);
            showTranslationsMenu.PerformClick();
            Select(form, packages, form.ActiveGame);
            await WaitUntil(() => xboxFiles.RowCount == 2 && nonXboxFiles.RowCount == 3 && translationList.Items.Count == 1, "the game with its own translation again");

            Say("== An Xbox save that cannot be read");
            ListBox profiles = Field<ListBox>(form, "xboxProfileListBox");
            string indexFile = Path.Combine(save.ProfileFolder, "containers.index");
            byte[] goodIndex = File.ReadAllBytes(indexFile);
            File.WriteAllBytes(indexFile, goodIndex.Take(20).ToArray());
            responder.ErrorWindowsExpected = 1;
            profiles.SelectedIndex = -1;
            profiles.SelectedIndex = 0;
            await WaitUntil(() => responder.ErrorWindowsSeen == 1 && !Application.OpenForms.OfType<ErrorForm>().Any(), "the error window for the Xbox save");
            CheckEqual(0, xboxFiles.RowCount, "the Xbox list is empty");
            status.Text = string.Empty;
            allToXbox.PerformClick();
            await WaitUntil(() => responder.Transcript.Any(t => t.Contains("[Configure Xbox Location]")) && !responder.AnyOpen(), "the transfer to be refused");
            Check(true, "a transfer is refused while no Xbox save is open");
            File.WriteAllBytes(indexFile, goodIndex);
            profiles.SelectedIndex = -1;
            profiles.SelectedIndex = 0;
            await WaitUntil(() => xboxFiles.RowCount == 2, "the Xbox list after the index is whole again");
            Check(true, "selecting the profile again reads the save once it is whole");

            Say("== An error that nothing in the application handles");
            GameInfo missing = GameLibrary.getGameInfo("Missing.Game_0000000000000");
            responder.ErrorWindowsExpected = 1;
            Select(form, packages, missing);
            await WaitUntil(() => responder.ErrorWindowsSeen == 2 && !Application.OpenForms.OfType<ErrorForm>().Any(), "the error window for the unhandled error");
            string lastWindow = responder.Transcript.Last();
            Check(lastWindow.Contains(ErrorReport.UnexpectedProblem), "the window says the tool did not expect the problem");
            Check(lastWindow.Contains("DirectoryNotFoundException") && lastWindow.Contains("Game: Missing.Game_0000000000000"), "its details name the error and the game");
            Check(form.Enabled && !form.IsDisposed, "the main window is still there and usable");
        }

        private static void CopyFolder(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
            }
            foreach (string folder in Directory.GetDirectories(from))
            {
                CopyFolder(folder, Path.Combine(to, Path.GetFileName(folder)));
            }
        }

        /// <summary>Selects a game the way a click on it in the package list does.</summary>
        private static void Select(SaveFileConverterForm form, DataGridView packages, GameInfo game)
        {
            packages.DataSource = new[] { game };
            packages.Rows[0].Selected = true;
            form.GetType().GetMethod("packagesDataGridView_Click", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { packages, EventArgs.Empty });
        }

        /// <summary>The first list of non-Xbox profiles, or null while there is none.</summary>
        private static DataGridView ProfileList(TabControl profileTabs)
        {
            return profileTabs.TabPages.Count == 0 ? null : (DataGridView)profileTabs.TabPages[0].Controls[0];
        }

        /// <summary>Picks a profile the way a click on its row does.</summary>
        private static void ClickProfile(SaveFileConverterForm form, DataGridView list, int row)
        {
            list.ClearSelection();
            list.Rows[row].Selected = true;
            form.GetType().GetMethod("nonXboxProfileTable_CellClicked", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { list, null });
        }

        /// <summary>
        /// Selects one file in the non-Xbox list, as a click on it does, and reads which Xbox files
        /// the application highlights for it.
        /// </summary>
        private static async Task<string> MatchOnXbox(DataGridView nonXboxFiles, DataGridView xboxFiles, string relativePath)
        {
            nonXboxFiles.ClearSelection();
            foreach (DataGridViewRow row in nonXboxFiles.Rows)
            {
                row.Selected = ((NonXboxFileInfo)row.DataBoundItem).RelativePath == relativePath;
            }
            await Task.Delay(150);
            return string.Join(", ", xboxFiles.SelectedRows.Cast<DataGridViewRow>().Select(r => ((XboxFileInfo)r.DataBoundItem).FileID));
        }

        /// <summary>What <see cref="LayoutProblems"/> found the last time the Preferences window was opened.</summary>
        private static string preferencesLayout;

        /// <summary>
        /// Lists the controls of a window that stick out of it or lie on top of one another. The
        /// windows are laid out by hand in code, and nobody looks at them on the hidden desktop.
        /// </summary>
        /// <returns>What is wrong, or an empty text.</returns>
        private static string LayoutProblems(Form window)
        {
            List<string> problems = new List<string>();
            Control[] controls = window.Controls.Cast<Control>().Where(c => c.Visible).ToArray();
            for (int i = 0; i < controls.Length; i++)
            {
                Rectangle bounds = controls[i].Bounds;
                if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > window.ClientSize.Width || bounds.Bottom > window.ClientSize.Height)
                {
                    problems.Add(controls[i].Name + " sticks out of the window");
                }
                for (int j = i + 1; j < controls.Length; j++)
                {
                    if (bounds.IntersectsWith(controls[j].Bounds))
                    {
                        problems.Add(controls[i].Name + " overlaps " + controls[j].Name);
                    }
                }
            }
            return string.Join("; ", problems);
        }

        /// <summary>
        /// Opens Preferences from the menu, allows internet access, sets the option for translations
        /// that are being tested, and presses Save.
        /// </summary>
        private static async Task SavePreferences(SaveFileConverterForm form, bool useTranslationsBeingTested)
        {
            Field<ToolStripMenuItem>(form, "preferencesToolStripMenuItem").PerformClick();
            await WaitUntil(() => Application.OpenForms.OfType<PreferencesForm>().Any(f => f.Visible), "the Preferences window");

            PreferencesForm preferences = Application.OpenForms.OfType<PreferencesForm>().Single();
            preferencesLayout = LayoutProblems(preferences);
            Field<CheckBox>(preferences, "allowNetworkCheckbox").Checked = true;
            Field<CheckBox>(preferences, "previewTranslationsCheckbox").Checked = useTranslationsBeingTested;
            Field<Button>(preferences, "saveButton").PerformClick();
            await WaitUntil(() => !preferences.Visible, "the Preferences window to close");
        }

        /// <summary>
        /// Presses a transfer button and waits for the outcome.
        /// </summary>
        /// <returns>True if the transfer was refused for want of a non-Xbox profile, false if it ran.</returns>
        private static async Task<bool> IsRefusedForProfile(Button transfer, ToolStripStatusLabel status, DialogResponder responder)
        {
            int boxesBefore = responder.Transcript.Count;
            Func<bool> refused = () => responder.Transcript.Skip(boxesBefore).Any(t => t.Contains("[Configure Profile]") && t.Contains("Select non-Xbox Profile(s)"));
            status.Text = string.Empty;
            transfer.PerformClick();
            await WaitUntil(() => (refused() && !responder.AnyOpen()) || status.Text.StartsWith("Transfer complete"), "the transfer, or its refusal");
            return refused();
        }

        /// <summary>
        /// Works the translation editor while the button that opened it is waiting for it to close.
        /// </summary>
        private static async Task DriveTranslationEditor(Func<TranslationEditorForm, Task> work)
        {
            TranslationEditorForm editor = null;
            try
            {
                await WaitUntil(() => Application.OpenForms.OfType<TranslationEditorForm>().Any(f => f.Visible), "the translation editor");
                editor = Application.OpenForms.OfType<TranslationEditorForm>().Single();
                await work(editor);
            }
            finally
            {
                if (editor != null && !editor.IsDisposed && editor.Visible)
                {
                    editor.Close();
                }
            }
        }

        /// <summary>
        /// Works the Backups window while the menu handler that opened it is waiting for it to close.
        /// </summary>
        private static async Task DriveBackupsDialog(FakeXboxSave save, SaveBackupStore store, DialogResponder responder)
        {
            BackupsForm dialog = null;
            try
            {
                await WaitUntil(() => Application.OpenForms.OfType<BackupsForm>().Any(), "the Backups window");
                dialog = Application.OpenForms.OfType<BackupsForm>().Single();
                ListView list = Field<ListView>(dialog, "backupsListView");
                Button restore = Field<Button>(dialog, "restoreButton");
                Label details = Field<Label>(dialog, "detailsLabel");
                Label kept = Field<Label>(dialog, "keptLabel");

                CheckEqual("Backups - Test Game", dialog.Text, "the window is titled with the game");
                CheckEqual(2, list.Items.Count, "it lists two backups");
                CheckEqual("Xbox save | Copying 3 files to Xbox", list.Items[0].SubItems[1].Text + " | " + list.Items[0].SubItems[2].Text, "newest first: the Xbox save");
                CheckEqual("Non-Xbox files | Copying 2 files from Xbox", list.Items[1].SubItems[1].Text + " | " + list.Items[1].SubItems[2].Text, "then the non-Xbox files");
                Check(list.Items[0].Selected && restore.Enabled, "the newest is selected and can be restored");
                Say("          details: " + details.Text.Replace(Environment.NewLine, " "));
                Say("          footer:  " + kept.Text);

                // The Xbox save first.
                restore.PerformClick();
                await WaitUntil(() => list.Items.Count == 3 && dialog.Enabled && responder.Transcript.Count(t => t.Contains("[Backup restored]")) == 1 && !responder.AnyOpen(), "the Xbox restore");
                Check(dialog.Restored, "the window reports that something was restored");
                Check(list.Items[0].SubItems[2].Text.StartsWith("Restoring the backup from"), "what the restore replaced is listed as the newest backup");

                // Then the non-Xbox files: the oldest backup, at the bottom.
                list.Items[2].Selected = true;
                await WaitUntil(() => details.Text.Contains("puts back 1 file and removes 1 file that was not there then"), "the details of the non-Xbox backup");
                Say("          details: " + details.Text.Replace(Environment.NewLine, " "));
                restore.PerformClick();
                await WaitUntil(() => list.Items.Count == 4 && dialog.Enabled && responder.Transcript.Count(t => t.Contains("[Backup restored]")) == 2 && !responder.AnyOpen(), "the non-Xbox restore");
            }
            finally
            {
                if (dialog != null)
                {
                    dialog.Close();
                }
            }
        }
    }
}
