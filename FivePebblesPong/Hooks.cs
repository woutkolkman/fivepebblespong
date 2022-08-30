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
            //selects room to place FPGameController type
            On.Room.Loaded += RoomLoadedHook;

            //creates FPGameController object
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObjectRealizeHook;

            //five pebbles update function
            On.SSOracleBehavior.Update += SSOracleBehaviorUpdateHook;

            //TODO, SLOracleBehaviorHasMark MoonConversation
        }


        static void RoomLoadedHook(On.Room.orig_Loaded orig, Room self)
        {
            bool firsttime = self.abstractRoom.firstTimeRealized;
            orig(self);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.game != null && self.roomSettings.name.Equals("SS_AI") && firsttime)
            {
                EntityID newID = self.game.GetNewID(-self.abstractRoom.index);
                IntVector2 intVector = self.RandomTile();

                //WorldCoordinate coord = new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1);
                WorldCoordinate coord = new WorldCoordinate(self.abstractRoom.index, 30, 10, -1); //consistent location

                AbstractPhysicalObject ent = new AbstractPhysicalObject(self.world, EnumExt_FPP.FPGameController, null, coord, newID);
                self.abstractRoom.AddEntity(ent);
                FivePebblesPong.ME.Logger_p.LogInfo("RoomLoadedHook, AddEntity at " + coord.SaveToString()); //TODO remove
            }
        }


        static void AbstractPhysicalObjectRealizeHook(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (FivePebblesPong.HasEnumExt && self.realizedObject == null && self.type == EnumExt_FPP.FPGameController)
            {
                self.realizedObject = new FPGameController(self);
            }
            orig(self);
        }


        static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            for (int i = 0; i < self.player.grasps.Length; i++)
            {
                if (self.player.grasps[i] != null && self.player.grasps[i].grabbed is FPGameController)
                {
//                    FivePebblesPong.ME.Logger_p.LogInfo("player holding FPGameController object");
                    FPGameBehavior.Update(eu);
                    return;
                }
            }
            orig(self, eu);
        }
    }
}
