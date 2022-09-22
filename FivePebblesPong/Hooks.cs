using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    class Hooks
    {
        public static void Apply()
        {
            //selects room to place GameController type
            On.Room.Loaded += RoomLoadedHook;

            //creates GameController object
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObjectRealizeHook;

            //five pebbles constructor
            On.SSOracleBehavior.ctor += SSOracleBehaviorCtorHook;

            //five pebbles update function
            On.SSOracleBehavior.Update += SSOracleBehaviorUpdateHook;

            //TODO, SLOracleBehaviorHasMark MoonConversation
        }


        //selects room to place GameController type
        static void RoomLoadedHook(On.Room.orig_Loaded orig, Room self)
        {
            bool firsttime = self.abstractRoom.firstTimeRealized;
            orig(self);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.game != null && self.roomSettings.name.Equals("SS_AI") && firsttime)
            {
                EntityID newID = self.game.GetNewID(-self.abstractRoom.index);

                //copy existing coordinate from a random object
                WorldCoordinate coord = self.GetWorldCoordinate(self.roomSettings.placedObjects[UnityEngine.Random.Range(0, self.roomSettings.placedObjects.Count - 1)].pos);

                AbstractPhysicalObject ent = new AbstractPhysicalObject(self.world, EnumExt_FPP.GameController, null, coord, newID);
                self.abstractRoom.AddEntity(ent);
                FivePebblesPong.ME.Logger_p.LogInfo("RoomLoadedHook, AddEntity at " + coord.SaveToString());
            }
        }


        //creates GameController object
        static void AbstractPhysicalObjectRealizeHook(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (FivePebblesPong.HasEnumExt && self.realizedObject == null && self.type == EnumExt_FPP.GameController)
            {
                self.realizedObject = new GameController(self);
            }
            orig(self);
        }


        //five pebbles constructor
        static void SSOracleBehaviorCtorHook(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);
            FivePebblesPong.starter = new GameStarter();
        }


        //five pebbles update function
        static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

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

            //prevent freezing/locking up the game
            if (self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior ||
                self.action == SSOracleBehavior.Action.ThrowOut_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_SecondThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_KillOnSight)
                return;

            //run state machine for starting/running/stopping games
            FivePebblesPong.starter?.StateMachine(self, CarriesController);
        }
    }
}
