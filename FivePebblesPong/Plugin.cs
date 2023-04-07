using BepInEx;
using System;
using System.Security.Permissions;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace FivePebblesPong
{
    //also edit version in "modinfo.json"
    [BepInPlugin("maxi-mol.fivepebblespong", "Five Pebbles Pong", "1.0.1")] //(GUID, mod name, mod version)
    public class Plugin : BaseUnityPlugin
    {
        //for accessing logger https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference __me; //WeakReference still allows garbage collection
        public Plugin() { __me = new WeakReference(this); }
        public static Plugin ME => __me?.Target as Plugin;
        public BepInEx.Logging.ManualLogSource Logger_p => Logger;

        public static bool HasEnumExt => (int)Enums.GameControllerPebbles > 0; //returns true after EnumExtender initializes
        private static bool IsEnabled = false;


        //called when mod is loaded, subscribe functions to methods of the game
        public void OnEnable()
        {
            if (IsEnabled) return;
            IsEnabled = true;

            Enums.RegisterValues();
            Hooks.Apply();

            Plugin.ME.Logger_p.LogInfo("OnEnable called");
        }


        //called when mod is unloaded
        public void OnDisable()
        {
            if (!IsEnabled) return;
            IsEnabled = false;

            Enums.UnregisterValues();
            Hooks.Unapply();

            Plugin.ME.Logger_p.LogInfo("OnDisable called");
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
        public static Player currentPlayer; //NOTE, currentPlayer might not reset to null if exiting/restarting game while playing an FPGame
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
