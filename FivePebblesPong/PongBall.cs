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


        public PongBall(SSOracleBehavior self, FPGame game, int radius, string imageName) : base(imageName)
        {
            this.radius = radius;
            this.movementSpeed = 4f; //TODO gradually increase
            base.pos = new Vector2(game.midX, game.midY);
            this.angle = 5.0;

            //position boundaries
            this.maxY = game.maxY;
            this.minY = game.minY;
            this.maxX = game.maxX;
            this.minX = game.minX;

            base.SetImage(self, CreateGamePNGs.DrawCircle(radius, 2));
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

            //bounce back at any edge
            if (newY + radius <= maxY - 0.01f && newY - radius >= minY + 0.01f)
            {
                pos.y = newY;
            }
            else
            {
                lastWallHit = base.pos;
                angle -= 2 * angle;
                //angle = angle + (180 - (2 * angle));
            }
            if (newX + radius <= maxX - 0.01f && newX - radius >= minX + 0.01f)
            {
                pos.x = newX;
            } else
            {
                lastWallHit = base.pos; //TODO lastWallHit komt niet direct overeen met wall
                //angle -= (Math.PI + 2*angle);
                angle += (Math.PI - 2 * angle);
                //angle = angle + (180 - (2 * angle));
            }

            if (angle >= 2*Math.PI) angle -= 2*Math.PI;
            if (angle < 0) angle += 2*Math.PI;

            FivePebblesPong.ME.Logger_p.LogInfo("angle: " + angle.ToString() + " pos: " + pos.ToString() + " lastWallHit: " + lastWallHit);
        }
    }
}
