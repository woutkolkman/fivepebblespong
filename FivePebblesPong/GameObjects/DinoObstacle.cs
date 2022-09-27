﻿using System.Collections.Generic;
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