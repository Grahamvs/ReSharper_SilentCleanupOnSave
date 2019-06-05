**ReSharper_SilentCleanupOnSave**
=============================
Automatically format your code on save
--------------------------------------

This extension automatically runs **ReSharper's Silent Cleanup** command when saving documents.

![Installing ReSharper_SilentCleanupOnSave via Visual Studio Extension Manager](https://i.ibb.co/nD6P5sY/Re-Sharper-Silent-Cleanup-On-Save-install-via-VS-Extension-Manager.png)

In **ReSharper 2018** (and up), you can set the default silent cleanup profile in 4 easy steps:

1. Open ReSharper's "Options", and go to "Code Cleanup".
2. Select the desired profile.
3. Click "Set as default".
4. Click save.

![Set ReSharper's default clean profile](https://i.ibb.co/Q9hdqcH/Re-Sharper-Set-default-clean-profile.png)

**NB:** You can create (and configure) your own custom profile by duplicating an existing profile.

To find out more about ReSharper's Code Cleanup profiles, [click here](https://www.jetbrains.com/help/resharper/Code_Cleanup__Index.html).


**Compatibility:**
-------------
* Only compatible with **Visual Studio 2019** and later (although this might change later).
* Any version of ReSharper that supports either the *"ReSharper.ReSharper_SilentCleanupCode"* or the *"ReSharper_SilentCleanupCode"* command.


**Known Issues:**
-------------
* When saving multiple files, the active document may flicker back and forth between the unsaved tabs and your last active document.
* After ReSharper has formated the document, you need to call save a second time - this is due to Visual Studio's API only triggering **AFTER** the document has already been saved. (Further investigation is required)


**Credits:**
------------
* The icon was created using serval different icons originally make by [Freepik](https://www.freepik.com/) from www.flaticon.com 