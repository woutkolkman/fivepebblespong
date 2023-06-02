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
            //initialize options
            On.RainWorld.OnModsInit += RainWorldOnModsInitHook;

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

            //five pebbles (rot) constructor
            On.MoreSlugcats.SSOracleRotBehavior.ctor += MoreSlugcatsSSOracleRotBehaviorCtorHook;

            //five pebbles (rot) update function
            On.MoreSlugcats.SSOracleRotBehavior.Update += MoreSlugcatsSSOracleRotBehaviorUpdateHook;

            //five pebbles (CL) constructor
            On.MoreSlugcats.CLOracleBehavior.ctor += MoreSlugcatsCLOracleBehaviorCtorHook;

            //five pebbles (CL) update function
            On.MoreSlugcats.CLOracleBehavior.Update += MoreSlugcatsCLOracleBehaviorUpdateHook;
        }


        public static void Unapply()
        {
            //TODO
        }


        //initialize options
        static void RainWorldOnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            MachineConnector.SetRegisteredOI(Plugin.ME.GUID, new Options());
        }


        //selects room to place GameController type
        static bool gameControllerPebblesInShelter;
        static bool gameControllerMoonInShelter;
        static void RoomLoadedHook(On.Room.orig_Loaded orig, Room self)
        {
            bool firsttime = self.abstractRoom.firstTimeRealized;
            orig(self);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;

            if (self.game != null && self.roomSettings != null && firsttime)
            {
                if (self.roomSettings.name.Equals("SS_AI") && !gameControllerPebblesInShelter)
                    //copy existing coordinate from a random object
                    PlaceObject(Enums.GameControllerPebbles, self.roomSettings.placedObjects[UnityEngine.Random.Range(0, self.roomSettings.placedObjects.Count)].pos);

                if (self.roomSettings.name.Equals("DM_AI") && !gameControllerMoonInShelter)
                    //copy existing coordinate from a random object
                    PlaceObject(Enums.GameControllerMoon, self.roomSettings.placedObjects[UnityEngine.Random.Range(0, self.roomSettings.placedObjects.Count)].pos);

                if (self.roomSettings.name.StartsWith("RM_AI") && !gameControllerPebblesInShelter)
                    PlaceObject(Enums.GameControllerPebbles, new Vector2(2740, 1280));

                if (self.roomSettings.name.Equals("SL_MOONTOP") && !gameControllerMoonInShelter)
                    PlaceObject(Enums.GameControllerMoon, new Vector2(1000, 650));

                if (self.roomSettings.name.Equals("CL_AI") && !gameControllerPebblesInShelter)
                    PlaceObject(Enums.GameControllerPebbles, new Vector2(2310, 730));
            }

            void PlaceObject(AbstractPhysicalObject.AbstractObjectType obj, Vector2 location)
            {
                EntityID newID = self.game.GetNewID(-self.abstractRoom.index);
                WorldCoordinate coord = self.GetWorldCoordinate(location);
                AbstractPhysicalObject ent = new AbstractPhysicalObject(self.world, obj, null, coord, newID);
                Plugin.ME.Logger_p.LogInfo("RoomLoadedHook, AddEntity " + obj + " at " + coord.SaveToString());
                self.abstractRoom.AddEntity(ent);
            }
        }


        //creates GameController object
        static void AbstractPhysicalObjectRealizeHook(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (Plugin.HasEnumExt && self.realizedObject == null)
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

            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;

            if (self?.playerState == null || self?.room?.physicalObjects == null) {
                Plugin.ME.Logger_p.LogInfo("PlayerCtorHook: Prevented rare exceptions");
                return;
            }

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
                Plugin.ME.Logger_p.LogInfo("PlayerCtorHook: Prevent controller duplicate");
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
            Plugin.ME.Logger_p.LogInfo("SSOracleBehaviorCtorHook");
            orig(self, oracle);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;
            SSGameStarter.notFullyStartedCounter = 0;
            SSGameStarter.calibratedProjector = false;
            SSGameStarter.controllerInStomachReacted = false;
            SSGameStarter.controllerThrownReacted = false;
            SSGameStarter.starter = null;
            Plugin.currentPlayer = null; //fix for bug where game starts without controller
        }


        //five pebbles update function
        static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;

            if (Options.pacifyPebbles?.Value == true &&
                self?.action == SSOracleBehavior.Action.ThrowOut_KillOnSight &&
                self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
            {
                self.action = Enums.Gaming_Gaming;
                self.currSubBehavior = new SSOracleBehavior.SSSleepoverBehavior(self);
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                self.inActionCounter = 0;
                self.dialogBox?.Interrupt("Never mind...", 0);
                SSGameStarter.notFullyStartedCounter = 0;
                Plugin.ME.Logger_p.LogInfo("SSOracleBehaviorUpdateHook: SlumberParty set");
                return;
            }

            //wait until slugcat can communicate
            if (self.timeSinceSeenPlayer <= 300 || !self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                return;

            //prevent freezing/locking up the game
            if (self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior ||
                self.action == SSOracleBehavior.Action.ThrowOut_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_SecondThrowOut ||
                self.action == SSOracleBehavior.Action.ThrowOut_KillOnSight ||
                self.action == SSOracleBehavior.Action.General_GiveMark)
            {
                SSGameStarter.starter = null; //free SSGameStarter object at end of sequence, when player needs to leave
                return;
            }

            //construct/free SSGameStarter object when player enters/leaves room
            if (self.player?.room?.roomSettings != null && self.player.room.roomSettings.name.EndsWith("_AI") && SSGameStarter.starter == null)
                SSGameStarter.starter = new SSGameStarter();
            if ((self.player?.room?.roomSettings == null || !self.player.room.roomSettings.name.EndsWith("_AI")) && SSGameStarter.starter != null && SSGameStarter.starter.state == SSGameStarter.State.Stop)
                SSGameStarter.starter = null;
            //NOTE checks only singleplayer: "self.player"

            //run state machine for starting/running/stopping games
            SSGameStarter.starter?.StateMachine(self);
        }

        
        //five pebbles gravity RuntimeDetour
        static bool previous_SSOracleBehavior_SubBehavior_Gravity;
        public delegate bool orig_Gravity(SSOracleBehavior.SubBehavior self);
        public static bool SSOracleBehavior_SubBehavior_Gravity_get(orig_Gravity orig, SSOracleBehavior.SubBehavior self)
        {
            if (Plugin.HasEnumExt && SSGameStarter.starter != null) { //avoid potential crashes
                if (previous_SSOracleBehavior_SubBehavior_Gravity ^ SSGameStarter.starter.gravity)
                    self.oracle.room.PlaySound(SSGameStarter.starter.gravity ? SoundID.SS_AI_Exit_Work_Mode : SoundID.Broken_Anti_Gravity_Switch_On, 0f, 1f, 1f);
                previous_SSOracleBehavior_SubBehavior_Gravity = SSGameStarter.starter.gravity;
            } else {
                previous_SSOracleBehavior_SubBehavior_Gravity = orig(self); //always true
            }
            return previous_SSOracleBehavior_SubBehavior_Gravity;
        }


        //prevent hologram lizard from killing creatures in GrabDot FPGame
        static void CreatureViolenceHook(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (Plugin.HasEnumExt && SSGameStarter.starter?.game is GrabDot) {
                foreach (AbstractCreature ac in (SSGameStarter.starter.game as GrabDot).creatures) {
                    if (ac?.realizedCreature?.mainBodyChunk == source) {
                        damage = 0f;
                        Plugin.ME.Logger_p.LogInfo("CreatureViolenceHook: Prevent damage");
                    }
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }


        //drawing lizard as hologram in GrabDot FPGame
        static void LizardGraphicsAddToContainerHook(On.LizardGraphics.orig_AddToContainer orig, LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            bool correctCreature = false;
            if (Plugin.HasEnumExt && SSGameStarter.starter?.game is GrabDot)
                foreach (AbstractCreature ac in (SSGameStarter.starter.game as GrabDot).creatures)
                    if (ac?.realizedCreature?.graphicsModule == self)
                        correctCreature = true;

            if (Plugin.HasEnumExt && correctCreature)
            {
                if (newContainer == null)
                    newContainer = rCam.ReturnFContainer("Midground"); //default is "Midground"
                if (sLeaser != null && sLeaser.sprites != null)
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                        sLeaser.sprites[i].shader = rCam.game.rainWorld.Shaders["Hologram"]; //default is "Basic"
                Plugin.ME.Logger_p.LogInfo("LizardGraphicsAddToContainerHook: Hologram lizard");
            }
            orig(self, sLeaser, rCam, newContainer);
        }


        //moon controller reaction
        static bool gameControllerPebblesShown = false;
        static bool gameControllerMoonShown = false;
        static void SLOracleBehaviorHasMarkMoonConversationAddEventsHook(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            orig(self);

            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;

            if (self.id == Conversation.ID.Moon_Misc_Item) {
                if (self.describeItem == Enums.GameControllerMoonReaction) {
                    gameControllerMoonShown = true;
                    if (gameControllerPebblesShown) {
                        self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("Hey, you found my controller! I was wondering where I left it.."), 0));
                    } else {
                        self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("I haven't seen this thing since my collapse. Where did you find this?"), 0));
                    }
                    self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("It's a miracle it still works."), 0));
                }
                if (self.describeItem == Enums.GameControllerPebblesReaction) {
                    gameControllerPebblesShown = true;
                    if (gameControllerMoonShown) {
                        self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("Another one? I don't remember having two.."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("..."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("The color indicates it's Five Pebbles' property.<LINE>I don't think he likes you stealing his stuff."), 0));
                    } else {
                        self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("It's an electronic device with buttons. Where did you find this?"), 0));
                        self.events.Add(new Conversation.TextEvent(self, 8, self.Translate("The shape looks vaguely familiar to me..."), 0));
                    }
                }
            }
        }
        static SLOracleBehaviorHasMark.MiscItemType SLOracleBehaviorHasMarkTypeOfMiscItemHook(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
        {
            if (Plugin.HasEnumExt && testItem.abstractPhysicalObject.type == Enums.GameControllerMoon)
                return Enums.GameControllerMoonReaction;
            if (Plugin.HasEnumExt && (testItem.abstractPhysicalObject.type == Enums.GameControllerPebbles || testItem is GameController))
                return Enums.GameControllerPebblesReaction;
            return orig(self, testItem);
        }


        //moon constructor
        static void SLOracleBehaviorCtorHook(On.SLOracleBehavior.orig_ctor orig, SLOracleBehavior self, Oracle oracle)
        {
            Plugin.ME.Logger_p.LogInfo("SLOracleBehaviorCtorHook");
            orig(self, oracle);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;
            SLGameStarter.moonDelayUpdateGame = SLGameStarter.moonDelayUpdateGameReset;
            SLGameStarter.moonCalibratedProjector = false;
            SLGameStarter.starter = null;
            Plugin.currentPlayer = null; //fix for bug where game starts without controller
        }


        //moon update functions
        static void SLOracleBehaviorUpdateHook(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;

            if (self.player?.room?.roomSettings != null && self.player.room.roomSettings.name.Equals("SL_AI") && SLGameStarter.starter == null)
                SLGameStarter.starter = new SLGameStarter();
            if ((self.player?.room?.roomSettings == null || !self.player.room.roomSettings.name.Equals("SL_AI")) && SLGameStarter.starter != null && SLGameStarter.starter.game == null)
                SLGameStarter.starter = null;
            //NOTE checks only singleplayer: "self.player"

            SLGameStarter.starter?.StateMachine(self);
        }


        static void SLOracleBehaviorHasMarkUpdateHook(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
        {
            orig(self, eu);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;
            if (SLGameStarter.starter?.game is Dino)
                (SLGameStarter.starter.game as Dino).MoonBehavior(self);
            if (SLGameStarter.starter != null)
                SLGameStarter.DefaultSLOracleBehavior(self);
        }


        static void SLOracleBehaviorNoMarkUpdateHook(On.SLOracleBehaviorNoMark.orig_Update orig, SLOracleBehaviorNoMark self, bool eu)
        {
            orig(self, eu);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;
            if (SLGameStarter.starter?.game is Dino)
                (SLGameStarter.starter.game as Dino).MoonBehavior(self);
            if (SLGameStarter.starter != null)
                SLGameStarter.DefaultSLOracleBehavior(self);
        }


        //RuntimeDetour for moon to move towards item which should be picked up (or pong)
        public static Vector2 SLOracleGetToPosOverride; //if written to during a game, moon will try to move to this position
        public delegate Vector2 orig_OracleGetToPos_HasMark(SLOracleBehaviorHasMark self);
        public delegate Vector2 orig_OracleGetToPos_NoMark(SLOracleBehaviorNoMark self);
        public static Vector2 SLOracleBehaviorHasMark_OracleGetToPos_get(orig_OracleGetToPos_HasMark orig, SLOracleBehaviorHasMark self)
        {
            if (Plugin.HasEnumExt) {
                if (SLGameStarter.grabItem != null) return SLGameStarter.grabItem.firstChunk.pos;
                if (SLGameStarter.starter?.game == null) SLOracleGetToPosOverride = new Vector2();
                if (SLOracleGetToPosOverride != new Vector2()) return SLOracleGetToPosOverride;
            }
            return orig(self);
        }
        public static Vector2 SLOracleBehaviorNoMark_OracleGetToPos_get(orig_OracleGetToPos_NoMark orig, SLOracleBehaviorNoMark self)
        {
            if (Plugin.HasEnumExt) {
                if (SLGameStarter.grabItem != null) return SLGameStarter.grabItem.firstChunk.pos;
                if (SLGameStarter.starter?.game == null) SLOracleGetToPosOverride = new Vector2();
                if (SLOracleGetToPosOverride != new Vector2()) return SLOracleGetToPosOverride;
            }
            return orig(self);
        }


        //five pebbles (rot) constructor
        static void MoreSlugcatsSSOracleRotBehaviorCtorHook(On.MoreSlugcats.SSOracleRotBehavior.orig_ctor orig, MoreSlugcats.SSOracleRotBehavior self, Oracle oracle)
        {
            Plugin.ME.Logger_p.LogInfo("MoreSlugcatsSSOracleRotBehaviorCtorHook");
            orig(self, oracle);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;
            RMGameStarter.starter = null;
            RMGameStarter.startedProjector = false;
            Plugin.currentPlayer = null; //fix for bug where game starts without controller
        }


        //five pebbles (rot) update function
        static void MoreSlugcatsSSOracleRotBehaviorUpdateHook(On.MoreSlugcats.SSOracleRotBehavior.orig_Update orig, MoreSlugcats.SSOracleRotBehavior self, bool eu)
        {
            orig(self, eu);

            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;

            //construct/free RMGameStarter object when player enters/leaves room
            if (self.player?.room?.roomSettings != null && self.player.room.roomSettings.name.StartsWith("RM_AI") && RMGameStarter.starter == null)
                RMGameStarter.starter = new RMGameStarter();
            if ((self.player?.room?.roomSettings == null || !self.player.room.roomSettings.name.StartsWith("RM_AI")) && RMGameStarter.starter != null && RMGameStarter.starter.state == RMGameStarter.State.Stop)
                RMGameStarter.starter = null;
            //NOTE checks only singleplayer: "self.player"

            RMGameStarter.starter?.StateMachine(self);
        }


        //five pebbles (CL) constructor
        static void MoreSlugcatsCLOracleBehaviorCtorHook(On.MoreSlugcats.CLOracleBehavior.orig_ctor orig, MoreSlugcats.CLOracleBehavior self, Oracle oracle)
        {
            Plugin.ME.Logger_p.LogInfo("MoreSlugcatsCLOracleBehaviorCtorHook");
            orig(self, oracle);
            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;
            CLOracleBehaviorReacted = false;
        }


        //five pebbles (CL) update function
        static bool CLOracleBehaviorReacted = false;
        static void MoreSlugcatsCLOracleBehaviorUpdateHook(On.MoreSlugcats.CLOracleBehavior.orig_Update orig, MoreSlugcats.CLOracleBehavior self, bool eu)
        {
            orig(self, eu);

            if (!Plugin.HasEnumExt) //avoid potential crashes
                return;

            var p = Plugin.GetPlayer(self);
            if (p == null || CLOracleBehaviorReacted || self.currentConversation != null || !self.hasNoticedPlayer || self.dialogBox == null || self.dialogBox.ShowingAMessage || self.oracle.health <= 0f)
                return;

            if (Vector2.Distance(self.oracle.bodyChunks[0].pos, p.DangerPos) > 200f)
                return;

            self.dialogBox.NewMessage(self.Translate("...No games..."), 60);
            self.dialogBox.NewMessage(self.Translate("...Sorry..."), 60);
            CLOracleBehaviorReacted = true;
        }
    }
}
