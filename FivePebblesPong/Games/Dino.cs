﻿using System.Collections.Generic;
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
        public int minObstacleInterval = 25;


        public Dino(SLOracleBehavior self) : base()
        {
            base.minY = 40;
            base.maxY = 620;
            base.minX = 1240;
            base.maxX = 1820;

            this.obstacles = new List<DinoObstacle>();

            this.dino = new DinoPlayer(self, Color.white, "FPP_Dino");
            this.dino.pos = new Vector2(midX + 30 - gameWidth/2, midY);
            this.dino.ground = midY - this.dino.height/2;

            this.line = new PongLine(self, true, gameWidth, 1, 0, Color.white, "FPP_Line", reloadImg: true);
            this.line.pos = new Vector2(midX, midY - 1 - this.dino.height/2);
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
            base.Update(self);

            prevGameStarted = gameStarted;

            if (self.player.input[0].y != 0 && gameCounter - lastCounter >= 60)
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
            }

            this.dino.Update(self.player.input[0].y);

            for (int i = 0; i < obstacles.Count; i++) {
                if (obstacles[i] != null) {
                    if (obstacles[i].Update(this.dino)) {
                        gameStarted = false; //obstacle was hit, stop game
                        lastCounter = gameCounter;

                    }

                    //obstacle left the screen
                    if (obstacles[i].pos.x < midX - gameWidth/2)
                    {
                        obstacles[i].Destroy();
                        obstacles.RemoveAt(i);
                    }
                }
            }

            //spawn obstacles
            if (gameCounter > 100 && UnityEngine.Random.value < 0.04f && gameCounter - lastCounter >= minObstacleInterval) {
                lastCounter = gameCounter;
                obstacles.Add(new DinoObstacle(self, DinoObstacle.Type.Cactus, -3f - (0.0005f*gameCounter), 0f, Color.white, "FPP_Cactus"));
                obstacles[obstacles.Count-1].pos = new Vector2(midX + gameWidth/2, midY - this.dino.height/2 + obstacles[obstacles.Count-1].height/2);
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
    }
}