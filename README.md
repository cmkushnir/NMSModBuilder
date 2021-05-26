# NMSModBuilder
No Man's Sky Mod Builder.

Alpha release for testers, no code available until goes live.

When the app starts select the game instance from the top-right: GoG, Steam, Select Folder.
The wait cursor will spin for 10 - 15 sec while it loads the language, products, substances, technologies, recipes .mbin files and indexes them.

If it needs a MBINCompiler or libMBIN version e.g. based on selected game instance or mbin you are trying to view, it will prompt to download the appropriate file from GitHub.

The cmkNMSReleases.txt file binds game releases to MBINCompiler|libMBIN versions, you must update it as new game releases and MBINCompiler|libMBIN versions are released.

Whatever version of libMBIN.dll is in the app folder will dictate the game release you can mod.  If you have multiple game instances, each a different release then you will need to manually change libMBIN.dll to the version for the release you want to mod.  You do not need to do this for viewing only modding, it will download whatever it needs for viewing.

All tabs are now active for use, from left-to-right:

Game Items:
- show icon, language id, and localized strings for all products, substances, technologies.

Game Recipes:
- show all refiner and cooking recipes.

PAK Viewer:
- enhanced version of NMS Pak Viewer app, includes:
- pak list of all mod and game pak files.  select blank pak tree to view all game pak items (default).
- breadcrumb to select item in current pak tree.
- double click on a path string in a mbin to navigate to that file (if it exists in the current pak tree).
- double click on a language, products, substances, technologies id to show the localized name in the toolbar (if it exists).
- use the prev|next navigation buttons like you would in a browser, keeps last 8 items on stack.
- custom 'ebin' text version of mbin files.
- uses spirv-cross.exe to view .spv shader files.
- if you pick a mod pak tree then will display side-by-side views of game-mod version of pak items (if they both exist).

Query Scripts:
- use C# to query game pak items.
- each script creates a distinct assembly that is loaded when run then unloaded.
- renaming the IScriptQuery class will auto-rename the script file after you stop editing for 2 sec.
- it's up to you to check the Cancel token when looping through many files.

Mod Scripts:
- use C# to mod game pak items, mainly mbin's.
- compiling individual scripts is just to verify syntax, use the build button to build the assembly w/ all mod script contents.
- renaming the IScriptMod class will auto-rename the script file after you stop editing for 2 sec.
  - critical to keep class and script file names in-sync.
  - there is also a folder created for each mod with the same name as the script, it is for loose files specific to the script - experimental.
- must build assembly here before can build mod pak in Build tab.

Build Mod:
- use the mod assembly created in the Mod Scripts tab to execute mods and build a mod pak from resulting modified items.
- first execute.  if nothing happens the mod assembly didn't build check log for each mod script to find error(s).
- the build mod pak stream (in memory).  after this you can use last tab to preview side-by-side changes made to files.
- finally save mod pak to disk, will default to game MODS folder but can save anywhere.
- if saved in game MODS folder then mod will now appear in PAK Viewer tab pak combobox.


Requires system to have .NET 5 installed:
https://dotnet.microsoft.com/download/dotnet/5.0

