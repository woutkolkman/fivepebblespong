﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class Breakout : FPGame
    {
        public PongPaddle paddle;
        public PongBall ball;
        public PongLine lineMid;
        public PongLine lineEnd;
        const float POS_OFFSET_SPEED = 80; //keep up with fast paddle by altering getTo position
        const int GETREADY_WAIT = 120; //frames
        public List<PongPaddle> bricks;


        public Breakout(SSOracleBehavior self) : base(self)
        {
            base.minX += 15;
            this.paddle = new PongPaddle(self, this, 20, 100, "FPP_Player", reloadImg: true);
            this.paddle.pos = new Vector2(minX, midY);
            this.paddle.maxX = minX + lenX / 3;
            this.paddle.ballBounceAngle = 1.1;

            this.lineMid = new PongLine(self, false, lenY, 4, 0, Color.white, "FPP_Line", reloadImg: true);
            this.lineMid.pos = new Vector2(this.paddle.maxX, midY);
            this.lineEnd = new PongLine(self, false, lenY, 4, 18, Color.red, "FPP_Line2", reloadImg: true);
            this.lineEnd.pos = new Vector2(this.paddle.minX, midY);

            this.ball = new PongBall(self, this, 15, "FPP_Ball", reloadImg: true);
            this.ball.pos = new Vector2(midX, midY);
            this.ball.movementSpeed = 8f;
            this.ball.angle = Math.PI; //move towards player first

            this.bricks = new List<PongPaddle>();
        }


        ~Breakout() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            paddle?.Destroy();
            ball?.Destroy();
            lineMid?.Destroy();
            lineEnd?.Destroy();
            for (int i = 0; i < bricks.Count; i++)
                bricks[i]?.Destroy();
            bricks?.Clear();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            //read player input
            int pX = self.player.input[0].x;
            int pY = self.player.input[0].y;

            //update objects
            if (base.gameCounter > GETREADY_WAIT)
                if (ball.Update()) //if wall is hit
                    self.oracle.room.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked, self.oracle.firstChunk);
            if (paddle.Update(pX, pY, ball)) //if ball is hit
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);

            //move puppet and look at player/ball
            self.SetNewDestination(paddle.pos); //moves handle closer occasionally
            self.currentGetTo = paddle.pos;
            self.currentGetTo.x += pX * POS_OFFSET_SPEED; //keep up with fast paddle
            self.currentGetTo.y += pY * POS_OFFSET_SPEED; //keep up with fast paddle
            if (self.currentGetTo.x > paddle.maxX) //keep puppet behind line
                self.currentGetTo.x = paddle.maxX;
            self.floatyMovement = false;
            self.lookPoint = (ball != null) ? ball.pos : self.player.DangerPos;

            //place bricks
            if (bricks.Count <= 0)
                this.PlaceBricks(self);

            //update brick closest to ball
            int closest = -1;
            float minDist = float.MaxValue;
            for (int i = 0; i < bricks.Count; i++)
            {
                float curDist = Vector2.Distance(ball.pos, bricks[i].pos);
                if (curDist < minDist)
                {
                    minDist = curDist;
                    closest = i;
                }
                bricks[i].DrawImage();
            }

            //if there is a closest brick, check if it is hit
            if (closest > -1) {
                if (bricks[closest].Update(0, 0, ball))
                {
                    bricks[closest].Destroy();
                    bricks.RemoveAt(closest);
                    //self.oracle.room.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, self.oracle.firstChunk);
                    //self.oracle.room.PlaySound(SoundID.HUD_Karma_Reinforce_Flicker, self.oracle.firstChunk);
                    //self.oracle.room.PlaySound(SoundID.Calm_Surface_Hit_Wall, self.oracle.firstChunk);
                    self.oracle.room.PlaySound(SoundID.Mouse_Light_Switch_On, self.oracle.firstChunk);
                }
            }

            //update image positions
            ball.DrawImage();
            paddle.DrawImage();
            lineMid.DrawImage();
            lineEnd.DrawImage();
        }


        private static readonly string[] PlaceBricks_imgNames = { "FPP_BrickG", "FPP_BrickB", "FPP_BrickY", "FPP_BrickR" };
        private static readonly Color[] PlaceBricks_colors = { Color.green, Color.blue, Color.yellow, Color.red };
        public void PlaceBricks(SSOracleBehavior self)
        {
            //clear any bricks that are left
            for (int i = 0; i < bricks.Count; i++)
                bricks[i]?.Destroy();
            bricks.Clear();

            //calculate brick size and distance
            const int brickSpacing = 20;
            const int brickColumn = 5;
            int brickHeight = lenY / brickColumn - brickSpacing;
            const int brickWidth = 30;

            //place bricks in different columns and rows
            for (int c = 0; c < PlaceBricks_imgNames.Length; c++)
            {
                for (int r = 0; r < brickColumn; r++)
                {
                    bricks.Add(new PongPaddle(self, this, brickWidth, brickHeight, PlaceBricks_imgNames[c], PlaceBricks_colors[c], brickWidth / 2));
                    bricks[r + c * brickColumn].pos = new Vector2(
                        maxX - (brickWidth * 2) - (brickWidth + brickSpacing) * c,
                        maxY - (brickHeight / 2) - (brickHeight + brickSpacing) * r
                    );
                    bricks[r + c * brickColumn].flatBounce = true;
                }
            }
        }
    }
}