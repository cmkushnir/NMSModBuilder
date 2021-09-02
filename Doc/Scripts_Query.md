# Scripts - Query

<!--ts-->
* [Change Language](#ChangeLanguage)
* [Decompress All WEM](#DecompressAllWem)
* [Dump PAK Item Paths](#DumpPakItemPaths)
* [Find In MBIN](#FindInMBIN)
* [Get Descriptor Tree](#GetDescriptorTree)
<!--te-->

</br>

## ChangeLanguage
<pre>
// Change the current game instance language using the language Name.
Game.Language.Name = "ENGLISH";

// Change the current game instance language using a Language.Identifier.
Game.Language.Identifier = Game.Language.Identifier.English;  

// Create a local variable that loads a specific language dictionary.		
var korean = new NMS.Game.Language.Collection(
	Game, NMS.Game.Language.Identifier.Korean  // second parameter could also be a Name string.
);

// Lookup language Id to get localized strings:
var atlas_pass_1_en = Game.Language.FindId("ACCESS1_NAME_L");  // "AtlasPass v1"
var atlas_pass_1_ko = korean.FindId("ACCESS1_NAME_L");  // "아틀라스패스 v1"
</pre>

Changing the current game instance language also updates the game item (substances, products, technologies) and recipes lists.
This is the simplest approach if you need to access the localized strings of any of those lists.
When the script finishes the language will stay set to whatever it was last set to.


## DecompressAllWem
<pre>
foreach( var info in Game.PAK.FindInfoEndsWith(".WEM", Cancel) ) {
	var wem = info.Data&lt;NMS.PAK.Item.Data&gt;(Log);
}		
</pre>

Since FindInfo\* is iterating through all game pak items to find matching Info objects, it has an optional Cancel parameter.
If the Cancel toolbar button is clicked while the script is running the Cancel object is signalled.
This will cause FindInfo\* to stop iterating through the game pak items; a cancelled operation error will be added to the Log.

You can also explicitly test if Cancel is signalled by doing:
<pre>if( Cancel.IsCancellationRequested ) break|return;</pre>

Extracting data can result in errors, so Data has an optional Log parameter.
Items added to the script Log property are displayed in the Log view window.

Data can optionally take the type of NMS.PAK.\*.Data type you are expecting to be generated, it will cast the created Data to this type.
NMS.PAK.Item.Data is the base type for all other NMS.PAK.\*.Data types and is always safe to use.


## DumpPakItemPaths
<pre>
// Display the Save File dialog and return the selected path, or null|empty if none selected.
var save_path = Dialog.SaveFile();
if( save_path.IsNullOrEmpty() ) return;

// Create|open (truncate) a text file
var file = System.IO.File.CreateText(save_path);

// Iterate through all Info objects in the merged game pak Info tree.
Game.PAK.InfoTree.ForEachTag(( INFO, CANCEL, LOG ) => {
	// For each pak item write the pak file name and pak item path to the file
	file.WriteLine($"{INFO.File.Name} {INFO.Path}");
},	Cancel, Log);  // Cancel -> CANCEL, Log -> LOG
</pre>

ForEachTag takes 3 parameters: Action, Cancel, Log.
The Cancel and Log parameters are passed to the Action as CANCEL and LOG.
The Action INFO parameter is from the iteration.


## FindInMBIN
<pre>
Game.PAK.ForEachMbin(( MBIN, CANCEL, LOG ) => {
	var ebin   = MBIN.CreateEBIN();  // Convert binary to dom, dom to ebin string
	var index  = ebin.IndexOf("GcTextStyleOutline");  // Search ebin for string
	if( index >= 0 ) LOG.AddInformation($"{MBIN.Path}[{ebin.Length}] @ {index}.");  // Log if found match
},	Cancel, Log);
</pre>

Game.PAK.ForEachMbin iterates through all game pak Info records and extracts the NMS.PAK.MBIN.Data objects for .MBIN and .MBIN.PC items.
NMS.PAK.MBIN.Data contains methods to query the header information, 
get the top-level class name, convert mbin binary to|from an object tree (dom),
convert dom to ebin string,
convert dom to|from exml string.


## GetDescriptorTree
A modified version of this is used to generate the .lua files used by the No Man's Sky - Creative & Sharing Hub discord bot.

Demonstrates loading an initial mbin and then recursing through and loading referenced mbin's.
For each mbin it dumps whatever part of the descriptor tree it contains to a text file.

</br>
