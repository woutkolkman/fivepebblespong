### Doom part
Thank you for your interest in this monstrosity! You'll probably need a keyboard and mouse to play in-game. It's not guaranteed to work on every PC.
1. Buy DOOM 1993 on Steam & download: https://store.steampowered.com/app/2280/DOOM_1993/
2. Download the latest DOOM Retro version: https://github.com/bradharding/doomretro/releases
3. Extract the DOOM Retro .zip, for example, to "C:\Program Files\doomretro"
4. Open the "doomretro.cfg" file and set "vid_fullscreen" off
5. Run it once to check if everything works as expected, select the DOOM.WAD file from DOOM 1993 at startup
6. Scale down the window for higher FPS, and for visibility in-game
7. Download this folder and its contents at "FivePebblesPong/bin/fivepebblespong" and manually install this mod
8. Any issues? Check BepInEx logs located in "Rain World\BepInEx\LogOutput.log", or enable a console window in "Rain World\BepInEx\config\BepInEx.cfg"


### Decisions
- Bitmaps are not supported directly in the plugin (System.PlatformNotSupportedException). So taking screenshots of other windows using the Win32 API is not possible within the plugin itself.
- Using OBS Studio to capture video, and streaming it at UDP over the local network should be possible. Manually writing an UDP client and transforming every frame would become quite complex.
- There's a [OBS Client plugin](https://github.com/tinodo/obsclient) by tinodo which can receive frames via obs-websocket protocol. This client is written in .NET 6.0, which [won't be compatible](https://stackoverflow.com/questions/74344769/how-to-reference-net-6-0-dll-in-net-framework-4-8) with this .NET 4.8 plugin.
- A separate independent console application in .NET 6.0 was created, which will catch the OBS stream and convert it to PNG, which the plugin can read.
- This console application would send Base64 PNG strings to the plugin. The plugin then needs to convert this string into a Texture2D. This works fairly well, but it is definitely not the most optimised solution.
- No in-game sound, this is probably played by the recorded program anyway.
- Note that with this method, you won't get above 10-15 fps on the projection.
- To increase performance, the Win32 API can be used which would probably be faster at taking screenshots.


---


### Install
Install via the Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2942659714

To manually install, download the most recent .zip from the [releases page](https://github.com/woutkolkman/fivepebblespong/releases) and extract it to "Rain World\RainWorld_Data\StreamingAssets\mods\fivepebblespong".

Next, enable Five Pebbles Pong via the in-game Remix menu.


### Credits
Thanks to [forthbridge and his original Five Pebbles video player](https://github.com/forthbridge/five-pebbles-bad-apple)! The obs_capture branch is just another possible implementation of the same idea.  
Thanks to the [Rain World Modding Wiki](https://rainworldmodding.miraheze.org/), without this site these mods wouldn't exist.


### Description
A Rain World mod that adds a game controller to Five Pebbles' room. If slugcat grabs it, Pebbles invites you to play Pong. Controls are the up and down keys. This mod does not alter existing dialogue or behavior.

<img src="https://github.com/woutkolkman/fivepebblespong/blob/master/gifs/fivepebblespong.gif" height="400">
More games:  
<img src="https://github.com/woutkolkman/fivepebblespong/blob/master/gifs/fivepebblesbreakout.gif" height="400">
<img src="https://github.com/woutkolkman/fivepebblespong/blob/master/gifs/fivepebblesgrabdot.gif" height="400">

Originally ratrat44's idea: https://www.youtube.com/watch?v=X-k5ytvMFQ4

Tested on v1.9.06

Please report any bug, problem or feature request via the [Issues](https://github.com/woutkolkman/fivepebblespong/issues) tab, or on the Steam Workshop page, or message me on the [Rain World Discord](https://discord.gg/rainworld): Maxi Mol#3079
