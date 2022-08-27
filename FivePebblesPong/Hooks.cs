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

            //TODO, SLOracleBehaviorHasMark MoonConversation
        }


        static void RoomLoadedHook(On.Room.orig_Loaded orig, Room self)
        {
            bool firsttime = self.abstractRoom.firstTimeRealized;
            orig(self);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            FivePebblesPong.ME.Logger_p.LogInfo("RoomLoadedHook, name == " + self.roomSettings.name + ", firstTimeRealized == " + firsttime); //TODO remove

            if (self.game != null && self.roomSettings.name.Equals("SU_A22") && firsttime)
            {
                EntityID newID = self.game.GetNewID(-self.abstractRoom.index);
                IntVector2 intVector = self.RandomTile();

                //WorldCoordinate coord = new WorldCoordinate(self.abstractRoom.index, intVector.x, intVector.y, -1);
                WorldCoordinate coord = new WorldCoordinate(self.abstractRoom.index, 23, 18, -1); //consistent location

                AbstractPhysicalObject ent = new AbstractPhysicalObject(
                    self.world,
                    EnumExt_FPP.FPGameController,
                    null,
                    coord,
                    newID
                );

                self.abstractRoom.AddEntity(ent);

                FivePebblesPong.ME.Logger_p.LogInfo("RoomLoadedHook, entity added at " + coord.SaveToString()); //TODO remove
            }
        }


        static void AbstractPhysicalObjectRealizeHook(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (!FivePebblesPong.HasEnumExt) { //avoid potential crashes
                orig(self);
                return;
            }

            if (self.realizedObject == null && self.type == EnumExt_FPP.FPGameController)
            {
                self.realizedObject = new FPGameController(self);
            }
            orig(self);
        }
    }
}
