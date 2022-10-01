using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class Dino : FPGame
    {
        public DinoPlayer dino;
        public List<DinoObstacle> obstacles;
        public PongLine line;
        public float imageAlpha = 0.4f;
        public int gameWidth = 250;
        public bool gameStarted, prevGameStarted;
        public int lastCounter;
        public static int highScore;
        public int minObstacleInterval = 25;


        public Dino(SLOracleBehavior self) : base()
        {
            base.minY = 40;
            base.maxY = 620;
            base.minX = 1240;
            base.maxX = 1820;

            this.obstacles = new List<DinoObstacle>();

            this.dino = new DinoPlayer(self, Color.white, "FPP_Dino");
            this.dino.pos = new Vector2(midX + 30 - gameWidth / 2, midY);
            this.dino.ground = midY - this.dino.height / 2;

            this.line = new PongLine(self, true, gameWidth, 1, 0, Color.white, "FPP_Line", reloadImg: true);
            this.line.pos = new Vector2(midX, midY - 1 - this.dino.height / 2);
        }


        ~Dino() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            this.dino?.Destroy();
            this.line?.Destroy();
            for (int i = 0; i < obstacles.Count; i++)
                obstacles[i]?.Destroy();
            obstacles?.Clear();
        }


        public override void Update(SLOracleBehavior self)
        {
            Update(self, self.player.input[0].y);
        }
        public void Update(SLOracleBehavior self, int input)
        {
            base.Update(self);

            prevGameStarted = gameStarted;

            if (input != 0 && gameCounter - lastCounter >= 60)
                gameStarted = true;

            if (!gameStarted) {
                return;
            } else if (gameStarted && !prevGameStarted) { //reset game
                //remove existing obstacles
                for (int i = 0; i < obstacles.Count; i++)
                    obstacles[i]?.Destroy();
                obstacles?.Clear();

                //reset score/ticks
                gameCounter = 0;
                lastCounter = 0;

                dino.SetAnimation(DinoPlayer.Animation.Walking);
            }

            this.dino.Update(input);

            for (int i = 0; i < obstacles.Count; i++) {
                if (obstacles[i] != null) {
                    if (obstacles[i].Update(this.dino)) {
                        gameStarted = false; //obstacle was hit, stop game
                        lastCounter = gameCounter;
                        dino.SetAnimation(DinoPlayer.Animation.Dead);
                    }

                    //obstacle left the screen
                    if (obstacles[i].pos.x < midX - gameWidth / 2)
                    {
                        obstacles[i].Destroy();
                        obstacles.RemoveAt(i);
                    }
                }
            }

            //spawn obstacles
            if (gameCounter > 100 && UnityEngine.Random.value < 0.04f && gameCounter - lastCounter >= minObstacleInterval) {
                lastCounter = gameCounter;

                //spawn cactus
                if (gameCounter < 1000 || UnityEngine.Random.value < 0.8f) {
                    obstacles.Add(new DinoObstacle(self, DinoObstacle.Type.Cactus, -3f - (0.0005f * gameCounter), 0f, Color.white, "FPP_Cactus"));
                    obstacles[obstacles.Count - 1].pos = new Vector2(midX + gameWidth / 2, this.line.pos.y + 1 + obstacles[obstacles.Count - 1].height / 2);

                } else { //spawn bird
                    int offsetFromGround = 21;
                    if (UnityEngine.Random.value < 0.20f) offsetFromGround = 11;
                    if (UnityEngine.Random.value < 0.20f) offsetFromGround = 31;
                    obstacles.Add(new DinoObstacle(self, DinoObstacle.Type.Bird, -3f - (0.0007f * gameCounter), UnityEngine.Random.Range(-0.1f, 0.1f), Color.white, "FPP_Bird"));
                    obstacles[obstacles.Count - 1].pos = new Vector2(midX + gameWidth / 2, this.line.pos.y + offsetFromGround + obstacles[obstacles.Count - 1].height / 2);
                }
            }
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            this.dino.DrawImage(offset);
            this.dino.image.setAlpha = imageAlpha;
            line.DrawImage(offset);
            line.image.setAlpha = imageAlpha;

            for (int i = 0; i < obstacles.Count; i++) {
                if (obstacles[i] == null)
                    continue;
                obstacles[i].DrawImage(offset);
                obstacles[i].image.setAlpha = imageAlpha;
            }
        }


        public void Reload(SLOracleBehavior self)
        {
            //if images get deloaded when player left when moon was playing
            this.dino.image.Destroy();
            this.dino.image = null;
            this.dino.SetImage(self);
            this.line.image.Destroy();
            this.line.image = null;
            this.line.SetImage(self, new Texture2D(1, 1), false); //image gets reloaded
            //TODO some existing obstacle images may become invisible
        }


        public void MoonBehavior(SLOracleBehavior self)
        {
            if (self.protest) //release controller if player grabs neuron
                self.holdingObject = null;

            //moon looks at game, else looks at slugcat
            if (gameStarted && !self.protest)
            {
                if ((self is SLOracleBehaviorNoMark) ||
                    ((self is SLOracleBehaviorHasMark) &&
                    !(self as SLOracleBehaviorHasMark).playerIsAnnoyingWhenNoConversation &&
                    !(self as SLOracleBehaviorHasMark).playerHoldingNeuronNoConvo &&
                    (self as SLOracleBehaviorHasMark).playerAnnoyingCounter < 20))
                    self.lookPoint = dino.pos;
            }

            //score dialog when player dies
            if (!gameStarted && prevGameStarted) {
                //moon was playing, don't start dialogue
                if (self.holdingObject != null && self.holdingObject is GameController)
                    return;

                //player was playing
                if (gameCounter > 1000 && (self is SLOracleBehaviorHasMark)) //minimum score to not make dialog annoying
                    (self as SLOracleBehaviorHasMark).dialogBox.Interrupt(self.Translate("Looks like your score is " + gameCounter + ". " + (gameCounter > highScore ? "Your new highscore!" : "Your highscore is " + highScore + ".")), 10);
                if (gameCounter > highScore)
                    highScore = gameCounter;
            }
        }


        public int MoonAI()
        {
            //if (false) //player controlled
            //    return self.player.input[0].y;

            if (dino.curAnim == DinoPlayer.Animation.Dead)
                return 0; //game will be exited soon
            if (!gameStarted)
                return 1; //starts game

            foreach (DinoObstacle ob in obstacles)
            {
                float positiveDist = (ob.pos.x - ob.width/2) - (dino.pos.x + (dino.width/2));
                float negativeDist = (ob.pos.x + ob.width/2) - (dino.pos.x - (dino.width/2));
                float minDist = 15f + (ob.velocityX * -1);
                if (!(positiveDist <= minDist && negativeDist >= 0))
                    continue;

                float bottom = ob.pos.y - ob.height / 2;
                float top = ob.pos.y + ob.height / 2;
                if (bottom > dino.pos.y)
                    return -1;
                if (bottom < dino.pos.y)
                    return 1;
            }
            return 0;
        }
    }
}
