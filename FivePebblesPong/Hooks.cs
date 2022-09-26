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

            //moon controller reaction
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += SLOracleBehaviorHasMarkMoonConversationAddEventsHook;
            On.SLOracleBehaviorHasMark.TypeOfMiscItem += SLOracleBehaviorHasMarkTypeOfMiscItemHook;

            //big sis moon constructor
            On.SLOracleBehavior.ctor += SLOracleBehaviorCtorHook;

            //big sis moon update functions
            On.SLOracleBehavior.Update += SLOracleBehaviorUpdateHook;
            On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMarkUpdateHook;
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
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;
            FivePebblesPong.pebblesNotFullyStartedCounter = 0;
            FivePebblesPong.starter = null;
            FivePebblesPong.pebblesCalibratedProjector = false;
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

            //construct/free GameStarter object
            if (CarriesController && FivePebblesPong.starter == null)
                FivePebblesPong.starter = new GameStarter();
            if (!CarriesController && FivePebblesPong.starter != null && FivePebblesPong.starter.state == GameStarter.State.Stop)
                FivePebblesPong.starter = null; //TODO, object is not destructed when player leaves early while carrying controller

            //run state machine for starting/running/stopping games
            FivePebblesPong.starter?.StateMachine(self, CarriesController);
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
                FivePebblesPong.moonControllerReacted = true;
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
            FivePebblesPong.moonControllerReacted = false;
            FivePebblesPong.moonDelayUpdateGame = FivePebblesPong.MOON_DELAY_UPDATE_GAME_RESET;
        }


        //big sis moon update functions
        static void SLOracleBehaviorUpdateHook(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
            const int MIN_X_POS_PLAYER = 1100;

            orig(self, eu);

            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            //check if slugcat is holding a gamecontroller
            bool CarriesController = false;
            for (int i = 0; i < self.player.grasps.Length; i++)
                if (self.player.grasps[i] != null && self.player.grasps[i].grabbed is GameController)
                    CarriesController = true;

            //start/stop game
            if (CarriesController &&
                self.hasNoticedPlayer &&
                self.player.DangerPos.x >= MIN_X_POS_PLAYER && //stop game when leaving
                (FivePebblesPong.moonControllerReacted || !self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark))
            {
                if (FivePebblesPong.moonDelayUpdateGame > 0) {
                    FivePebblesPong.moonDelayUpdateGame--;
                    return;
                }
                //TODO gradually reveal game (setAlpha)

                if (FivePebblesPong.moonGame == null)
                    FivePebblesPong.moonGame = new Dino(self);
                FivePebblesPong.moonGame?.Update(self);
                FivePebblesPong.moonGame?.Draw();
            } else {
                //TODO, object is not destructed when FPGame was being played and player exits main game
                FivePebblesPong.moonGame?.Destroy();
                FivePebblesPong.moonGame = null;
            }
        }


        static void SLOracleBehaviorHasMarkUpdateHook(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
        {
            orig(self, eu);

            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (FivePebblesPong.moonGame == null)
                return;

            //moon looks at game, else looks at slugcat
            if (FivePebblesPong.moonGame.gameStarted && FivePebblesPong.moonGame.gameCounter > 75)
                self.lookPoint = FivePebblesPong.moonGame.dino.pos;

            //score dialog
            if (!FivePebblesPong.moonGame.gameStarted && FivePebblesPong.moonGame.prevGameStarted)
            {
                FivePebblesPong.ME.Logger_p.LogInfo("score: " + FivePebblesPong.moonGame.gameCounter);
                self.dialogBox.Interrupt(self.Translate("Looks like your score is " + FivePebblesPong.moonGame.gameCounter + "!"), 10);
            }
        }
    }
}
