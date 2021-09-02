# Substances, Products, Technologies
![](Tab_GameItems.png)

The displayed names and descriptions used localized strings from the currently selected language.

When you first select this tab for a given game instance it will take several seconds before it displays.
ListBox's can run in virtualizing mode, where only the list items shown on-screen have controls created for them.
However, for non-trivial item displays this can cause significant lag when you scroll as the ListBox
keeps releasing controls for items no longer visible and creating new controls for the items now scrolled into view.
To keep scrolling smooth for these lists virtualization has been turned off;
however, this means that when you first try to view the lists it has to create the item controls for all items in the list - hence the delay.

The search filter uses simple case-insensitive substring searches.

</br>
