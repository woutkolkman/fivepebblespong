using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;

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
            
            //ProjectedImage constructor hook for hiding LoadFile()
            On.ProjectedImage.ctor += ProjectedImageCtorHook;
            
            //five pebbles constructor
            On.SSOracleBehavior.ctor += SSOracleBehaviorCtorHook;

            //five pebbles update function
            On.SSOracleBehavior.Update += SSOracleBehaviorUpdateHook;
            
            //five pebbles gravity RuntimeDetour
            Hook SSOracleBehaviorSubBehaviorGravityHook = new Hook(
                typeof(SSOracleBehavior.SubBehavior).GetProperty("Gravity", propFlags).GetGetMethod(),
                typeof(Hooks).GetMethod("SSOracleBehavior_SubBehavior_Gravity_get", myMethodFlags)
            );
            
            //prevent projected lizard from killing player in GrabDot FPGame
            On.Creature.Violence += CreatureViolenceHook;

            //drawing lizard as hologram in GrabDot FPGame
            On.LizardGraphics.AddToContainer += LizardGraphicsAddToContainerHook;
            
            //moon controller reaction
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += SLOracleBehaviorHasMarkMoonConversationAddEventsHook;
            On.SLOracleBehaviorHasMark.TypeOfMiscItem += SLOracleBehaviorHasMarkTypeOfMiscItemHook;

            //moon constructor
            On.SLOracleBehavior.ctor += SLOracleBehaviorCtorHook;

            //moon update functions
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


        public static void Unapply()
        {
            //TODO
        }


        //selects room to place GameController type
        static bool gameControllerPebblesInShelter;
        static bool gameControllerMoonInShelter;
        static void RoomLoadedHook(On.Room.orig_Loaded orig, Room self)
        {
            bool firsttime = self.abstractRoom.firstTimeRealized;
            orig(self);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.game != null && self.roomSettings != null && firsttime)
            {
                if (self.roomSettings.name.Equals("SS_AI") && !gameControllerPebblesInShelter)
                {
                    EntityID newID = self.game.GetNewID(-self.abstractRoom.index);

                    //copy existing coordinate from a random object
                    WorldCoordinate coord = self.GetWorldCoordinate(self.roomSettings.placedObjects[UnityEngine.Random.Range(0, self.roomSettings.placedObjects.Count - 1)].pos);

                    AbstractPhysicalObject ent = new AbstractPhysicalObject(self.world, Enums.GameControllerPebbles, null, coord, newID);
                    self.abstractRoom.AddEntity(ent);
                    FivePebblesPong.ME.Logger_p.LogInfo("RoomLoadedHook, AddEntity at " + coord.SaveToString());
                }
                if (self.roomSettings.name.Equals("SL_MOONTOP") && !gameControllerMoonInShelter)
                {
                    EntityID newID = self.game.GetNewID(-self.abstractRoom.index);
                    WorldCoordinate coord = self.GetWorldCoordinate(new Vector2(1000, 650));
                    AbstractPhysicalObject ent = new AbstractPhysicalObject(self.world, Enums.GameControllerMoon, null, coord, newID);
                    self.abstractRoom.AddEntity(ent);
                    FivePebblesPong.ME.Logger_p.LogInfo("RoomLoadedHook, AddEntity at " + coord.SaveToString());
                }
            }
        }


        //creates GameController object
        static void AbstractPhysicalObjectRealizeHook(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (FivePebblesPong.HasEnumExt && self.realizedObject == null)
            {
                if (self.type == Enums.GameControllerPebbles)
                    self.realizedObject = new GameController(self, new Color(0.44705883f, 0.9019608f, 0.76862746f)); //5P overseer color
                if (self.type == Enums.GameControllerMoon)
                    self.realizedObject = new GameController(self, new Color(1f, 0.8f, 0.3f)); //Moon overseer color
            }
            orig(self);
        }


        //checks if controller already exists
        static void PlayerCtorHook(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.playerState.playerNumber == 0) { //reset
                gameControllerPebblesInShelter = false;
                gameControllerMoonInShelter = false;
            }

            if (self.objectInStomach != null) {
                if (self.objectInStomach.type == Enums.GameControllerPebbles)
                    gameControllerPebblesInShelter = true;
                if (self.objectInStomach.type == Enums.GameControllerMoon)
                    gameControllerMoonInShelter = true;
            }

            for (int i = 0; i < self.room.physicalObjects.Length; i++) {
                for (int j = 0; j < self.room.physicalObjects[i].Count; j++) {
                    if (self.room.physicalObjects[i][j].abstractPhysicalObject.type == Enums.GameControllerPebbles)
                        gameControllerPebblesInShelter = true;
                    if (self.room.physicalObjects[i][j].abstractPhysicalObject.type == Enums.GameControllerMoon)
                        gameControllerMoonInShelter = true;
                }
            }

            if (gameControllerPebblesInShelter || gameControllerMoonInShelter)
                FivePebblesPong.ME.Logger_p.LogInfo("PlayerCtorHook: Prevent controller duplicate");
            //TODO, when a GameController is stored in another shelter, it's not detected and duplication is allowed
        }

        
        //ProjectedImage constructor hook for hiding LoadFile() (function cannot be overridden or hidden for ProjectedImage class)
        static void ProjectedImageCtorHook(On.ProjectedImage.orig_ctor orig, ProjectedImage self, List<string> imageNames, int cycleTime)
        {
            //remove LoadFile() call from constructor, so no .PNG file is required
            if (self is ProjectedImageFromMemory)
            {
                self.imageNames = imageNames;
                self.cycleTime = cycleTime;
                self.setAlpha = new float?(1f);
                return;
            }
            orig(self, imageNames, cycleTime);
        }

        
        //five pebbles constructor
        static void SSOracleBehaviorCtorHook(On.SSOracleBehavior.orig_ctor orig, SSOracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;
            PebblesGameStarter.pebblesNotFullyStartedCounter = 0;
            PebblesGameStarter.pebblesCalibratedProjector = false;
            PebblesGameStarter.controllerInStomachReacted = false;
            PebblesGameStarter.controllerThrownReacted = false;
            PebblesGameStarter.starter = null;
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

            //prevent freezing/locking up the game
            if (self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior ||
                self.action == SSOracleBehavior.Action.ThrowOut_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_SecondThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_KillOnSight ||
                self.action == SSOracleBehavior.Action.General_GiveMark ||
                self.conversation == null) //if there's no conversation, dialog interrupts will freeze the game
            {
                PebblesGameStarter.starter = null; //free PebblesGameStarter object at end of sequence, when player needs to leave
                return;
            }

            //construct/free PebblesGameStarter object when player enters/leaves room
            if (self.player?.room?.roomSettings != null && self.player.room.roomSettings.name.Equals("SS_AI") && PebblesGameStarter.starter == null)
                PebblesGameStarter.starter = new PebblesGameStarter();
            if ((self.player?.room?.roomSettings == null || !self.player.room.roomSettings.name.Equals("SS_AI")) && PebblesGameStarter.starter != null && PebblesGameStarter.starter.state == PebblesGameStarter.State.Stop)
                PebblesGameStarter.starter = null;
            //NOTE checks only singleplayer: "self.player"

            //run state machine for starting/running/stopping games
            PebblesGameStarter.starter?.StateMachine(self);
        }

        
        //five pebbles gravity RuntimeDetour
        static bool previous_SSOracleBehavior_SubBehavior_Gravity;
        public delegate bool orig_Gravity(SSOracleBehavior.SubBehavior self);
        public static bool SSOracleBehavior_SubBehavior_Gravity_get(orig_Gravity orig, SSOracleBehavior.SubBehavior self)
        {
            if (FivePebblesPong.HasEnumExt && PebblesGameStarter.starter != null) { //avoid potential crashes
                if (previous_SSOracleBehavior_SubBehavior_Gravity ^ PebblesGameStarter.starter.gravity)
                    self.oracle.room.PlaySound(PebblesGameStarter.starter.gravity ? SoundID.SS_AI_Exit_Work_Mode : SoundID.Broken_Anti_Gravity_Switch_On, 0f, 1f, 1f);
                previous_SSOracleBehavior_SubBehavior_Gravity = PebblesGameStarter.starter.gravity;
            } else {
                previous_SSOracleBehavior_SubBehavior_Gravity = orig(self); //always true
            }
            return previous_SSOracleBehavior_SubBehavior_Gravity;
        }


        //prevent hologram lizard from killing creatures in GrabDot FPGame
        static void CreatureViolenceHook(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (FivePebblesPong.HasEnumExt && PebblesGameStarter.starter?.game is GrabDot) {
                foreach (AbstractCreature ac in (PebblesGameStarter.starter.game as GrabDot).creatures) {
                    if (ac?.realizedCreature?.mainBodyChunk == source) {
                        damage = 0f;
                        FivePebblesPong.ME.Logger_p.LogInfo("CreatureViolenceHook: Prevent damage");
                    }
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }


        //drawing lizard as hologram in GrabDot FPGame
        static void LizardGraphicsAddToContainerHook(On.LizardGraphics.orig_AddToContainer orig, LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            bool correctCreature = false;
            if (FivePebblesPong.HasEnumExt && PebblesGameStarter.starter?.game is GrabDot)
                foreach (AbstractCreature ac in (PebblesGameStarter.starter.game as GrabDot).creatures)
                    if (ac?.realizedCreature?.graphicsModule == self)
                        correctCreature = true;

            if (FivePebblesPong.HasEnumExt && correctCreature)
            {
                if (newContainer == null)
                    newContainer = rCam.ReturnFContainer("Midground"); //default is "Midground"
                if (sLeaser != null && sLeaser.sprites != null)
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                        sLeaser.sprites[i].shader = rCam.game.rainWorld.Shaders["Hologram"]; //default is "Basic"
                FivePebblesPong.ME.Logger_p.LogInfo("LizardGraphicsAddToContainerHook: Hologram lizard");
            }
            orig(self, sLeaser, rCam, newContainer);
        }

        
        //moon controller reaction
        static void SLOracleBehaviorHasMarkMoonConversationAddEventsHook(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            orig(self);

            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.id == Conversation.ID.Moon_Misc_Item && self.describeItem == Enums.GameControllerReaction)
            {
                self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("It's an electronic device with buttons.<LINE>Where did you find this?"), 0));
                self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("It looks like something that Five Pebbles would like..."), 0));
            }
        }
        static SLOracleBehaviorHasMark.MiscItemType SLOracleBehaviorHasMarkTypeOfMiscItemHook(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
        {
            if (FivePebblesPong.HasEnumExt && testItem is GameController)
                return Enums.GameControllerReaction;
            return orig(self, testItem);
        }


        //moon constructor
        static void SLOracleBehaviorCtorHook(On.SLOracleBehavior.orig_ctor orig, SLOracleBehavior self, Oracle oracle)
        {
            orig(self, oracle);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;
            MoonGameStarter.moonDelayUpdateGame = MoonGameStarter.moonDelayUpdateGameReset;
            MoonGameStarter.starter = null;
        }


        //moon update functions
        static void SLOracleBehaviorUpdateHook(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (!FivePebblesPong.HasEnumExt) //avoid potential crashes
                return;

            if (self.player?.room?.roomSettings != null && self.player.room.roomSettings.name.Equals("SL_AI") && MoonGameStarter.starter == null)
                MoonGameStarter.starter = new MoonGameStarter();
            if ((self.player?.room?.roomSettings == null || !self.player.room.roomSettings.name.Equals("SL_AI")) && MoonGameStarter.starter != null && MoonGameStarter.starter.moonGame == null)
                MoonGameStarter.starter = null;
            //NOTE checks only singleplayer: "self.player"

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


        //RuntimeDetour for moon to move towards item which should be picked up
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
