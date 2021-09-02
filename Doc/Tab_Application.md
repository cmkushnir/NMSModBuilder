# Application
![](Tab_Application.png)

> In the future any app configuration settings would be placed here.

Currently displays the default Log.

Each log window has Clear and Save buttons in their toolbar.</br>
The default save file name is based on the data the log represents.

> If you have multiple game instances, for different Releases, then you will need to manually change libMBIN.dll to the appropriate version for the Release you want to mod.
> You do not need to do this for viewing mbins only modding them, it will prompt to download whatever it needs for viewing.

</br>

## Download
There are two times the app may prompt you to download a file:
1) You try to view an mbin that was built using a version of libMBIN that isn't in the app folder.
2) You click the app version button, in the bottom-right of the statusbar, and a newer app version is available on GitHub.

![](Download.png)

</br>

## Toolbar
![](Toolbar.png)

To the right of the tabs is the application toolbar.
It contains the game selection buttons (GoG, Steam, Folder), and the GitHub button.
The GitHub button opens the project GitHub site in your browser.

If you use the Select Folder button the following dialog will open:</br>
![](SelectGameFolder.png)</br>
Selecting a valid game folder will display the build date of the NMS.exe and the best guess of the game release.
If the selected path matches a discovered GoG or Steam install path, then any pre-loaded GoG or Steam game instance data will be used i.e. you cannot load two instances of the same game instance.

Since there is no official way to determine the game release from an installed instance, the application does the following:
- For GoG, the release information is extracted from a string in the registry.</br>
  e.g. 3.53 is extracted from Computer\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1446213994\ver = "3.53_Prisms_73811"
- For Steam, the release is looked up in cmkNMSReleases.txt based on the NMS.exe build date.
- For Selected Folders, the release is looked up in cmkNMSReleases.txt based on the NMS.exe build date, but the user can override by selecting a different release in the Select Game Location dialog.

> The NMS.exe build date for a given game release can be different for each platform, including GoG vs Steam.
> The NMS.exe build date may be a couple days before the official release date.
> The cmkNMSReleases.txt build dates should correspond to the Steam NMS.exe build dates.

</br>

## Statusbar
![](Statusbar.png)

The statusbar shows: version of libMBIN.dll in application folder (one link loaded by application),
the build date of the NMS.exe (once a game instance is loaded), and on the right the app version.

If you load a game instance that is supported by the libMBIN.dll in the app folder, then the libMBIN version in the statusbar will have a green background - you can create mods for this game instance,
otherwise the background will be red and all tabs other than Application, libMBIN API, and PAK Items will be hidden. 

Clicking the app version will check GitHub for a newer version, and prompt the user to download if one is found.
The user is responsible for unzipping and updating the app files.

</br>
