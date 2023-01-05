using BepInEx;
using System;

namespace FivePebblesPong
{
    public static class EnumExt_FPP //dependency: EnumExtender.dll
    {
        //type for spawning controller
        public static AbstractPhysicalObject.AbstractObjectType GameController; //needs to be first in list

        //five pebbles action
        public static SSOracleBehavior.Action Gaming_Gaming;

        //five pebbles movement (controlled by FPGame subclass)
        public static SSOracleBehavior.MovementBehavior PlayGame;

        //moon reaction on controller
        public static SLOracleBehaviorHasMark.MiscItemType GameControllerReaction;
    }


    [BepInPlugin("woutkolkman.fivepebblespong", "Five Pebbles Pong", "0.3.1")] //(GUID, mod name, mod version)
    public class FivePebblesPong : BaseUnityPlugin
    {
        //for accessing logger https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference __me; //WeakReference still allows garbage collection
        public FivePebblesPong() { __me = new WeakReference(this); }
        public static FivePebblesPong ME => __me?.Target as FivePebblesPong;
        public BepInEx.Logging.ManualLogSource Logger_p => Logger;

        public static bool HasEnumExt => (int)EnumExt_FPP.GameController > 0; //returns true after EnumExtender initializes


        //called when mod is loaded, subscribe functions to methods of the game
        public void OnEnable()
        {
            Hooks.Apply();
        }


        //called when game selection is active, add new games here
        public static int amountOfGames = 3; //increase counter when adding more games
        public static FPGame GetNewFPGame(SSOracleBehavior self, int nr) //-1 if no game was selected yet
        {
            if (amountOfGames != 0)
                nr %= amountOfGames;
            switch (nr)
            {
                case 0: return new Pong(self);
                case 1: return new Breakout(self);
                case 2: return new GrabDot(self);
                //add new FPGames here
                default: return null;
            }
        }


        //get player with controller
        public static Player currentPlayer;
        public static Player GetPlayer(OracleBehavior self)
        {
            bool CarriesController(Creature p) { //check if creature is holding a gamecontroller
                for (int i = 0; i < p.grasps.Length; i++)
                    if (p.grasps[i] != null && p.grasps[i].grabbed is GameController)
                        return true;
                return false;
            }

            //check if current player is holding gamecontroller
            if (currentPlayer != null && !CarriesController(currentPlayer))
                currentPlayer = null;

            //cycle through all players
            if (currentPlayer == null && self.oracle?.room?.game?.Players != null) {
                foreach (AbstractCreature ac in self.oracle.room.game.Players)
                    if (ac?.realizedCreature is Player && CarriesController(ac.realizedCreature))
                        currentPlayer = ac.realizedCreature as Player;
            }
            return currentPlayer;
        }
    }
}
