using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
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
        public double paddleBounceAngle;
        public float velocityX { get { return (float) (movementSpeed * Math.Cos(angle)); } }
        public float velocityY { get { return (float) (movementSpeed * -Math.Sin(angle)); } }


        public PongBall(SSOracleBehavior self, FPGame game, int radius, string imageName) : base(imageName)
        {
            this.radius = radius;
            this.movementSpeed = 5.5f;
            //base.pos = new Vector2(game.midX, game.midY);
            this.angle = 0;
            this.paddleBounceAngle = 1.3;

            //position boundaries
            this.maxY = game.maxY;
            this.minY = game.minY;
            this.maxX = game.maxX;
            this.minX = game.minX;

            base.SetImage(self, CreateGamePNGs.DrawCircle(radius, radius));
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
    }
}
