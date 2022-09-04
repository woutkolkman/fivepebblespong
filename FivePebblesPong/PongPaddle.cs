using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class PongPaddle : FPGameObject
    {
        public int width, height;
        public float movementSpeed;
        public int maxY, minY, maxX, minX; //positions


        public PongPaddle(SSOracleBehavior self, FPGame game, int width, int height, string imageName) : base(imageName)
        {
            this.width = width;
            this.height = height;
            this.movementSpeed = 5f;

            //position boundaries
            this.maxY = game.maxY;
            this.minY = game.minY;
            this.maxX = game.maxX;
            this.minX = game.minX;

            base.SetImage(self, CreateGamePNGs.DrawRectangle(width, height, 2));
        }


        public void Update(int inputX, int inputY)
        {
            //TODO better edge detection

            float newX = pos.x + inputX * movementSpeed;
            float newY = pos.y + inputY * movementSpeed;

            if (newX + (width / 2) < maxX && newX - (width / 2) > minX)
                pos.x = newX;
            if (newY + (height / 2) < maxY && newY - (height / 2) > minY)
                pos.y = newY;

            //only if paddle is spawned at invalid location
            if (minX != maxX)
            {
                if (pos.x - (width / 2) <= minX) pos.x++;
                if (pos.x + (width / 2) >= maxX) pos.x--;
            }
            if (minY != maxY)
            {
                if (pos.y - (height / 2) <= minY) pos.y++;
                if (pos.y + (height / 2) >= maxY) pos.y--;
            }
        }
    }
}
