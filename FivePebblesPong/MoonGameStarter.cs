using RWCustom;
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
        public float maxControllerGrabDist = 90f;
        public int SearchDelayCounter = 0;
        public int SearchDelay = 600;
        public bool moonMayGrabController = true;
        public bool fadeGame; //if true, game will fade until it is invisible


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

            //special effects (flicker, revealgame)
            float revealGameAlpha = (400 - MoonGameStarter.moonDelayUpdateGame) * (0.5f / 400);
            if (revealGameAlpha < 0f)
                revealGameAlpha = 0f;
            if (this.moonGame != null)
                this.moonGame.imageAlpha = revealGameAlpha * ProjectorFlickerUpdate(fadeGame);

            //start/stop game
            fadeGame = false;
            if (playerMayPlayGame) //player plays
            {
                if (revealGameAlpha > 0.001f && this.moonGame == null)
                    this.moonGame = new Dino(self) { imageAlpha = 0f };

                //wait before allowing game to start
                if (MoonGameStarter.moonDelayUpdateGame > 0) {
                    MoonGameStarter.moonDelayUpdateGame--;
                } else {
                    this.moonGame?.Update(self);
                }

                this.moonGame?.Draw();
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
                this.moonGame?.Update(self, this.moonGame.MoonAI());
                this.moonGame?.Draw();
            }
            else if (this.moonGame != null) //destroy game
            {
                fadeGame = true;
                this.moonGame.Draw();
                if (this.moonGame.imageAlpha <= 0f)
                {
                    this.moonGame.Destroy();
                    this.moonGame = null;
                }
                //release controller if SLOracleBehavior child doesn't do this automatically
                if (self is SLOracleBehaviorNoMark && self.holdingObject != null && self.holdingObject is GameController)
                    self.holdingObject = null;
            }

            //moon grabs controller
            if (SearchDelayCounter < SearchDelay) //search after specified delay
                SearchDelayCounter++;

            if (!moonMayGrabController || this.moonGame != null || self.oracle.health < 1f || self.oracle.stun > 0 || !self.oracle.Consious)
                SearchDelayCounter = 0; //cancel grabbing

            if (MoonGameStarter.moonDelayUpdateGame > 0 || self.holdingObject != null || self.reelInSwarmer != null || !self.State.SpeakingTerms)
                SearchDelayCounter = 0; //cancel grabbing

            if (self is SLOracleBehaviorHasMark &&
                ((self as SLOracleBehaviorHasMark).moveToAndPickUpItem != null || (self as SLOracleBehaviorHasMark).DamagedMode || (self as SLOracleBehaviorHasMark).currentConversation != null))
                SearchDelayCounter = 0; //cancel grabbing

            bool? nullable = GrabObjectType<GameController>(self, maxControllerGrabDist, SearchDelayCounter < SearchDelay);
            if (nullable.HasValue) //success or failed
                SearchDelayCounter = 0; //reset count
        }


        //also copied via dnSpy and made static, you should only need ProjectorFlickerUpdate() returned value
        private static int flickerCounter;
        private static float flickerFade, flickerLastFade, flicker;
        public static float ProjectorFlickerUpdate(bool kill)
        {
            flickerCounter++;
            flickerLastFade = flickerFade;
            flicker = Mathf.Max(0f, flicker - 0.071428575f);
            if (kill) {
                if (flickerFade <= 0f && flickerLastFade <= 0f)
                    return flickerFade;
                flickerFade = Mathf.Max(0f, flickerFade - 0.125f);
            } else {
                flickerFade = 0.7f * Mathf.Lerp(0.95f - 0.7f * flicker * UnityEngine.Random.value, 1f, UnityEngine.Random.value);
                if (UnityEngine.Random.value < 0.033333335f) {
                    flicker = Mathf.Pow(UnityEngine.Random.value, 0.5f);
                } else {
                    flicker = Mathf.Max(0.5f, flicker);
                }
            }
            return flickerFade;
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
