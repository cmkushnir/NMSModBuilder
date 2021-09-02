# NMS Mod Builder

No Man's Sky Mod Builder.

<!--ts-->
* [Features](#Features)
* [Install](#Install)
* [Startup](#Startup)
* Tabs
  * [Application](Doc/Tab_Application.md)
    * [Download](Doc/Tab_Application.md#Download)
    * [Toolbar](Doc/Tab_Application.md#Toolbar)
    * [Statusbar](Doc/Tab_Application.md#Statusbar)
  * [libMBIN API](Doc/Tab_libMBIN.md)
  * [Language](Doc/Tab_Language.md)
  * [Substances, Products, Technologies](Doc/Tab_GameItems.md)
  * [Refiner & Cooking Recipes](Doc/Tab_GameRecipes.md)
  * [PAK Items](Doc/Tab_PakItems.md)
  * [Query Scripts](Doc/Tab_QueryScripts.md)
  * [Mod Scripts](Doc/Tab_ModScripts.md)
  * [Mod Builder](Doc/Tab_ModBuilder.md)
  * [Mod Diffs](Doc/Tab_ModDiffs.md)
* [Script API](Doc/Script_API.md)
* Script Samples
  * [Query](Doc/Scripts_Query.md)
  * [Mod](Doc/Scripts_Mod.md)
* [Dependencies](#Dependencies)
<!--te-->

</br>
</br>

## Features
A one-stop solution for creating NMS mods using C#:</br>
- Automatically detects normally installed Steam and GoG game instances.
  - Optionally select game instance using folder browser dialog, for non-standard installs.
- View MBINCompiler|libMBIN Enums, Classes, Fields.
- Select NMS language to view all language ID's and their localized values.
- View all substances, products, and technologies - ID, icon, localized names.
- View all refiner and cooking recipes - ID's, icons, localized names.
- View all game and mod pak items without having to unpack or decompile anything - it's all done on-demand in-memory.
  - Specialized viewers for common pak item types e.g. .mbin, .dds, .xml, .cs, .lua, ... .
  - Side-by-side views of game & mod pak items, with built-in differ for text-based views.
- Use C# to create query scripts that search game and mod pak items.
- Use C# to create mod scripts that modify pak items.
- Compile, enable, and execute mod scripts to create modified pak items, save the modified pak items in new mod pak files.
- Includes a number of query and mod scripts to get you started.

Some of the included mod scripts (easily adjustable values, easily enable only those you want to use):
  - Increase base radius, wire lengths, extractor and power rates and storage limits, ...
  - Adjust C,B,A,S class probabilities for poor, average, wealthy systems (e.g. ships, multitools).
  - Enable all creatures to become pets, enable all creatures to be ridable.
  - Adjust frequency of freighter battles.
  - Adjust C,B,A,S class probabilities for freighter tech quality.
  - Increase freighter warp distance.
  - Reduce negative penalties for frigates.
  - Remove camera shake effects.
  - Adjust stack sizes, current script sets max stack size to 100,000 for everything.
  - Make all upgrades install with maximum bonus'.
  - Remove lower-right notifications, shorten display time for other notifications.
  - Make all portal runes known, make portal buttons no-cost.
  - Adjust reward locations and chances for star charts, add portal as possible ancient chart location.
  - Make ship salvage terminals placeable anywhere e.g. in bases or right beside crashed ships.
  - Auto-mark various locations e.g. crashed ships, portals, when you get within range.
  - Remove bloom and star twinkle effect, make space darker.
  - Increase interaction and teleport distance between player and ship.
  - Make all ship weapons auto-aim.
  - Ship hover.
  - Increase spawn chance for royals, or increase number of royals per system.
  - Adjust asteroid spacing.
  - Make water clearer.
  - Many adjustments when starting a new game:
    - Random suit, ship, multitool.
    - Specify min|max starting distance to ship.
    - Specify starting units, nanites, quicksilver.
    - Specify number of inventory slots for suit, ship, multitool.
    - Easily add starting items to various inventories, many already defined.
    - Specify known tech and products.

</br>
</br>

## Install
Make sure [.NET Desktop Runtime 5.0.8](https://dotnet.microsoft.com/download/dotnet/5.0) or greater is installed.</br>
Download the latest [Release](https://github.com/cmkushnir/NMSModBuilder/releases/latest/download/NMSModBuilder.7z), or choose a Release from [Releases](https://github.com/cmkushnir/NMSModBuilder/releases).</br>
There is no installer; simply unzip the Release file into a folder.</br>
The app has no configuration.

> When updating, remember to backup any [Scripts](Doc/Scripts.md) you may have modified, before overwritting them with those from the newer Release.

</br>
</br>

## Startup
> The cmkNMSReleases.txt file in the app folder binds game releases to MBINCompiler | libMBIN versions, you must update it when new game releases and|or MBINCompiler | libMBIN versions are installed.

> You will only be able to create mods for game releases supported by the libMBIN.dll version in the app folder, as specified in cmkNMSReleases.txt.

Run cmkNMSModBuilder.exe.

When the app starts it may take 1-3 seconds before the window is displayed.
During that time the app is loading and linking all enums, classes, and fields from the libMBIN.dll in the app folder,
and searching for installed Steam and GoG game instances.

You will only see the GoG or Steam button if the app found a corresponding installed instance of the game.

![](Doc/Tab_Application.png)

Once the main window is displayed, select the game instance by clicking the GoG, Steam, or Select Folder button on the [application toolbar](Doc/Tab_Application.md#Toolbar).
It will take 8 - 15 sec to load and index the various mbin files.
The [Application tab](Doc/Tab_Application.md) log window will update as tasks are started and completed.

Loading a game instance:
- **Loading Types**:
  Each game instance gets it's own wrapper of the linked libMBIN.dll.
  This is to allow for different game instances that use the same libMBIN.dll version but may have different mbin's
  to display the game instance specific mbins in the [libMBIN API tab](Doc/Tab_libMBIN.md).
  So even though it already did this at the app level, it needs to do it again.
- **Loading all game & mod .pak item info**:
  Load meta-data for each .pak file in the PCBANKS and PCBANKS/MODS folders.
  - Load each .pak header and manifest.
    Pak item Info objects are created for each manifest entry.
    The Info objects contain the item Path and meta-data required to extract the item Data.
    Each .pak file wrapper maintains both a sorted list and a tree of manifest Info objects.
  - Load the header for each .pak mbin item.  Store the top-level class name in the Info object.
- **Building merged game .pak item info**:
  The game .pak files (PCBANKS) have their Info lists added to a merged Info tree.
- **Loading MBIN paths for libMBIN classes**:
  Link the Path of each game .pak mbin item to the top-level class in the game instance libMBIN wrapper.
- **Loading cmk.NMS.Game.Language.Collection**:
  Each supported language has Id - value pairs stored in 5 different mbin's.
  This loads all the mbin's for the current language, and adds all Id - value pairs to a dictionary.
  The dictionary is displayed in the [Language tab](Doc/Tab_Language.md).
  It is also used to map Game.Item Id's to localized strings.
- **Loading cmk.NMS.Game.Items.\*.Collection**:
  Load the corresponding mbin, create a wrapper for each item.
  The wrapper objects contain the Id's and localized strings, and 32x32 bitmap version of the icon.
  These collections are displayed in the [Substances, Products, Technologies tab](Doc/Tab_GameItems.md).
- **Loading cmk.NMS.Game.Recipes.\*.Collection**:
  Load the recipes mbin and create wrappers for each item.
  The wrapper objects contain the Id's and localized strings, and 32x32 bitmap version of the icon.
  These collections are displayed in the [Refiner & Cooking Recipes tab](Doc/Tab_GameRecipes.md).
- **Updated cmk.NMS.\*.Collection 'ENGLISH'**:
  Whenever the current language is changed the Game.Items and Game.Recipes lists have their localized strings updated.
  It takes 1-2 seconds to load a different language.

The above tasks are done in parallel.
This means that having a processor that supports more concurrent threads will help keep the load time down.
It also means the load may become I/O limited, especially if the game files are on a hard-disk instead of an SSD.

</br>
</br>

## Dependencies

- https://github.com/monkeyman192/MBINCompiler</br>
MBINCompiler.exe | libMBIN.dll decompiles|recompiles .mbin items to|from in-memory dom and .exml text.

- https://github.com/dotnet/roslyn</br>
Microsoft .NET C# compiler, used to compile C# scripts.

- https://github.com/icsharpcode/AvalonEdit</br>
AvalonEdit view|edit items that can be converted to text.

- https://github.com/icsharpcode/SharpZipLib</br>
SharpZipLib decompress' .pak item data.

- https://github.com/nickbabcock/Pfim</br>
Pfim converts (most) .dds items to bitmaps for viewing.

- https://github.com/mmanela/diffplex</br>
DiffPlex calculates differences between items that can be converted to text.

</br>
