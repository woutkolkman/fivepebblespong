using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    //TODO multiplayer support???

    public static class EnumExt_FPP //dependency: EnumExtender.dll
    {
        //type for spawning controller
        public static AbstractPhysicalObject.AbstractObjectType GameController; //needs to be first in list

        //five pebbles action
        public static SSOracleBehavior.Action Gaming_Gaming;

        //five pebbles movement (controlled by FPGame subclass)
        public static SSOracleBehavior.MovementBehavior PlayGame;

//        public static SSOracleBehavior.Action Gaming_Init; //TODO implement state machine? 
//        public static SSOracleBehavior.Action Gaming_FPWin; //TODO implement
//        public static SSOracleBehavior.Action Gaming_FPLose; //TODO implement
//        public static SSOracleBehavior.SubBehavior.SubBehavID Gaming; //TODO implement
    }


    [BepInPlugin("author.my_mod_id", "FivePebblesPong", "0.1.0")]	// (GUID, mod name, mod version)
    public class FivePebblesPong : BaseUnityPlugin
    {
        //for accessing logger https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference __me; //WeakReference still allows garbage collection
        public FivePebblesPong() { __me = new WeakReference(this); }
        public static FivePebblesPong ME => __me?.Target as FivePebblesPong;
        public BepInEx.Logging.ManualLogSource Logger_p => Logger;

        public static bool HasEnumExt => (int)EnumExt_FPP.GameController > 0; //returns true after EnumExtender initializes
        static SSOracleBehavior.Action PreviousAction; //five pebbles action (from main game) before carrying gamecontroller
        private static FPGame Game;
        
        
        //called when mod is loaded, subscribe functions to methods of the game
        public void OnEnable()
        {
            Hooks.Apply();
            Logger.LogInfo("OnEnable()"); //TODO remove
        }


        //five pebbles update function
        public static void Update(SSOracleBehavior self, bool eu)
        {
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            //wait until slugcat can communicate
            if (self.timeSinceSeenPlayer <= 300 || !self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                return;

            //check if slugcat is holding a gamecontroller
            bool CarriesController = false;
            for (int i = 0; i < self.player.grasps.Length; i++)
                if (self.player.grasps[i] != null && self.player.grasps[i].grabbed is GameController)
                    CarriesController = true;
            //TODO, holding gamecontroller after pebbles asking slugcat to leave will freeze the game

            //toggle action Gaming with PreviousAction
            if (CarriesController)
            {
                if (self.action != EnumExt_FPP.Gaming_Gaming)
                {
                    self.dialogBox.Interrupt(self.Translate("Start"), 10);
                    FivePebblesPong.ME.Logger_p.LogInfo("Start"); //TODO remove
                    PreviousAction = self.action;
                    self.action = EnumExt_FPP.Gaming_Gaming;
                    Game = new Pong(self);
                }
                self.conversation.paused = true;
                self.restartConversationAfterCurrentDialoge = false;
            }
            else if (!CarriesController && self.action == EnumExt_FPP.Gaming_Gaming)
            {
                self.restartConversationAfterCurrentDialoge = true;
                self.dialogBox.Interrupt(self.Translate("Stop"), 10);
                FivePebblesPong.ME.Logger_p.LogInfo("Stop"); //TODO remove
                self.action = PreviousAction;
                Game.Destroy();
                Game = null;
            }

            //code to run in Gaming_Gaming action
            if (self.action == EnumExt_FPP.Gaming_Gaming)
            {
                self.movementBehavior = EnumExt_FPP.PlayGame;
                if (Game != null)
                    Game.Update(self);
            }
        }
    }
}
