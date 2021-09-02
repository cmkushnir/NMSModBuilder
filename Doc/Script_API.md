# Script API


In general the word __file__ is used to talk about data stored on a drive.
The term __pak item__ is used to refer to the items in a pak file e.g. mbin items.
If these items are save to disk then they would be called files e.g. mbin file.

In general __path__ refers to the fully qualified path, it includes the directory, name, and extension.
The __directory__ is everything in the path upto and including the last slash.
The __name__ is everything between the last slash and the last period.
The __extension__ is everything after and including the last period.

File paths can generally use whatever slash you want, they are usually corrected as needed.
Pak item paths need to be in the correct format: all upper-case, no leading slash, use '/' instead of '\\'.
Pak item searches are case-sensitive.

Pak item __Info__ objects contain the path of the item in the pak file, and the meta-data needed to extract the item __Data__ from the pak file.
When a pak file is loaded only the info objects are generated, the item data is extracted on-demand.
As such, iterating over the info is fast, while iterating over the data is slow, as each item has to be extracted.

Scripts generally start with the following helper libMBIN namespaces defined:
<pre>
using nms     = libMBIN.NMS;
using mbin_gl = libMBIN.NMS.Globals;
using mbin_gc = libMBIN.NMS.GameComponents;
using mbin_tk = libMBIN.NMS.Toolkit;

namespace cmk.NMS.Scripts.Mod
{
	using BaseTerrainEditShapeEnum = mbin_gc.GcBaseBuildingEntry.BaseTerrainEditShapeEnum;
	...
</pre>

Scripts that use libMBIN enums will generally create an alias using for them as well.

</br>

All scripts need to have a class that ultimately derives from cmk.NMS.Script, and has a void Execute() method.
cmk.NMS.Script defines three properties that are set while the Script is executing:
- Game: The current Game instance.  This is the main object used to get data from the current Game and Mod pak files.
- Log: Each Query script has its own Log, all Mod scripts share their Log with their parent ScriptFiles collection.
  Items written to the Script.Log will display in the Log view window below the script edit window.
  You should pass this Log object to any methods you call in the script that accept a Log parameter.
  Some methods do not accept a Log parameter, they will write any errors to Log.Default, which is displayed in the Application tab.
- Cancel: A token that will be signalled if you hit the Cancel button on the toolbar while the script is executing.
  You should pass this Cancel token to any methods you call in the script that accept a Cancel parameter.
  These are generally methods that could take a long time to run.

</br>

## Game.PAK and Game.MOD Collections

Search for pak files:
- FindFileFromPath: Find first matching pak file path.  For internal use, should generally use FindFileFromName.
- FindFileFromName: Find first matching pak file name.

Search for pak items:
- FindInfo: Find first matching pak item path in all pak files.
- FindInfoEndsWith: Find all pak items in all pak files that end with specified string (text).
- FindInfoRegex: Use a Regex pattern to search for matching pak item paths.</br>
  See: https://regex101.com/ to help with building valid Regex patterns.

Iterate over the Info or extracted Data using:
- ForEachInfo: Iterate over all pak item info objects in all pak files.
- ForEachData: Extract a new instance of each pak item Data.
- ForEachMbin: Extract a new instance of each pak mbin item.

Extract specific pak items:
- Data: Extract the pak item with the specified path, creates NMS.PAK.*.Data object based on extension, returns base PAK.Item.Data.
- Data<>: Call Data, casts return to specified NMS.PAK.*.Data type e.g. NMS.PAK.SPV.Data.
- Mbin: Extract the pak mbin item with the specified path, returns base libMBIN NMSTemplate.
- Mbin<>: Call Mbin, cast return to specified libMBIN NMSTemplate based class e.g. mbin_gl.GcDebugOptions.
- DdsBitmapSource:  Extract the pak dds item with the specified path and convert to a bitmap, can specify the height of the resulting bitmap.

The above methods can also be called on the Game object, which causes them to search both the PAK and MOD collections.
In particular, the FindInfo method will first first look through the MOD paks in reverse order, then look through the game PAK collection.
This allows Game.FindInfo to find the same pak item the game would use.
You would use this if you wanted to do mod on mod or create a mod patch.

</br>

## File System

Several static methods are available in the Dialog class:
- SelectFolder.  Open a dialog to prompt the user to select a folder.
  Returns an empty strin if the user does not select a folder.
- OpenFile. Open a dialog to prompt the user to select a file to open.
  Returns an empty strin if the user does not select a folder.
  Can specify initial path and file.
- SaveFile.  Open a dialog to prompt the user to select a save file path and name.
  Returns an empty strin if the user does not select a folder.
  Can specify initial path and file.
  Cannot specify allowable extensions, user can use any extension.

</br>

## Query Scripts

All query scripts should derive from cmk.NMS.QueryScript, which derives from cmk.NMS.Script.

The cmk.NMS.QueryScript Data<>( string PATH, Log LOG = null ) and Mbin<>( string PATH, Log LOG = null ) methods
call the cmk.NMS.Script methods of the same but that take an additional bool CACHE parameter.
The QueryScript methods pass CACHE = false.  This means every call to Data<>() or Mbin<>() will extract a new instance of the Data from the pak file.

</br>

## Mod Scripts

All mod scripts should derive from cmk.NMS.ModScript, which derives from cmk.NMS.Script.
 
The cmk.NMS.ModScript Data<>( string PATH, Log LOG = null ) and Mbin<>( string PATH, Log LOG = null ) methods
call the cmk.NMS.Script methods of the same but that take an additional bool CACHE parameter.
The ModScript methods pass CACHE = true.  This means the first call to Data<>() or Mbin<>(), for a given pak item,
will extract a new instance of the Data and store it in a cache;
subsequent calls will return the cached Data.  The Log shows 'Extracted' when a new instance is extracted
and 'Reference' when Data is returned from the cache.

Each of pak file maintains a list of cached Data objects.
When building a mod pak in the [Mod Builder tab](Tab_ModBuilder.md), 
all cached Data for the current game instance are Saved,
and any with edits are added to the resulting mod pak.

This means, if you really wanted to, you could mod a mod, or create a mod patch,
by calling Game.Mbin<>() or Game.MOD.Mbin<>() instead of Mbin<>() in a script; Mbin<>() just calls Game.PAK.Mbin<>().

This isn't the default behaviour as
i\) you really need to understand what the current mods contain, 
and ii\) people are lazy and would use this blindly hoping to merge a bunch of mods together.

