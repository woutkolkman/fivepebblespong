﻿using System.Collections.Generic;
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


        public bool Update(int inputX, int inputY, PongBall ball)
        {
            bool hitBall = false;

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
                        hitBall = true;
                    } else if (ball.pos.x + ball.radius >= pos.x - vEdge && ball.pos.x < pos.x)
                    { //bounce to left
                        ball.angle = ball.paddleBounceAngle * normalized;
                        ball.ReverseXDir();
                        hitBall = true;
                    }
                }

                //up/bottom side
                if (Math.Abs(ball.pos.x - pos.x) <= vEdge)
                {
                    if ((ball.pos.y - ball.radius <= pos.y + hEdge && ball.pos.y > pos.y) ||
                        (ball.pos.y + ball.radius >= pos.y - hEdge && ball.pos.y < pos.y))
                    {
                        ball.ReverseYDir();
                        hitBall = true;
                    }
                }


                //bounce from points of paddle
                float x = ((ball.pos.x >= pos.x) ? (pos.x + vEdge) : (pos.x - vEdge));
                float y = ((ball.pos.y >= pos.y) ? (pos.y + hEdge) : (pos.y - hEdge));
                Vector2 closestPoint = new Vector2(x, y);
                if (Vector2.Distance(ball.pos, closestPoint) <= ball.radius)
                {
                    ball.angle = ball.paddleBounceAngle;
                    if (y > pos.y)
                        ball.ReverseYDir();
                    if (x < pos.x)
                        ball.ReverseXDir();
                    hitBall = true;
                }
            }
            return hitBall;
        }
    }
}
