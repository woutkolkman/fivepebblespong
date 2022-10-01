using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class DinoObstacle : FPGameObject
    {
        public int width, height; //hitbox, not imagesize
        public float velocityX, velocityY;
        public Color color { get; }
        public Type type { get; }
        public enum Type
        {
            Cactus,
            Bird
        }


        public DinoObstacle(OracleBehavior self, Type type, float velocityX, float velocityY, Color color, string imageName) : base(imageName)
        {
            this.velocityX = velocityX;
            this.velocityY = velocityY;
            this.color = color;
            this.type = type;

            if (!SetImage(self))
                return;
        }


        ~DinoObstacle() //destructor
        {
            base.Destroy(); //if not done already
        }


        public bool SetImage(OracleBehavior self)
        {
            List<Texture2D> imgs = new List<Texture2D>();

            if (this.type == Type.Cactus)
            {
                imgs.Add(CreateGamePNGs.DrawCactus(this.color));
                this.width = 19;
                this.height = 21;
            }
            else if (this.type == Type.Bird)
            {
                imgs.Add(CreateGamePNGs.DrawBird(this.color, 0));
                imgs.Add(CreateGamePNGs.DrawBird(this.color, 1));
                this.width = 20;
                this.height = 10;
            }

            if (imgs.Count <= 0)
                return false;

            base.SetImage(self, imgs, 10, false);
            return true;
        }


        //returns true if obstacle is hit
        public bool Update(DinoPlayer player)
        {
            base.pos.x += velocityX;
            base.pos.y += velocityY;

            float pHalfW = player.width / 2;
            float pHalfH = player.height / 2;
            Vector2[] coords =
            {
                new Vector2(player.pos.x + pHalfW, player.pos.y + pHalfH),
                new Vector2(player.pos.x - pHalfW, player.pos.y + pHalfH),
                new Vector2(player.pos.x + pHalfW, player.pos.y - pHalfH),
                new Vector2(player.pos.x - pHalfW, player.pos.y - pHalfH)
            };

            float oHalfW = this.width / 2;
            float oHalfH = this.height / 2;
            foreach (Vector2 vect in coords)
                if ((vect.x <= this.pos.x + oHalfW) && (vect.x >= this.pos.x - oHalfW) && (vect.y <= this.pos.y + oHalfH) && (vect.y >= this.pos.y - oHalfH))
                    return true;

            return false;
        }
    }
}
