using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class FPGame
    {
        static SSOracleBehavior.Action PreviousAction;


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
                if (self.player.grasps[i] != null && self.player.grasps[i].grabbed is FPGameController)
                    CarriesController = true;

            //toggle action Gaming with PreviousAction
            if (CarriesController && self.action != EnumExt_FPP.Gaming_Gaming)
            {
                self.conversation.paused = true;
                self.restartConversationAfterCurrentDialoge = false;
                self.dialogBox.Interrupt(self.Translate("Start"), 10);
                FivePebblesPong.ME.Logger_p.LogInfo("Start"); //TODO remove
                PreviousAction = self.action;
                self.action = EnumExt_FPP.Gaming_Gaming;
            }
            else if (!CarriesController && self.action == EnumExt_FPP.Gaming_Gaming)
            {
                self.restartConversationAfterCurrentDialoge = true;
                self.dialogBox.Interrupt(self.Translate("Stop"), 10);
                FivePebblesPong.ME.Logger_p.LogInfo("Stop"); //TODO remove
                self.action = PreviousAction;
            }

            //code to run in Gaming_Gaming action
            if (self.action == EnumExt_FPP.Gaming_Gaming)
            {
                self.movementBehavior = EnumExt_FPP.PlayGame;
            }
        }


        public static void Move(SSOracleBehavior self)
        {
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.movementBehavior == EnumExt_FPP.PlayGame)
            {
                //look at player
                self.lookPoint = self.player.DangerPos;

                //move to location of player
                Vector2 vector = self.player.DangerPos;
                self.currentGetTo = vector;
                FivePebblesPong.ME.Logger_p.LogInfo("pebbles vect: " + vector.ToString());
            }
        }
    }
}
