﻿using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class MoonGameStarter
    {
        public static MoonGameStarter starter; //object gets created when player is in moons room
        public static int moonDelayUpdateGameReset = 1200;
        public static int moonDelayUpdateGame = moonDelayUpdateGameReset;

        public Dino moonGame;
        public int minXPosPlayer = 1100;
        public float maxControllerGrabDist = 92f;
        public int searchDelayCounter = 0;
        public int searchDelay = 600;
        public bool moonMayGrabController = true;


        //for ShowMediaMovementBehavior
        public static bool moonCalibratedProjector;
        public ShowMediaMovementBehavior calibrate = new ShowMediaMovementBehavior();


        public MoonGameStarter() { }
        ~MoonGameStarter()
        {
            this.moonGame?.Destroy();
        }


        public void Handle(SLOracleBehavior self)
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
                MoonGameStarter.moonDelayUpdateGame <= 0 //so game doesn't start until player has played it at least once
            );
            //NOTE checks only singleplayer: "self.player"

            //start/stop game
            if (playerMayPlayGame) //player plays
            {
                if (MoonGameStarter.moonDelayUpdateGame < 400 && this.moonGame == null)
                    this.moonGame = new Dino(self);

                //calibrate projector animation
                if (!MoonGameStarter.moonCalibratedProjector && moonGame != null)
                {
                    //run animation, true ==> target location reached, "projector" is calibrated
                    if (calibrate.Update(self, new Vector2(moonGame.midX, moonGame.midY), MoonGameStarter.moonDelayUpdateGame <= 0))
                        MoonGameStarter.moonCalibratedProjector = true;
                }

                //wait before allowing game to start
                if (MoonGameStarter.moonDelayUpdateGame > 0) {
                    MoonGameStarter.moonDelayUpdateGame--;
                } else if (MoonGameStarter.moonCalibratedProjector) {
                    this.moonGame?.Update(self);
                }

                moonGame?.Draw(calibrate.showMediaPos - new Vector2(moonGame.midX, moonGame.midY));
            }
            else if (moonMayPlayGame) //moon plays
            {
                if (this.moonGame != null && this.moonGame.dino != null)
                {
                    //prevent moon from auto releasing controller if not game over
                    if (self is SLOracleBehaviorHasMark && this.moonGame.dino.curAnim != DinoPlayer.Animation.Dead)
                        (self as SLOracleBehaviorHasMark).describeItemCounter = 0;
                    //release controller if SLOracleBehavior child doesn't do this automatically
                    if (self is SLOracleBehaviorNoMark && this.moonGame.dino.curAnim == DinoPlayer.Animation.Dead)
                        self.holdingObject = null;
                }

                if (this.moonGame == null)
                    this.moonGame = new Dino(self) { imageAlpha = 0f };
                this.moonGame?.Update(self, (
                    (self is SLOracleBehaviorHasMark && (self as SLOracleBehaviorHasMark).currentConversation != null) 
                    ? 0 //when moon is speaking, don't control game
                    : this.moonGame.MoonAI()
                ));
                this.moonGame?.Draw();
            }
            else if (this.moonGame != null) //destroy game
            {
                this.moonGame.Destroy();
                this.moonGame = null;
                
                //release controller if SLOracleBehavior child doesn't do this automatically
                if (self is SLOracleBehaviorNoMark && self.holdingObject != null && self.holdingObject is GameController)
                    self.holdingObject = null;
            }

            //moon grabs controller
            if (searchDelayCounter < searchDelay) //search after specified delay
                searchDelayCounter++;

            if (!moonMayGrabController || this.moonGame != null || self.oracle.health < 1f || self.oracle.stun > 0 || !self.oracle.Consious)
                searchDelayCounter = 0; //cancel grabbing

            if (MoonGameStarter.moonDelayUpdateGame > 0 || self.holdingObject != null || self.reelInSwarmer != null || !self.State.SpeakingTerms)
                searchDelayCounter = 0; //cancel grabbing

            if (self is SLOracleBehaviorHasMark &&
                ((self as SLOracleBehaviorHasMark).moveToAndPickUpItem != null || (self as SLOracleBehaviorHasMark).DamagedMode || (self as SLOracleBehaviorHasMark).currentConversation != null))
                searchDelayCounter = 0; //cancel grabbing

            bool? nullable = GrabObjectType<GameController>(self, maxControllerGrabDist, searchDelayCounter < searchDelay);
            if (nullable.HasValue) //success or failed
                searchDelayCounter = 0; //reset count
        }


        //moon grabs closest object by type
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
    }
}
