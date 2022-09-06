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
            this.movementSpeed = 6f;

            //position boundaries
            this.maxY = game.maxY;
            this.minY = game.minY;
            this.maxX = game.maxX;
            this.minX = game.minX;

            base.SetImage(self, CreateGamePNGs.DrawRectangle(width, height, 2));
        }


        public void Update(int inputX, int inputY, PongBall ball)
        {
            float newX = pos.x + inputX * movementSpeed;
            float newY = pos.y + inputY * movementSpeed;
            float vEdge = (width / 2);
            float hEdge = (height / 2);

            //close gap towards edge, also in case of invalid spawn location
            if (newX - vEdge < minX && maxX != minX) newX = minX + vEdge;
            if (newX + vEdge > maxX && maxX != minX) newX = maxX - vEdge;
            if (newY - hEdge < minY && maxY != minY) newY = minY + hEdge;
            if (newY + hEdge > maxY && maxY != minY) newY = maxY - hEdge;

            //stop at any edge
            if (newX + vEdge <= maxX && newX - vEdge >= minX)
                pos.x = newX;

            if (newY + hEdge <= maxY && newY - hEdge >= minY)
                pos.y = newY;

            //check if ball is hit
            if (ball != null)
            {
                //left/right side
                if (Math.Abs(ball.pos.y - pos.y) <= hEdge)
                {
                    float normalized = (pos.y - ball.pos.y) / hEdge;
                    if (ball.pos.x - ball.radius <= pos.x + vEdge && ball.pos.x > pos.x)
                    { //bounce to right
                        ball.angle = ball.paddleBounceAngle * normalized;
                    } else if (ball.pos.x + ball.radius >= pos.x - vEdge && ball.pos.x < pos.x)
                    { //bounce to left
                        ball.angle = ball.paddleBounceAngle * normalized + Math.PI; //TODO check ball bounce direction
                        ball.angle -= 2 * ball.angle;
                    }
                }

                //up/bottom side
                if (Math.Abs(ball.pos.x - pos.x) <= vEdge)
                {
                    if ((ball.pos.y - ball.radius <= pos.y + hEdge && ball.pos.y > pos.y) ||
                        (ball.pos.y + ball.radius >= pos.y - hEdge && ball.pos.y < pos.y))
                    {
                        ball.ReverseYDir();
                    }
                }

                //bounce from points of paddle
                //TODO, missing calculation is not noticable while playing --> don't implement and save CPU cycles
                //TODO, if length from point to ball.pos is smaller or equal to ball.radius
            }
        }
    }
}
