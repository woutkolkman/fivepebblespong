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


        public Dino(SLOracleBehavior self) : base()
        {
            base.minY = 40;
            base.maxY = 620;
            base.minX = 1240;
            base.maxX = 1820;

            this.obstacles = new List<DinoObstacle>();

            this.dino = new DinoPlayer(self, Color.white, "FPP_Dino");
            this.dino.pos = new Vector2(midX + 30 - gameWidth/2, midY); //TODO startpos
            this.dino.ground = midY - dino.height/2;

            this.line = new PongLine(self, true, gameWidth, 1, 0, Color.white, "FPP_Line", reloadImg: true);
            this.line.pos = new Vector2(midX, midY - 1 - dino.height/2);
        }


        ~Dino() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            this.dino?.Destroy(); //TODO remove
            this.line?.Destroy();
            for (int i = 0; i < obstacles.Count; i++)
                obstacles[i]?.Destroy();
            obstacles?.Clear();
        }


        public override void Update(SLOracleBehavior self)
        {
            base.Update(self);

            dino.Update(self.player.input[0].y);

            for (int i = 0; i < obstacles.Count; i++) {
                if (obstacles[i] != null) {
                    obstacles[i].Update();

                    //obstacle left screen
                    if (obstacles[i].pos.x < midX - gameWidth/2)
                    {
                        obstacles[i].Destroy();
                        obstacles.RemoveAt(i);
                    }
                }
            }

            //spawn obstacles
            if (gameCounter % 80 == 0) {
                obstacles.Add(new DinoObstacle(self, DinoObstacle.Type.Cactus, -3f, 0f, Color.white, "FPP_Cactus"));
                obstacles[obstacles.Count-1].pos = new Vector2(midX + gameWidth/2, midY - dino.height/2 + obstacles[obstacles.Count-1].height/2);
            }
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            dino.DrawImage(offset);
            dino.image.setAlpha = imageAlpha;
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
