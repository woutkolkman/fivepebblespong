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
        public float ballVelX;
        public float ballVelY;
        public float movementSpeed;
        public int maxY, minY, maxX, minX; //positions
        public double angle; //radians
        public Vector2 lastWallHit;
        const float CMP = 0.01f; //compare precision


        public PongBall(SSOracleBehavior self, FPGame game, int radius, string imageName) : base(imageName)
        {
            this.radius = radius;
            this.movementSpeed = 5f; //TODO gradually increase
            base.pos = new Vector2(game.midX, game.midY);
            this.angle = 6.0;

            //position boundaries
            this.maxY = game.maxY;
            this.minY = game.minY;
            this.maxX = game.maxX;
            this.minX = game.minX;

            base.SetImage(self, CreateGamePNGs.DrawCircle(radius, radius));
        }


        public void Update()
        {
            //calculate new velocity and location
            ballVelX = (float) (movementSpeed * Math.Cos(angle));
            ballVelY = (float) (movementSpeed * -Math.Sin(angle));
            float newX = pos.x + ballVelX;
            float newY = pos.y + ballVelY;

            //close gap towards edge
            if (newX - radius < minX) newX = minX + radius;
            if (newX + radius > maxX) newX = maxX - radius;
            if (newY - radius < minY) newY = minY + radius;
            if (newY + radius > maxY) newY = maxY - radius;

            //bounce back at top/bottom 
            pos.y = newY;
            if (!(newY + radius <= maxY - CMP && newY - radius >= minY + CMP)) {
                lastWallHit = base.pos;
                if (lastWallHit.y + radius > maxY - CMP) lastWallHit.y = maxY;
                if (lastWallHit.y - radius < minY + CMP) lastWallHit.y = minY;
                angle -= 2 * angle;
            }

            //bounce back at left/right wall
            pos.x = newX;
            if (!(newX + radius <= maxX - CMP && newX - radius >= minX + CMP)) {
                lastWallHit = base.pos;
                if (lastWallHit.x + radius > maxX - CMP) lastWallHit.x = maxX;
                if (lastWallHit.x - radius < minX + CMP) lastWallHit.x = minX;
                angle += (Math.PI - 2 * angle);
            }

            angle %= 2 * Math.PI; //prevent overflow

            FivePebblesPong.ME.Logger_p.LogInfo("angle: " + angle.ToString() + " pos: " + pos.ToString() + " lastWallHit: " + lastWallHit);
        }
    }
}
