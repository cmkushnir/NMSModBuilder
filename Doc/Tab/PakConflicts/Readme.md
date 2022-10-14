# PAK Conflicts
The conflicts tab is only displayed if conflicts are found.
>The application will check for conflicts whenever it detects a *.pak change in the nms/GAMEDATA/PCBANKS/MODS/ folder.

Conflicts occur when two or more mod pak files contain the same game pak item.

![](PakConflicts.png)

In the above screenshot we can see that 4 mod pak files contain GCENVIRONMENTGLOBALS.GLOBAL.MBIN;
the game will only load one, the changes in the other mod pak files will be ignored.

The mods are prefixed with the MBINC version used to compile the mod mbin in conflict.
The mods are listed in load-order, so the mbin from the last listed mod is the one that will be used by the game.

Double-clicking on the mod name will open a diff view of the game version of the mbin (left) and the clicked mod version of the mbin (right).

---
