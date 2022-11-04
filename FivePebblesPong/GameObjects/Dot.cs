using System.Collections.Generic;
using System;
using UnityEngine;

namespace FivePebblesPong
{
    public class Dot : FPGameObject
    {
        public int radius;
        public float alpha = 1f;
        public float fadeAnim = 0f;
        private bool reached = false;


        public Dot(OracleBehavior self, FPGame game, int radius, string imageName, Color? color = null, bool reloadImg = false) : base(imageName)
        {
            this.radius = radius;
            pos = new Vector2(UnityEngine.Random.Range(game.minX, game.maxX), UnityEngine.Random.Range(game.minY, game.maxY));

            Color c = Color.white;
            if (color != null)
                c = (Color)color;

            FivePebblesPong.ME.Logger_p.LogInfo("new Dot pos: " + pos.ToString());
            base.SetImage(self, CreateGamePNGs.DrawCircle(radius, radius, c), reloadImg);
        }


        ~Dot() //destructor
        {
            base.Destroy(); //if not done already
        }


        public bool Update(OracleBehavior self)
        {
            float minDist = float.MaxValue;
            foreach (AbstractCreature c in self.oracle.room.abstractRoom.creatures)
            {
                if (c.realizedCreature is Player)
                {
                    foreach (BodyChunk b in c.realizedCreature.bodyChunks)
                    {
                        float dist = Vector2.Distance(b.pos, this.pos);
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
            }
            if (minDist <= radius)
                reached = true;

            if (reached) {
                if (fadeAnim >= 1f)
                    self.oracle.room.PlaySound(SoundID.Mouse_Light_Flicker, self.oracle.firstChunk);
                if (fadeAnim > 0f) fadeAnim -= 0.08f;
                if (fadeAnim < 0f) fadeAnim = 0f;
            } else {
                if (fadeAnim < 1f) fadeAnim += 0.08f;
                if (fadeAnim > 1f) fadeAnim = 1f;
            }
            image.alpha = alpha * fadeAnim;
            return reached && fadeAnim <= 0f;
        }
    }
}
