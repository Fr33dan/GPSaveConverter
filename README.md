# PC Gamepass Save File Converter
Games available through PC Game Pass or purchased via the Xbox app keep their save files in an arbitrary file structure. This means if you purchase a game on another storefront, moving your save files to or from the Xbox version is extremely difficult. This software aims to make that process easy.

Finds games by listing any Microsoft Store package with a wgs folder (believed to stand for "Windows Game Save"). This means some non-game applications may be shown.

There is an extremely small (as of now, at least) game library that contains the locations of non-Xbox save files. Some other game storefronts/save systems support multiple profiles; if this is the case with your game profiles will be shown by the file name identifying them. If your game is not found in this library, you will be prompted to select the location manually when a package is selected.

From v.0.4.12 the application backs up what a transfer is about to change, and **File > Backups** puts it back. With an older version, it is strongly recommended you back up both save file folders before using it.

![Save File Converter v0 4 3 Screenshot](https://user-images.githubusercontent.com/4670572/159533555-dbc49599-7620-4774-bd78-501e997ac22c.png)

## Getting help

Open an [issue](https://github.com/Fr33dan/GPSaveConverter/issues). From v.0.4.17, **File > Request a Translation** opens one for the selected game with its file lists filled in.

Issues are answered twice a day, at about 9:00 and 19:00 New York time. The replies are written by Claude, an AI assistant, on the maintainer's behalf. Adding a translation to the built-in library, closing an issue and making a release wait for the maintainer, so those can take longer.

Nobody maintaining the tool owns most of the games, so a translation you are given has only been checked against the file lists you posted. Please say in the issue whether the game loaded your save. Once someone confirms it, the translation goes into the tool for everyone.

If a game does not exist in the library, a custom file translation must be created. See the [video linked](https://youtu.be/EdCjEr4-jfc) below for an explanation of file translations and how to create them:

[![File translations tutorial](http://img.youtube.com/vi/EdCjEr4-jfc/0.jpg)](https://youtu.be/EdCjEr4-jfc "File translations tutorial") 

Garen Thomas created a YouTube video going over the process for Forza Horizon 5. The version used is older and buggier it may be useful for anyone still struggling to understand how to use the tool:

[![FH 5 Tutorial](http://img.youtube.com/vi/tGKV-aumZ-g/0.jpg)](http://www.youtube.com/watch?v=tGKV-aumZ-g "FH 5 Tutorial")
