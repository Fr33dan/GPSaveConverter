# Window test

Runs the application's real main window against a made-up save, presses the real buttons, and
compares the folders on disk afterwards. CI runs it on every pull request.

A real transfer needs a Game Pass save, and nobody maintaining this project has one for most games.
The unit tests cover the code behind the windows. This covers the windows themselves: what happens
when a button is pressed, in which order, and what the user is told.

## Running it

```
dotnet build tools/UiSmoke/UiSmoke.csproj -c Release
tools/UiSmoke/bin/Release/net472/GPSaveConverter.UiSmoke.exe
```

It prints each check as `ok` or `FAIL`, then the text of every message box and error window the
application showed. The exit code is 0 when every check passed.

The project is not in the solution, so `dotnet build GPSaveConverter.sln` and the release build leave
it out.

## What to expect

- **Nothing appears on screen.** The windows open on a hidden desktop of their own, so they cannot
  take the keyboard from you. Pass `--visible` to watch it on your own desktop instead. If a hidden
  desktop cannot be set up, the program says so and uses the current one.
- **Nothing real is touched.** The Xbox save, the non-Xbox folder and the backups are throwaway
  folders under `%TEMP%\gpsc`, removed at the end. The settings are kept in memory. The network and
  PowerShell are not used.
- **Message boxes are answered for you.** Yes or OK unless the script says otherwise. The
  application's own error window is read and closed; one the script did not cause on purpose fails
  the run. Its Copy button is never pressed, because the clipboard is yours.

## What it covers

- A save with a container that the index lists but that is not on the PC: the rest is listed, the
  status line says so, and what the index says of that container survives a transfer.
- Copying everything from Xbox, and everything to Xbox, with a backup made first.
- A backup that cannot be made, answered with No: nothing is copied.
- **File ▸ Backups**: both backups listed, each restored, both folders back byte for byte, the file
  lists refreshed.
- Backups turned off in the preferences.
- A save location with a place for a non-Xbox profile: the transfer is refused until a profile is
  picked, and then goes into that profile's folder.
- A folder picked by hand, and the tool started again after it: the transfer goes ahead, with no
  profile asked for.
- A mistyped pattern in a translation: the status line says so and no window opens.
- An Xbox save that cannot be read: the error window names the profile, and no profile stays open.
- An error nothing handles: the error window comes up and the main window survives.

## Adding to it

`Script` in `Program.cs` is the scenario, read top to bottom. A step sets something up, presses a
button with `PerformClick()`, waits with `WaitUntil` for what the application should do, and then
checks the result with `Check` or `CheckEqual`.

Three things are easy to get wrong:

- **A window opened with `ShowDialog` blocks the code that opened it.** The code that works inside
  such a window has to be started before the click that opens it. `DriveBackupsDialog` is the example.
- **Private controls are reached by name**, with `Field<T>(form, "name")`. Renaming a control in the
  designer breaks the test at run time, not at build time.
- **An answer other than Yes or OK** is set per message box title, in `responder.AnswerByTitle`.

## Other modes

- `--speed` times backups and restores with many files and with large ones.
- `--wiki <folder>` runs the PCGamingWiki reader over saved page sections. Each `.txt` file has
  `TITLE: <name>` on its first line and the section's wikitext after it.
