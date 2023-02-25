using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class Dino : FPGame
    {
        public DinoPlayer dino;
        public List<DinoObstacle> obstacles;
        public PongLine line;
        public float imageAlpha = 1f;
        public int gameWidth = 250;
        public bool gameStarted, prevGameStarted;
        public int lastCounter;
        public static int highScore;
        public int minObstacleInterval = 18;
        public float obstacleSpawnChance = 0.04f;
        public readonly Color color;
        public float startSpeed = -4f;


        public Dino(OracleBehavior self) : base(self)
        {
            this.obstacles = new List<DinoObstacle>();
            this.color = new Color(0f, 0.89411765f, 1f); //color of bootlabel moon

            this.dino = new DinoPlayer(self, color, "FPP_Dino");
            this.dino.pos = new Vector2(midX + 30 - gameWidth / 2, midY);
            this.dino.ground = midY - this.dino.height / 2;

            this.line = new PongLine(self, true, gameWidth, 1, 0, color, "FPP_Line", reloadImg: true);
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


        public override void Update(OracleBehavior self)
        {
            Update(self, p?.input[0].y ?? 0);
        }
        public void Update(OracleBehavior self, int input)
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
            if (gameCounter > 100 && UnityEngine.Random.value < obstacleSpawnChance && gameCounter - lastCounter >= minObstacleInterval) {
                lastCounter = gameCounter;

                //spawn cactus
                if (gameCounter < 1000 || UnityEngine.Random.value < 0.8f) {
                    obstacles.Add(new DinoObstacle(self, DinoObstacle.Type.Cactus, startSpeed + (-0.0006f * gameCounter), 0f, color, "FPP_Cactus"));
                    obstacles[obstacles.Count - 1].pos = new Vector2(midX + gameWidth / 2, this.line.pos.y + 1 + obstacles[obstacles.Count - 1].height / 2);

                } else { //spawn bird
                    int offsetFromGround = 21;
                    if (UnityEngine.Random.value < 0.20f) offsetFromGround = 11;
                    if (UnityEngine.Random.value < 0.20f) offsetFromGround = 31;
                    obstacles.Add(new DinoObstacle(self, DinoObstacle.Type.Bird, startSpeed + (-0.0007f * gameCounter), UnityEngine.Random.Range(-0.1f, 0.1f), color, "FPP_Bird"));
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


        static bool prevDeadTalk;
        public void MoonBehavior(SLOracleBehavior self)
        {
            //release controller if player grabs neuron
            if (self.protest)
                self.holdingObject = null;

            //release controller once at the moment of player death
            if (self is SLOracleBehaviorHasMark) {
                if ((self as SLOracleBehaviorHasMark).deadTalk && !prevDeadTalk)
                    self.holdingObject = null;
                prevDeadTalk = (self as SLOracleBehaviorHasMark).deadTalk;
            }

            //moon looks at game, else looks at slugcat
            if (gameStarted && !self.protest)
            {
                self.lookPoint = p?.DangerPos ?? self.player?.DangerPos ?? new Vector2(); //moon looks at slugcat instead of controller during game
                if (((self is SLOracleBehaviorNoMark) && Vector2.Distance(self.oracle.firstChunk.pos, (p?.DangerPos ?? self.player?.DangerPos ?? new Vector2())) > 60) ||
                    ((self is SLOracleBehaviorHasMark) &&
                    (self as SLOracleBehaviorHasMark).currentConversation == null && //look at player when talking
                    !(self as SLOracleBehaviorHasMark).playerIsAnnoyingWhenNoConversation &&
                    !(self as SLOracleBehaviorHasMark).playerHoldingNeuronNoConvo &&
                    (self as SLOracleBehaviorHasMark).playerAnnoyingCounter < 20))
                    self.lookPoint = dino.pos;
            }

            //score dialog when player dies
            if (!gameStarted && prevGameStarted) {
                //moon was playing
                if (self.holdingObject != null && self.holdingObject is GameController) {
                    if (self is SLOracleBehaviorHasMark && self.State.SpeakingTerms && self.oracle.health >= 1f)
                        (self as SLOracleBehaviorHasMark).dialogBox.Interrupt(self.Translate(gameCounter + "!"), 10);
                    return;
                }
                //player was playing
                if (gameCounter > 1000 && (self is SLOracleBehaviorHasMark) && self.State.SpeakingTerms && self.oracle.health >= 1f) //minimum score to not make dialog annoying
                    (self as SLOracleBehaviorHasMark).dialogBox.Interrupt(self.Translate("Looks like your score is " + gameCounter + ". " + (gameCounter > highScore ? "Your new highscore!" : "Your highscore is " + highScore + ".")), 10);
                if (gameCounter > highScore)
                    highScore = gameCounter;
            }
        }


        public int MoonAI()
        {
            //if (false) //player controlled
            //    return p?.input[0].y ?? 0;

            if (dino.curAnim == DinoPlayer.Animation.Dead)
                return 0; //game will be exited soon
            if (!gameStarted)
                return 1; //starts game

            foreach (DinoObstacle ob in obstacles)
            {
                float positiveDist = (ob.pos.x - ob.width/2) - (dino.pos.x + (dino.width/2));
                float negativeDist = (ob.pos.x + ob.width/2) - (dino.pos.x - (dino.width/2));
                float minDist = 20f - (ob.velocityX * 3f);
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
