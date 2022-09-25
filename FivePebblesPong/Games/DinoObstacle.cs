using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class DinoObstacle : FPGameObject
    {
        public int width, height;
        public float velocityX, velocityY;
        public enum Type
        {
            Cactus
        }


        public DinoObstacle(OracleBehavior self, Type type, float velocityX, float velocityY, Color color, string imageName, bool reloadImg = false) : base(imageName)
        {
            Texture2D img = null;
            this.velocityX = velocityX;
            this.velocityY = velocityY;
            this.width = 20;
            this.height = 22;

            if (type == Type.Cactus)
                img = CreateGamePNGs.DrawCactus(color);

            if (img == null)
                return;

            base.SetImage(self, img, reloadImg);
        }


        ~DinoObstacle() //destructor
        {
            base.Destroy(); //if not done already
        }


        public void Update()
        {
            base.pos.x += velocityX;
            base.pos.y += velocityY;
        }
    }
}
