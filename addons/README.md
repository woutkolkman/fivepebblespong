### Build
Check the contents of the BUILD.md file for instructions on how to setup your IDE.


### Helpful links
- [Rain World Discord](https://discord.gg/rainworld)
- [Rain World Modding Academy](https://discord.gg/4rqYRexHW3)
- [Rain World Modding Wiki](https://rainworldmodding.miraheze.org)
- [RainDB](https://www.raindb.net)


### Steps to create your game
1. Download the templateaddon folder, and try to build the code as described in BUILD.md.
2. Copy the "templateaddon\TemplateAddon\bin\templateaddon\" folder to your Rain World mods folder at "Steam\steamapps\common\Rain World\RainWorld_Data\StreamingAssets\mods\". The "plugins" folder should contain a .dll file if building was successful.
3. Load the plugin and try to start the mod by selecting your games' pearl. You can use Warp Menu to quickly go to room SS_AI, and with Dev Tools you can teleport around by pressing 'O' and 'V'.

You should see a ball projection bouncing, and you can change the palette by pressing up or down. If everything works you can start programming your game.

4. Replace text like "TemplateAddon" and "YourGame" and "yourname" from all files in the project.
5. The YourGame.cs file contains some examples. More examples are available in the Five Pebbles Pong code.
6. You can upload your plugin in the Remix menu within Rain World, or by using an uploader tool.
7. Every time you update your mod. You should change the version in Plugin.cs and modinfo.json. Updating is done with the same Remix menu button.
