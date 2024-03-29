﻿using System.Collections.Generic;
using System;
using UnityEngine;

namespace FivePebblesPong
{
    public class PongBall : FPGameObject
    {
        public int radius;
        public float movementSpeed;
        public int maxY, minY, maxX, minX; //positions
        public double angle; //radians
        public Vector2 lastWallHit;
        const float CMP = 0.01f; //compare precision
        public float velocityX { get { return (float) (movementSpeed * Math.Cos(angle)); } }
        public float velocityY { get { return (float) (movementSpeed * -Math.Sin(angle)); } }


        public PongBall(OracleBehavior self, FPGame game, int radius, string imageName, Color? color = null, int cycleTime = 15, bool reloadImg = false) : base(imageName)
        {
            this.radius = radius;
            this.movementSpeed = 5.5f;
            this.angle = 0;

            //position boundaries
            this.maxY = game.maxY;
            this.minY = game.minY;
            this.maxX = game.maxX;
            this.minX = game.minX;

            Color c = Color.white;
            if (color != null)
                c = (Color)color;

            List<Texture2D> textures = new List<Texture2D>() {
                CreateGamePNGs.DrawCircle(radius, radius, c), //imageName
                CreateGamePNGs.DrawCircle(radius, 2, c)       //imageName + 1
            };
            base.SetImage(self, textures, cycleTime, reloadImg);
            SetFlashing(false);
        }


        ~PongBall() //destructor
        {
            base.Destroy(); //if not done already
        }


        public bool Update()
        {
            bool hitWall = false;

            //calculate new location
            float newX = pos.x + velocityX;
            float newY = pos.y + velocityY;

            //close gap towards edge of wall
            if (newX - radius < minX) newX = minX + radius;
            if (newX + radius > maxX) newX = maxX - radius;
            if (newY - radius < minY) newY = minY + radius;
            if (newY + radius > maxY) newY = maxY - radius;

            //bounce back at top/bottom wall
            pos.y = newY;
            if (!(newY + radius <= maxY - CMP && newY - radius >= minY + CMP)) {
                lastWallHit = base.pos;
                if (lastWallHit.y + radius > maxY - CMP) lastWallHit.y = maxY;
                if (lastWallHit.y - radius < minY + CMP) lastWallHit.y = minY;
                ReverseYDir();
                hitWall = true;
            }

            //bounce back at left/right wall
            pos.x = newX;
            if (!(newX + radius <= maxX - CMP && newX - radius >= minX + CMP)) {
                lastWallHit = base.pos;
                if (lastWallHit.x + radius > maxX - CMP) lastWallHit.x = maxX;
                if (lastWallHit.x - radius < minX + CMP) lastWallHit.x = minX;
                ReverseXDir();
                hitWall = true;
            }

            angle %= 2 * Math.PI; //prevent overflow
            return hitWall;
        }


        public void ReverseXDir() { angle += (Math.PI - 2 * angle); }
        public void ReverseYDir() { angle *= -1; }
        public void ReverseDir() { angle += Math.PI; }


        public void SetFlashing(bool enabled)
        {
            if (enabled) {
                image.imageNames = new List<string> { imageName, imageName + "1" };
            } else {
                image.imageNames = new List<string> { imageName };
                image.currImg = 0;
            }
        }
    }
}
