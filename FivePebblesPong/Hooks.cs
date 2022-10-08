using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace FivePebblesPong
{
    class Hooks
    {
        static BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
        static BindingFlags myMethodFlags = BindingFlags.Static | BindingFlags.Public;


        public static void Apply()
        {
            //selects room to place GameController type
            On.Room.Loaded += RoomLoadedHook;

            //creates GameController object
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObjectRealizeHook;

            //check if controller already exists
            On.Player.ctor += PlayerCtorHook;

            //five pebbles constructor
            On.SSOracleBehavior.ctor += SSOracleBehaviorCtorHook;

            //five pebbles update function
            On.SSOracleBehavior.Update += SSOracleBehaviorUpdateHook;

            //moon controller reaction
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += SLOracleBehaviorHasMarkMoonConversationAddEventsHook;
            On.SLOracleBehaviorHasMark.TypeOfMiscItem += SLOracleBehaviorHasMarkTypeOfMiscItemHook;

            //big sis moon constructor
            On.SLOracleBehavior.ctor += SLOracleBehaviorCtorHook;

            //big sis moon update functions
            On.SLOracleBehavior.Update += SLOracleBehaviorUpdateHook;
            On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMarkUpdateHook;
            On.SLOracleBehaviorNoMark.Update += SLOracleBehaviorNoMarkUpdateHook;

            //Moon OracleGetToPos RuntimeDetour
            Hook SLOracleBehaviorHasMarkOracleGetToPosHook = new Hook(
                typeof(SLOracleBehaviorHasMark).GetProperty("OracleGetToPos", propFlags).GetGetMethod(),
                typeof(Hooks).GetMethod("SLOracleBehaviorHasMark_OracleGetToPos_get", myMethodFlags)
            );
            Hook SLOracleBehaviorNoMarkOracleGetToPosHook = new Hook(
                typeof(SLOracleBehaviorNoMark).GetProperty("OracleGetToPos", propFlags).GetGetMethod(),
                typeof(Hooks).GetMethod("SLOracleBehaviorNoMark_OracleGetToPos_get", myMethodFlags)
            );
        }


        //selects room to place GameController type
        static bool gameControllerInShelter;
        static void RoomLoadedHook(On.Room.orig_Loaded orig, Room self)
        {
            bool firsttime = self.abstractRoom.firstTimeRealized;
            orig(self);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.game != null && self.roomSettings.name.Equals("SS_AI") && firsttime && !gameControllerInShelter)
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


        //checks if controller already exists
        static void PlayerCtorHook(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.playerState.playerNumber == 0) //reset
                gameControllerInShelter = false;

            if (self.objectInStomach != null && self.objectInStomach.type == EnumExt_FPP.GameController)
                gameControllerInShelter = true;

            for (int i = 0; i < self.room.physicalObjects.Length; i++)
                for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
                    if (self.room.physicalObjects[i][j] is GameController)
                        gameControllerInShelter = true;

            if (gameControllerInShelter)
                FivePebblesPong.ME.Logger_p.LogInfo("gameControllerInShelter");
            //TODO, when a GameController is stored in another shelter, it's not detected and duplication is allowed
        }


        //five pebbles constructor
        static void SSOracleBehaviorCtorHook(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;
            PebblesGameStarter.pebblesNotFullyStartedCounter = 0;
            PebblesGameStarter.starter = null;
            PebblesGameStarter.pebblesCalibratedProjector = false;
            PebblesGameStarter.controllerInStomachReacted = false;
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

            //construct/free PebblesGameStarter object
            if ((CarriesController && self.player.room.roomSettings.name.Equals("SS_AI")) && PebblesGameStarter.starter == null)
                PebblesGameStarter.starter = new PebblesGameStarter();
            if ((!CarriesController || !self.player.room.roomSettings.name.Equals("SS_AI")) && PebblesGameStarter.starter != null && PebblesGameStarter.starter.state == PebblesGameStarter.State.Stop)
                PebblesGameStarter.starter = null;

            //run state machine for starting/running/stopping games
            PebblesGameStarter.starter?.StateMachine(self, CarriesController);
        }


        //moon controller reaction
        static void SLOracleBehaviorHasMarkMoonConversationAddEventsHook(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            orig(self);

            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.id == Conversation.ID.Moon_Misc_Item && self.describeItem == EnumExt_FPP.GameControllerReaction)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("It's an electronic device with buttons. Where did you find this?"), 0));
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("It looks like something that Five Pebbles would like..."), 0));
            }
        }
        static SLOracleBehaviorHasMark.MiscItemType SLOracleBehaviorHasMarkTypeOfMiscItemHook(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
        {
            if (FivePebblesPong.HasEnumExt && testItem is GameController)
                return EnumExt_FPP.GameControllerReaction;
            return orig(self, testItem);
        }


        //big sis moon constructor
        static void SLOracleBehaviorCtorHook(On.SLOracleBehavior.orig_ctor orig, SLOracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;
            MoonGameStarter.moonDelayUpdateGame = MoonGameStarter.moonDelayUpdateGameReset;
            MoonGameStarter.starter = null;
        }


        //big sis moon update functions
        static void SLOracleBehaviorUpdateHook(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.player.room.roomSettings.name.Equals("SL_AI") && MoonGameStarter.starter == null)
                MoonGameStarter.starter = new MoonGameStarter();
            if (!self.player.room.roomSettings.name.Equals("SL_AI") && MoonGameStarter.starter != null && MoonGameStarter.starter.moonGame == null)
                MoonGameStarter.starter = null;

            MoonGameStarter.starter?.Handle(self);
        }


        static void SLOracleBehaviorHasMarkUpdateHook(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
        {
            orig(self, eu);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;
            MoonGameStarter.starter?.moonGame?.MoonBehavior(self);
        }


        static void SLOracleBehaviorNoMarkUpdateHook(On.SLOracleBehaviorNoMark.orig_Update orig, SLOracleBehaviorNoMark self, bool eu)
        {
            orig(self, eu);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;
            MoonGameStarter.starter?.moonGame?.MoonBehavior(self);
        }


        public delegate Vector2 orig_OracleGetToPos_HasMark(SLOracleBehaviorHasMark self);
        public delegate Vector2 orig_OracleGetToPos_NoMark(SLOracleBehaviorNoMark self);
        public static Vector2 SLOracleBehaviorHasMark_OracleGetToPos_get(orig_OracleGetToPos_HasMark orig, SLOracleBehaviorHasMark self)
        {
            if (FivePebblesPong.HasEnumExt && MoonGameStarter.grabItem != null)
                return MoonGameStarter.grabItem.firstChunk.pos;
            return orig(self);
        }
        public static Vector2 SLOracleBehaviorNoMark_OracleGetToPos_get(orig_OracleGetToPos_NoMark orig, SLOracleBehaviorNoMark self)
        {
            if (FivePebblesPong.HasEnumExt && MoonGameStarter.grabItem != null)
                return MoonGameStarter.grabItem.firstChunk.pos;
            return orig(self);
        }
    }
}
