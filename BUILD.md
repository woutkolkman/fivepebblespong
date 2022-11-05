### Build (on Windows)
Follow this guide: https://rainworldmodding.miraheze.org/wiki/BepInPlugins  
The basic steps are also listed below.

Download and install Visual Studio (Community): https://visualstudio.microsoft.com/

Make sure to select the ".NET desktop development" workload, and at the individual components tab, select ".NET Framework 3.5 development tools".

Clone this repository, or download as ZIP and unzip. Open the project (.sln).

Generate a modified version of Assembly-CSharp.dll using the guide at "Referencing private members": https://rain-world-modding.github.io/pages/using-mods/BepInEx.html  
(download and place GeneratePublicAssembly.dll in "Rain World/BepInEx/patchers", run the game once, the file is placed in the root Rain World folder)

Add references to files as described in the guide above:
- Assembly-CSharp.dll (the file you just generated)
- BepInEx.dll
- HOOKS-Assembly-CSharp.dll
- UnityEngine.dll
- MonoMod.RuntimeDetour.dll
- MonoMod.Utils.dll

You can store these, for example, in a folder "references" next to the folder containing this source code.

You can build the code using the shortcut CTRL + SHIFT + B.


### Contributing
Follow the "standard fork -> clone -> edit -> pull request workflow": https://github.com/firstcontributions/first-contributions

You're free to add games! You can add games by creating a class that inherits FPGame, and adding an object to GetNewFPGame() in FivePebblesPong.cs.  
The mod needs testing for multiplayer. Objects may fall out of sync, but this is unknown. The base game 5P mostly focusses on one player anyway.

Stuff that may need improvements are:
- Better dialogue, something more 5P like.
- Improved ball/paddle collision.
- Multiplayer support?
- Your own improvements (with your explanation and reason).

If your additions need extra files or assets, please propose a system for users to easily add these files to Rain World for your additions to work. Currently graphics are generated via code.
