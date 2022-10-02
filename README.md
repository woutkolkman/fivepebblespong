## Description
This code mod adds a game controller to Five Pebbles' room. If slugcat grabs it, Pebbles invites you to play Pong. Controls are the up and down keys. This mod does not alter existing dialogue or behavior.


## Install
Download and install BOI / BepInEx:

https://rainworldmodding.miraheze.org/wiki/BOI#Installation


Download the file "\FivePebblesPong\bin\Debug\FivePebblesPong.dll" and place it in the /Rain World/Mods folder.


## Build (on Windows)
Follow this guide:

https://rainworldmodding.miraheze.org/wiki/BepInPlugins

The basic steps are also listed below.

Download and install Visual Studio (Community):

https://visualstudio.microsoft.com/

Make sure to select the ".NET desktop development" workload, and at the individual components tab, select ".NET Framework 3.5 development tools".

Clone this repository, or download as ZIP and unzip. Open the project (.sln).

Generate a modified version of Assembly-CSharp.dll using the guide at "Referencing private members":

https://rain-world-modding.github.io/pages/using-mods/BepInEx.html

(download and place GeneratePublicAssembly.dll in "Rain World/BepInEx/patchers", run the game once, the file is placed in the root Rain World folder)

Add references to files as described in the guide above:
	- Assembly-CSharp.dll (the file you just generated)
	- BepInEx.dll
	- HOOKS-Assembly-CSharp.dll
	- UnityEngine.dll
You can store these, for example, in a folder "references" next to the folder containing this source code.

You can build the code using the shortcut CTRL + SHIFT + B.


## Contributing
Follow the "standard fork -> clone -> edit -> pull request workflow":

https://github.com/firstcontributions/first-contributions

You're free to add games! You can add games by creating a class that inherits FPGame, and adding an object to GetNewFPGame() in FivePebblesPong.cs.
The mod needs testing for multiplayer. Maybe stuff falls out of sync, but this is unknown. The base game 5P mostly focusses on one player anyway.

Stuff that may need improvements are:
- Improved AI, more human like or complex.
- Better dialogue, something that 5P could really say.
- Translations? And a way of applying those.
- Maybe better code structure or folder hierarchy.
- Multiplayer support?
- Your own improvements (with your explanation and reason)

If your additions need extra files or assets, please propose a guide for users to easily add these files to Rain World for your additions to work.
