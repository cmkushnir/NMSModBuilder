# Query Scripts
![](Tab_QueryScripts.png)

Use C# to query information from the currently loaded game instance, in particular from its pak items.

The tab toolbar has buttons to: create a new script, clear all script logs, save all script edits to disk, compile all scripts.</br>
The script toolbar has buttons to: delete, rename, save, compile, execute, stop execute (only enabled while script running).

</br>

## New Script

The New Script button will create a new script file in the app/Scripts/Query/ folder.
A file system watcher will detect the new file and add it to the listbox.
The class and file names are initially set to the current tick count.

A class cannot start with a number, so you must rename the class, and should rename the file.

</br>

## Editing
See: [Script API](Script_API.md)

You can add other fields, properties, methods, classes to the script file as desired.
However, a given script file should only have one class derived from cmk.NMS.QueryScript.

As you move the cursor over the script intellisense like feedback is provided in the script window statusbar (wip).
When you enter a '.' the app will display any available code-completion options in a popup e.g. methods, fields.

The script can reference all public app objects, fields, properties, methods.

> You can double-click a pak item path string to view the item in the PAK Items tab.

There are three main properties a query script will use:

- Game.  This is the currently loaded game instance.  It contains the following notable properties:
  - Location.  Game path, NMS build date, Release information.
  - MBINC.  libMBIN|MBINCompiler to be used based on Release information.
    Currently this will always be the link loaded libMBIN.dll.
  - PAK.  Collection of all game pak files.
  - MOD.  Collection of all mod pak files.
  - Language.  Dictionary of language Id - Value pairs for current language Identifier.
  - Substances, Products, Technologies.  Lists of the various in-game items.
  - RefinerRecipes, CookingRecipes.  Lists of the various in-game recipes.
- Log.  List of log items for script.  These items are displayed in the LogViewer below the script editor.
- Cancel.  A token that is signalled when you click the Cancel button in the script toolbar while the script is being Executed.

</br>

## Executing

> If you add Log entries in a tight loop the UI thread may spend all its time updating the Log view.
> This may result in the other app windows and controls becoming unresponsive.
> If you may need to capture many log messages consider writing them to a file instead.

When Log items are added, CollectionChanged?.DispatcherBeginInvoke is called to notify any listeners (the Log view window) of the change.
This can result in groups of Log items suddenly being displayed in a Log view,
as opposed to one at a time.  Although DispatcherInvoke would give a smoother Log view response,
it also adds significant performance overhead in these cases due to the background threads, that are adding the Log items, 
having to wait for the UI thread to handle each CollectionChanged event and update the Log view.

When a query script is executing you can select and execute other query scripts.
When you select a query script the script toolbar button states will update based on if the script is being executed or not.

</br>
