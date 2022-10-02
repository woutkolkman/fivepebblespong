using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    static class MoonGameStarter
    {
        //moongame if controller is taken to moon
        public static Dino moonGame;
        public static bool moonControllerReacted;
        public static int moonDelayUpdateGameReset = 1200;
        public static int moonDelayUpdateGame = moonDelayUpdateGameReset;

        static int minXPosPlayer = 1100;
        static float maxControllerGrabDist = 100f;
        static int SearchDelayCounter = 0;
        static bool moonMayGrabController = true;
        static float prevPlayerX;
        static bool destroyFadeGame;


        public static void Handle(SLOracleBehavior self)
        {
            //check if slugcat is holding a gamecontroller
            bool playerCarriesController = false;
            for (int i = 0; i < self.player.grasps.Length; i++)
                if (self.player.grasps[i] != null && self.player.grasps[i].grabbed is GameController)
                    playerCarriesController = true;

            bool playerMayPlayGame = (
                playerCarriesController && self.hasNoticedPlayer &&
                self.player.DangerPos.x >= minXPosPlayer && //stop game when leaving
                (MoonGameStarter.moonControllerReacted || !self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
            );
            bool moonMayPlayGame = (
                self.holdingObject is GameController &&
                self.player.room.roomSettings.name.Equals("SL_AI") && //memory gets freed if player leaves
                MoonGameStarter.moonDelayUpdateGame <= 0 //so game doesn't start until player has played it at least once
            );

            //reload images if slugcat left screen when moon was playing
            if (MoonGameStarter.moonGame != null && prevPlayerX < minXPosPlayer && self.player.DangerPos.x >= minXPosPlayer)
                MoonGameStarter.moonGame.Reload(self);
            prevPlayerX = self.player.DangerPos.x;

            //special effects (flicker, revealgame)
            float revealGameAlpha = (400 - MoonGameStarter.moonDelayUpdateGame) * (0.5f / 400);
            if (revealGameAlpha < 0f)
                revealGameAlpha = 0f;
            if (MoonGameStarter.moonGame != null)
                MoonGameStarter.moonGame.imageAlpha = revealGameAlpha * ProjectorFlickerUpdate(destroyFadeGame);

            //start/stop game
            destroyFadeGame = false;
            if (playerMayPlayGame) //player plays
            {
                if (revealGameAlpha > 0.001f && MoonGameStarter.moonGame == null)
                    MoonGameStarter.moonGame = new Dino(self) { imageAlpha = 0f };

                //wait before allowing game to start
                if (MoonGameStarter.moonDelayUpdateGame > 0)
                {
                    MoonGameStarter.moonDelayUpdateGame--;
                }
                else
                {
                    MoonGameStarter.moonGame?.Update(self);
                }

                MoonGameStarter.moonGame?.Draw();
                return;
            }
            else if (moonMayPlayGame) //moon plays
            {
                //prevent moon from releasing controller if not game over
                if (self is SLOracleBehaviorHasMark && MoonGameStarter.moonGame != null && MoonGameStarter.moonGame.dino != null && MoonGameStarter.moonGame.dino.curAnim != DinoPlayer.Animation.Dead)
                    (self as SLOracleBehaviorHasMark).describeItemCounter = 0;

                if (MoonGameStarter.moonGame == null)
                    MoonGameStarter.moonGame = new Dino(self) { imageAlpha = 0f };
                MoonGameStarter.moonGame?.Update(self, MoonGameStarter.moonGame.MoonAI());
                MoonGameStarter.moonGame?.Draw();
                return;
            }
            else if (MoonGameStarter.moonGame != null) //destroy game
            {
                //TODO, object is not immediately destructed when FPGame was being played and player exits Rain World
                destroyFadeGame = true;
                MoonGameStarter.moonGame.Draw();
                if (MoonGameStarter.moonGame.imageAlpha <= 0f)
                {
                    MoonGameStarter.moonGame.Destroy();
                    MoonGameStarter.moonGame = null;
                }
            }

            //moon grabs controller
            if (!moonMayGrabController)
                return;

            if ((!MoonGameStarter.moonControllerReacted && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark) ||
                MoonGameStarter.moonDelayUpdateGame > 0 || self.holdingObject != null || self.reelInSwarmer != null ||
                (!self.State.SpeakingTerms && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark))
            {
                SearchDelayCounter = 0; //reset count
                return;
            }

            if (self is SLOracleBehaviorHasMark &&
                ((self as SLOracleBehaviorHasMark).moveToAndPickUpItem != null || (self as SLOracleBehaviorHasMark).DamagedMode || (self as SLOracleBehaviorHasMark).currentConversation != null))
            {
                SearchDelayCounter = 0; //reset count
                return;
            }

            if (SearchDelayCounter < 300) { //don't execute every loop
                SearchDelayCounter++;
                return;
            }

            bool? nullable = GrabObjectType<GameController>(self, maxControllerGrabDist);
            if (nullable.HasValue) //success or failed
                SearchDelayCounter = 0; //reset count
        }


        //also copied via dnSpy and made static
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


        //moon grabs object by type, crawling not (yet) implemented
        public static int moveToItemDelay;
        public static PhysicalObject grabItem;
        public static bool? GrabObjectType<T>(SLOracleBehavior self, float maxDist)
        {
            if (grabItem == null)
            {
                FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, Searching for grabItem");

                //get object from room
                for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++)
                    for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++)
                        if (self.oracle.room.physicalObjects[i][j] is T && self.oracle.room.physicalObjects[i][j].grabbedBy.Count <= 0)
                            grabItem = self.oracle.room.physicalObjects[i][j];

                if (grabItem == null)
                {
                    FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, grabItem not found");
                    return false;
                }
            }

            float dist = Vector2.Distance(grabItem.firstChunk.pos, self.oracle.bodyChunks[0].pos);
            if (dist <= maxDist)
            {
                if (moveToItemDelay == 0)
                    FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, Trying to grab grabItem, distance: " + dist.ToString());

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

                if (moveToItemDelay > 300)
                {
                    FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, Grabbing grabItem canceled (time)");
                    grabItem = null;
                    moveToItemDelay = 0;
                    return false; //failed
                }

                //move/crawl towards object
                //TODO, it's hard to apply hooks for this feature

                return null; //still trying
            }
            FivePebblesPong.ME.Logger_p.LogInfo("GrabObjectType, grabItem distance too large: " + dist.ToString());
            grabItem = null;
            moveToItemDelay = 0;
            return false; //failed
        }
    }
}
