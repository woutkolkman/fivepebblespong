using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TemplateAddon
{
    class Hooks
    {
        public static void Apply()
        {
            //hook for SSOracleBehavior games
            IDetour detourSSGetNewFPGame = new Hook(
                typeof(FivePebblesPong.Plugin).GetMethod("SSGetNewFPGame", BindingFlags.Static | BindingFlags.Public),
                typeof(Hooks).GetMethod("FivePebblesPongPlugin_SSGetNewFPGame_RuntimeDetour", BindingFlags.Static | BindingFlags.Public)
            );

            //hook for SLOracleBehavior games
            IDetour detourSLGetNewFPGame = new Hook(
                typeof(FivePebblesPong.Plugin).GetMethod("SLGetNewFPGame", BindingFlags.Static | BindingFlags.Public),
                typeof(Hooks).GetMethod("FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour", BindingFlags.Static | BindingFlags.Public)
            );

            //add game to gamelist
            gameNr = FivePebblesPong.Plugin.amountOfGames;
            FivePebblesPong.Plugin.amountOfGames++;
        }


        public static void Unapply()
        {
            //remove game from gamelist
            FivePebblesPong.Plugin.amountOfGames--;
            gameNr = -1;
        }


        static int gameNr = -1; //index of the pearl corresponding to your game
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SSGetNewFPGame_RuntimeDetour(Func<SSOracleBehavior, int, FivePebblesPong.FPGame> orig, SSOracleBehavior ob, int nr)
        {
            bool option = false; //true overrides ALL games
            if (option)
                return new YourGame(ob);

            if (FivePebblesPong.Plugin.amountOfGames != 0 &&         //divide by 0 safety
                gameNr >= 0 &&                                       //game is added to gamelist
                nr % FivePebblesPong.Plugin.amountOfGames == gameNr) //correct pearl is grabbed
                return new YourGame(ob);

            return orig(ob, nr);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FivePebblesPong.FPGame FivePebblesPongPlugin_SLGetNewFPGame_RuntimeDetour(Func<SLOracleBehavior, FivePebblesPong.FPGame> orig, SLOracleBehavior ob)
        {
            //if (false) //true lets you override the existing shoreline game
            //    return new YourGame(ob);
            return orig(ob);
        }
    }
}