The sample mod scripts generally group related functionality into a given script file and may modify multiple pak items,
as opposed to having each script only modify a single pak item.

To make it easier to disable functionality by mbin e.g. if libMBIN is broken for specific mbin's,
the Execute methods only call other helper methods, usually named by what mbin object they modify.

This allows the helper methods to generally use the same pattern:
<pre>
protected void GcDebugOptions()
{
	// Extract|reference a MBIN we want to modify.
	// If the MBIN was already extracted by a prior script we will get a reference to that instance.
	// Can use the PAK Items tab Copy button to copy a Path to paste here.
	// The third line in the PAK Items tab ebin view of the mbin will list the class name.
	// If you are unsure about the namespace you can lookup the class name in the libMBIN API tab.
	// Double-clicking the path in the script will view the item in the PAK Items tab.
	var mbin = Mbin&lt;mbin_gl.GcDebugOptions&gt;(
		"GCDEBUGOPTIONS.GLOBAL.MBIN"
	);
	// The entire GCDEBUGOPTIONS.GLOBAL.MBIN has been loaded into an object tree, we can access the fields directly.
	// This allows us to catch invalid field names at compile-time.
	// It also provides code-completion popups when we enter '.' after a variable name
	// to see all available methods, fields, properties, events.
	mbin.DisableBaseBuildingLimits = true;
}
</pre>

</br>
