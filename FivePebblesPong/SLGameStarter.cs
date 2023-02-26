using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class SLGameStarter
    {
        public static SLGameStarter starter; //object gets created when player is in moons room
        public static int moonDelayUpdateGameReset = 1200;
        public static int moonDelayUpdateGame = moonDelayUpdateGameReset;

        public FPGame game;
        public int minXPosPlayer = 1100;
        public float maxControllerGrabDist = 92f;
        public int searchDelayCounter = 0;
        public int searchDelay = 600;
        public bool moonMayGrabController = true;
        public static bool lastPlayedReacted = false;

        //for state machine when moon has heart restored
        public enum State
        {
            Stop,
            StartDialog,
            Started,
            StopDialog
        }
        public State state { get; set; }
        public State statePreviousRun = State.Stop;
        public SLOracleBehavior.MovementBehavior previousMovementBehavior; //movementbehavior (from main game) before starting game


        //for ShowMediaMovementBehavior
        public static bool moonCalibratedProjector;
        public ShowMediaMovementBehavior calibrate = new ShowMediaMovementBehavior();


        public SLGameStarter() { }
        ~SLGameStarter()
        {
            this.game?.Destroy();
        }


        public void StateMachine(SLOracleBehavior self)
        {
            if (self.oracle.room.game.IsMoonHeartActive()) {
                HeartHandle(self);
            } else {
                NoHeartHandle(self);
            }
        }


        public void HeartHandle(SLOracleBehavior self)
        {
            //check if slugcat is holding a gamecontroller
            Player p = FivePebblesPong.GetPlayer(self);

            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.Stop:
                    //no player carries controller
                    if (p?.room?.roomSettings == null)
                        break;

                    //player not in room
                    if (!p.room.roomSettings.name.Equals("SL_AI") || p.DangerPos.x < minXPosPlayer)
                        break;

                    //conversation active
                    if (self is SLOracleBehaviorHasMark && (self as SLOracleBehaviorHasMark).currentConversation != null)
                        break;

                    //moon not healthy
                    if (self.oracle.health < 1f || self.oracle.stun > 0 || !self.oracle.Consious || (self is SLOracleBehaviorHasMark && (self as SLOracleBehaviorHasMark).DamagedMode))
                        break;

                    //moon busy or doesn't want to talk
                    if (self.holdingObject != null || self.reelInSwarmer != null || !self.State.SpeakingTerms || 
                        (self is SLOracleBehaviorHasMark && ((self as SLOracleBehaviorHasMark).moveToAndPickUpItem != null)))
                        break;

                    //hasn't seen player yet
                    if (self.player == null || !self.hasNoticedPlayer || 
                        self.movementBehavior == SLOracleBehavior.MovementBehavior.Meditate || self.movementBehavior == SLOracleBehavior.MovementBehavior.ShowMedia)
                        break;

                    //didn't speak to player yet
                    if (self is SLOracleBehaviorHasMark && (self as SLOracleBehaviorHasMark).sayHelloDelay != 0)
                        break;

                    state = State.StartDialog;
                    break;

                //======================================================
                case State.StartDialog:
                    if (statePreviousRun != state)
                    {
                        self.dialogBox.Interrupt(self.Translate("Sure, I'll play a game with you!"), 10);
                        previousMovementBehavior = self.movementBehavior;
                        FivePebblesPong.ME.Logger_p.LogInfo("Save " + nameof(self.movementBehavior) + ": " + self.movementBehavior.ToString());
                    }
                    if (p == null)
                        state = State.StopDialog;
                    if (!self.dialogBox.ShowingAMessage) //dialog finished
                        state = State.Started;
                    break;

                //======================================================
                case State.Started:
                    if (self.player.slugcatStats.name.ToString().Equals("Rivulet") && !lastPlayedReacted) {
                        lastPlayedReacted = true;
                        self.dialogBox.Interrupt(self.Translate("It's been a while since I last played this one..."), 10);
                    }

                    if (game == null)
                        game = new Pong(self);

                    self.movementBehavior = Enums.SLPlayGame;

                    game?.Update(self);
                    game?.Draw();

                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.Equals("SL_AI"))
                        state = State.StopDialog;
                    break;

                //======================================================
                case State.StopDialog:
                    if (state != statePreviousRun)
                    {
                        self.movementBehavior = previousMovementBehavior;
                        FivePebblesPong.ME.Logger_p.LogInfo("Restore " + nameof(self.movementBehavior) + ": " + self.movementBehavior.ToString());
                        game?.Destroy();
                        game = null;

                        if (statePreviousRun == State.StartDialog)
                        {
                            switch (UnityEngine.Random.Range(0, 3))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                                case 1: self.dialogBox.Interrupt(self.Translate("Ah, don't want to?"), 10); break;
                                case 2: self.dialogBox.Interrupt(self.Translate("No obligations."), 10); break;
                            }
                        }
                        else
                        {
                            switch (UnityEngine.Random.Range(0, 3))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("You're welcome to play again!"), 10); break;
                                case 1: self.dialogBox.Interrupt(self.Translate("That was fun. Unfortunately I don't have many other games currently."), 10); break;
                                case 2: self.dialogBox.Interrupt(self.Translate("Thank you for playing!"), 10); break;
                            }
                        }
                    }

                    if (!self.dialogBox.ShowingAMessage)
                        state = State.Stop;
                    break;
            }
            statePreviousRun = stateBeforeRun;
        }


        public void NoHeartHandle(SLOracleBehavior self)
        {
            //check if slugcat is holding a gamecontroller
            Player p = FivePebblesPong.GetPlayer(self);

            bool playerMayPlayGame = (
                self.hasNoticedPlayer &&
                p?.room?.roomSettings != null && //player carries controller
                p.room.roomSettings.name.Equals("SL_AI") &&
                p.DangerPos.x >= minXPosPlayer //stop game when leaving
            );
            bool moonMayPlayGame = (
                self.holdingObject is GameController &&
                self.player?.room?.roomSettings != null &&
                self.player.room.roomSettings.name.Equals("SL_AI") && //memory gets freed if player leaves
                SLGameStarter.moonDelayUpdateGame <= 0 //so game doesn't start until player has played it at least once
            );
            //NOTE checks only singleplayer: "self.player"

            //start/stop game
            if (playerMayPlayGame) //player plays
            {
                if (SLGameStarter.moonDelayUpdateGame < 400 && this.game == null)
                    this.game = new Dino(self);

                //calibrate projector animation
                if (!SLGameStarter.moonCalibratedProjector && game != null)
                {
                    //run animation, true ==> target location reached, "projector" is calibrated
                    if (calibrate.Update(self, new Vector2(game.midX, game.midY), SLGameStarter.moonDelayUpdateGame <= 0))
                        SLGameStarter.moonCalibratedProjector = true;
                }

                //wait before allowing game to start
                if (SLGameStarter.moonDelayUpdateGame > 0) {
                    SLGameStarter.moonDelayUpdateGame--;
                } else if (SLGameStarter.moonCalibratedProjector) {
                    this.game?.Update(self);
                }

                game?.Draw(calibrate.showMediaPos - new Vector2(game.midX, game.midY));
            }
            else if (moonMayPlayGame) //moon plays
            {
                if (this.game is Dino && (this.game as Dino).dino != null)
                {
                    //prevent moon from auto releasing controller if not game over
                    if (self is SLOracleBehaviorHasMark && (this.game as Dino).dino.curAnim != DinoPlayer.Animation.Dead)
                        (self as SLOracleBehaviorHasMark).describeItemCounter = 0;
                    //release controller if SLOracleBehavior child doesn't do this automatically
                    if (self is SLOracleBehaviorNoMark && (this.game as Dino).dino.curAnim == DinoPlayer.Animation.Dead)
                        self.holdingObject = null;
                }

                if (this.game == null)
                    this.game = new Dino(self) { imageAlpha = 0f };
                (this.game as Dino)?.Update(self, (
                    (self is SLOracleBehaviorHasMark && (self as SLOracleBehaviorHasMark).currentConversation != null)
                    ? 0 //when moon is speaking, don't control game
                    : (this.game as Dino).MoonAI()
                ));
                this.game?.Draw();
            }
            else if (this.game != null) //destroy game
            {
                this.game.Destroy();
                this.game = null;

                //release controller if SLOracleBehavior child doesn't do this automatically
                if (self is SLOracleBehaviorNoMark && self.holdingObject != null && self.holdingObject is GameController)
                    self.holdingObject = null;
            }

            //moon grabs controller
            if (searchDelayCounter < searchDelay) //search after specified delay
                searchDelayCounter++;

            if (!moonMayGrabController || this.game != null || self.oracle.health < 1f || self.oracle.stun > 0 || !self.oracle.Consious)
                searchDelayCounter = 0; //cancel grabbing

            if (SLGameStarter.moonDelayUpdateGame > 0 || self.holdingObject != null || self.reelInSwarmer != null || !self.State.SpeakingTerms)
                searchDelayCounter = 0; //cancel grabbing

            if (self is SLOracleBehaviorHasMark &&
                ((self as SLOracleBehaviorHasMark).moveToAndPickUpItem != null || (self as SLOracleBehaviorHasMark).DamagedMode || (self as SLOracleBehaviorHasMark).currentConversation != null))
                searchDelayCounter = 0; //cancel grabbing

            bool? nullable = GrabObjectType<GameController>(self, maxControllerGrabDist, searchDelayCounter < searchDelay);
            if (nullable.HasValue) //success or failed
                searchDelayCounter = 0; //reset count
        }


        //moon grabs closest object by type
        //dependent on SLOracleBehaviorHasMark_OracleGetToPos_get & SLOracleBehaviorNoMark_OracleGetToPos_get
        public static int moveToItemDelay;
        public static PhysicalObject grabItem; //if not null, moon moves to this position
        public static bool? GrabObjectType<T>(SLOracleBehavior self, float maxDist, bool cancel = false, int timeout = 300)
        {
            if (cancel)
            {
                if (grabItem != null)
                    FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, Grabbing " + typeof(T).Name + " was canceled");
                grabItem = null;
                moveToItemDelay = 0;
                return null; //try again later
            }

            if (grabItem == null)
            {
                FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, Searching for " + typeof(T).Name);

                //get object from room
                float closest = float.MaxValue;
                for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++) {
                    for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++) {
                        if (self.oracle.room.physicalObjects[i][j] is T && self.oracle.room.physicalObjects[i][j].grabbedBy.Count <= 0)
                        {
                            float newDist = Vector2.Distance(self.oracle.room.physicalObjects[i][j].firstChunk.pos, self.oracle.bodyChunks[0].pos);
                            if (newDist < closest) {
                                closest = newDist;
                                grabItem = self.oracle.room.physicalObjects[i][j];
                            }
                        }
                    }
                }

                if (grabItem == null)
                {
                    FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, " + typeof(T).Name + " not found");
                    return false; //failed
                }
            }

            if (grabItem.grabbedBy.Count > 0)
            {
                FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, " + typeof(T).Name + " was grabbed by another");
                grabItem = null;
                moveToItemDelay = 0;
                return false; //failed
            }

            float dist = Vector2.Distance(grabItem.firstChunk.pos, self.oracle.bodyChunks[0].pos);
            if (dist <= maxDist)
            {
                if (moveToItemDelay == 0)
                    FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, Trying to grab " + typeof(T).Name + ", distance: " + dist.ToString());

                moveToItemDelay++;

                //grab object if close enough
                if ((moveToItemDelay > 40 && Custom.DistLess(grabItem.firstChunk.pos, self.oracle.firstChunk.pos, 40f)) ||
                    (moveToItemDelay < 20 && !Custom.DistLess(grabItem.firstChunk.lastPos, grabItem.firstChunk.pos, 5f) && Custom.DistLess(grabItem.firstChunk.pos, self.oracle.firstChunk.pos, 20f)))
                {
                    self.holdingObject = grabItem;
                    if (self.holdingObject.graphicsModule != null)
                        self.holdingObject.graphicsModule.BringSpritesToFront();
                    if (self.holdingObject is IDrawable)
                        for (int i = 0; i < self.oracle.abstractPhysicalObject.world.game.cameras.Length; i++)
                            self.oracle.abstractPhysicalObject.world.game.cameras[i].MoveObjectToContainer(self.holdingObject as IDrawable, null);

                    grabItem = null;
                    moveToItemDelay = 0;
                    return true; //success
                }

                if (moveToItemDelay > timeout)
                {
                    FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, Grabbing " + typeof(T).Name + " canceled (time)");
                    grabItem = null;
                    moveToItemDelay = 0;
                    return false; //failed
                }

                //moves/crawls towards object via RuntimeDetour, if grabItem != null

                return null; //still trying
            }
            FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, " + typeof(T).Name + " distance too large: " + dist.ToString());
            grabItem = null;
            moveToItemDelay = 0;
            return false; //failed
        }


        public static Vector2 moonLookPoint;
        static bool prevDeadTalk;
        public static bool forceFlightMode;
        public static void DefaultSLOracleBehavior(SLOracleBehavior self)
        {
            //overwrite moon look position
            if (moonLookPoint != new Vector2())
                self.lookPoint = moonLookPoint;
            moonLookPoint = new Vector2();

            //overwrite if moon may sit or not
            if (forceFlightMode) {
                self.forceFlightMode = true;
                self.timeOutOfSitZone = 50;
            }
            forceFlightMode = false;

            //release controller if player grabs neuron
            if (self.protest && self.holdingObject is GameController)
                self.holdingObject = null;

            //release controller once at the moment of player death (when dialog starts)
            if (self is SLOracleBehaviorHasMark) {
                if ((self as SLOracleBehaviorHasMark).deadTalk && !prevDeadTalk)
                    if (self.holdingObject is GameController)
                        self.holdingObject = null;
                prevDeadTalk = (self as SLOracleBehaviorHasMark).deadTalk;
            }
        }
    }
}
