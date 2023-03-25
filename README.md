### Doom part
Thank you for your interest in this monstrosity!  
You'll probably need a keyboard and mouse for this to properly work.  
1. Buy DOOM 1993 on Steam: https://store.steampowered.com/app/2280/DOOM_1993/  
2. Download the latest DOOM Retro version: https://github.com/bradharding/doomretro/releases  
3. Extract the DOOM Retro .zip to "C:\Program Files\doomretro"  
4. Open the "doomretro.cfg" file and set "vid_fullscreen" off  
5. Run it once to check if everything works as expected, select the DOOM.WAD file from DOOM 1993 at startup  
6. Download [OBS Studio](https://obsproject.com/) & follow [this tutorial](https://www.youtube.com/watch?v=18oGqNH9zmo)  
7. Configure OBS to record the DOOM window  
8. In OBS Studio, edit Tools > WebSocket Server Settings and enable the server, disable authentication  

<unfinished TODO>
...


### Decisions
- Bitmaps are not supported directly in the plugin (System.PlatformNotSupportedException). So taking screenshots of other windows using the Win32 API is not possible within the plugin itself.
- Using OBS Studio to capture video, and streaming it at UDP over the local network should be possible. Manually writing an UDP client and transforming every frame would become quite complex.
- There's a [OBS Client plugin](https://github.com/tinodo/obsclient) by tinodo which can receive video via obs-websocket protocol. This client is written in .NET 6.0, which [won't be compatible](https://stackoverflow.com/questions/74344769/how-to-reference-net-6-0-dll-in-net-framework-4-8) with this .NET 4.8 plugin.
- Then I'd figured it would be a great idea to create a separate independent console application in .NET 6.0, which will catch the OBS stream and convert it to Texture2D, which the plugin can read.

<unfinished TODO>
...


---


### Install
Install via the Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2942659714

To manually install, download the most recent .zip from the [releases page](https://github.com/woutkolkman/fivepebblespong/releases) and extract it to "Rain World\RainWorld_Data\StreamingAssets\mods\fivepebblespong".

Next, enable Five Pebbles Pong via the in-game Remix menu.


### Credits
Thanks to the [Rain World Modding Wiki](https://rainworldmodding.miraheze.org/), without this site these mods wouldn't exist.  
Also thanks to tinodo for creating [ObsClient](https://github.com/tinodo/obsclient).


### Description
A Rain World mod that adds a game controller to Five Pebbles' room. If slugcat grabs it, Pebbles invites you to play Pong. Controls are the up and down keys. This mod does not alter existing dialogue or behavior.

<img src="https://github.com/woutkolkman/fivepebblespong/blob/master/gifs/fivepebblespong.gif" height="400">
More games:  
<img src="https://github.com/woutkolkman/fivepebblespong/blob/master/gifs/fivepebblesbreakout.gif" height="400">
<img src="https://github.com/woutkolkman/fivepebblespong/blob/master/gifs/fivepebblesgrabdot.gif" height="400">

Originally ratrat44's idea: https://www.youtube.com/watch?v=X-k5ytvMFQ4

Tested on v1.9.06

Please report any bug, problem or feature request via the [Issues](https://github.com/woutkolkman/fivepebblespong/issues) tab, or on the Steam Workshop page, or message me on the [Rain World Discord](https://discord.gg/rainworld): Maxi Mol#3079
