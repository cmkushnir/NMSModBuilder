# Substances, Products, Technologies

**Substances:** METADATA/REALITY/TABLES/NMS_REALITY_GCSUBSTANCETABLE.MBIN
![](Substances.png)

**Products:** METADATA/REALITY/TABLES/NMS_REALITY_GCPRODUCTTABLE.MBIN
![](Products.png)

**Technologies:** METADATA/REALITY/TABLES/NMS_REALITY_GCTECHNOLOGYTABLE.MBIN
![](Technologies.png)

Each of the tabs displays the items from the corresponding mbin.  The lists are grouped by category.
The displayed names and descriptions use localized strings from the currently selected language.
Double-clicking the icon will open the corresponding dds item in the PAK Items tab.

The descriptions may have some of the carriage-return and newline characters compressed out for more compact display.
Use the Language tab to get the description as it is exactly stored.

The lists are drawn using virtualization - only the ui elements to be displayed are generated.
If you scroll through a list with many items you will get periodic lag, this is a result of the .NET WPF control not the application code;
the WPF control is having to repeatedly regenerate the ui elements to display as you scroll.
It is possible to disable the list virtualization; however, it can then take 5+ seconds to generate all the ui elements for the list the first time the tab is selected,
as well as when the filter is changed.

The search filter uses simple case-insensitive substring searches.

---
