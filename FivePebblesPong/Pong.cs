using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class Pong : FPGame
    {
        public PongPaddle playerPdl;
        public PongPaddle pebblesPdl;
        public PongBall ball;
        public PongLine line;
        public bool playerLastWin;
        const float POS_OFFSET_SPEED = 80; //keep up with fast paddle by altering getTo position
        const int GETREADY_WAIT = 120; //frames

        public enum State
        {
            GetReady,
            Playing,
            PlayerWin,
            PebblesWin
        }
        public State state { get; set; }


        public Pong(SSOracleBehavior self) : base(self)
        {
            base.maxX += 40; //ball can move offscreen
            base.minX -= 40; //ball can move offscreen
            int paddleOffset = 260;
            this.playerPdl = new PongPaddle(self, this, 20, 100, "FPP_Player");
            this.playerPdl.pos = new Vector2(midX - paddleOffset, midY);

            this.pebblesPdl = new PongPaddle(self, this, 20, 100, "FPP_Pebbles");
            this.pebblesPdl.pos = new Vector2(midX + paddleOffset, midY);

            this.ball = new PongBall(self, this, 10, "FPP_Ball");

            this.line = new PongLine(self, this, "FPP_Line");

            FivePebblesPong.ME.Logger_p.LogInfo("Pong constructor"); //TODO remove
        }


        ~Pong() //destructor
        {
            this.Destroy(); //if not done already
            FivePebblesPong.ME.Logger_p.LogInfo("Pong destructor"); //TODO remove
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            this.playerPdl?.Destroy();
            this.pebblesPdl?.Destroy();
            this.ball?.Destroy();
            this.line?.Destroy();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            //increase ball speed gradually
            ball.movementSpeed = 5.5f + (0.001f * base.gameCounter);

            StateMachine(self);
            if (state == State.GetReady)
            { //reset ball position and angle
                ball.pos = new Vector2(midX, midY);
                ball.angle = (playerLastWin ? Math.PI : 0); //pass ball to last winner
                ball.lastWallHit = new Vector2(); //reset wall hit
            }
            if (state == State.Playing)
                if (ball.Update()) //if wall is hit
                    self.oracle.room.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked, self.oracle.firstChunk);

            //update paddles
            int pebblesInput = PebblesAI(self);
            if (pebblesPdl.Update(0, pebblesInput, ball)) //if ball is hit
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);
            if (playerPdl.Update(0, self.player.input[0].y, ball)) //if ball is hit
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);

            //move puppet and look at player/ball
            self.SetNewDestination(pebblesPdl.pos); //moves handle closer occasionally
            self.currentGetTo = pebblesPdl.pos;
            self.currentGetTo.y += pebblesInput * POS_OFFSET_SPEED; //keep up with fast paddle
            self.floatyMovement = false;
            self.lookPoint = (state == State.Playing) ? ball.pos : self.player.DangerPos;

            //update image positions
            playerPdl.DrawImage();
            pebblesPdl.DrawImage();
            ball.DrawImage();
            line.DrawImage();
        }


        private void StateMachine(SSOracleBehavior self)
        {
            State previousState = state;
            switch (state)
            {
                //======================================================
                case State.GetReady:
                    if (base.gameCounter > GETREADY_WAIT)
                        state = State.Playing;
                    break;

                //======================================================
                case State.Playing:
                    if (ball.lastWallHit.x == minX) //check if wall is hit
                        state = State.PebblesWin;
                    if (ball.lastWallHit.x == maxX)
                        state = State.PlayerWin;
                    break;

                //======================================================
                case State.PlayerWin:
                    state = State.GetReady;
                    playerLastWin = true;
                    FivePebblesPong.ME.Logger_p.LogInfo("PlayerWin"); //TODO remove
                    //TODO dialogue
                    break;

                //======================================================
                case State.PebblesWin:
                    state = State.GetReady;
                    playerLastWin = false;
                    FivePebblesPong.ME.Logger_p.LogInfo("PebblesWin"); //TODO remove
                    //TODO dialogue
                    break;

                //======================================================
                default:
                    state = State.GetReady;
                    break;
            }
            if (state != previousState)
                base.gameCounter = 0;
        }


        public int PebblesAI(SSOracleBehavior self)
        {
            return self.player.input[0].y;
            //TODO
        }
    }
}
