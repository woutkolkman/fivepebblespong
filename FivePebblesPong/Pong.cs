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
        public bool playerLastWin;

        public enum State
        {
            GetReady,
            Playing,
            PlayerWin,
            PebblesWin,
            Reset
        }
        public State state { get; set; }


        public Pong(SSOracleBehavior self) : base(self)
        {
            base.maxX += 40; //ball can move offscreen
            base.minX -= 40; //ball can move offscreen
            this.playerPdl = new PongPaddle(self, this, 20, 100, "FPP_Player");
            this.playerPdl.pos = new Vector2(midX-260, midY);

            this.pebblesPdl = new PongPaddle(self, this, 20, 100, "FPP_Pebbles");
            this.pebblesPdl.pos = new Vector2(midX+260, midY);

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
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            State previousState = state;
            switch (state)
            {
                //======================================================
                case State.GetReady:
                    if (ball == null)
                        ball = new PongBall(self, this, 10, "FPP_Ball");
                    ball.pos = new Vector2(midX, midY);
                    ball.angle = (playerLastWin ? Math.PI : 0); //pass ball to last winner
                    ball.lastWallHit = new Vector2(); //reset
                    if (base.gameCounter > 120)
                        state = State.Playing;
                    break;

                //======================================================
                case State.Playing:
                    if (ball == null) {
                        state = State.GetReady;
                        break;
                    }
                    ball.Update();
                    if (ball.lastWallHit.x == minX) //check if wall is hit
                        state = State.PebblesWin;
                    if (ball.lastWallHit.x == maxX)
                        state = State.PlayerWin;
                    if (state != State.Playing)
                    {
                        ball?.Destroy();
                        ball = null;
                    }
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

            //update paddles
            int pebblesInput = PebblesAI(self);
            pebblesPdl.Update(0, pebblesInput, ball);
            playerPdl.Update(0, self.player.input[0].y, ball);

            //move puppet and look at player/ball
            self.SetNewDestination(pebblesPdl.pos); //moves handle closer occasionally
            self.currentGetTo = pebblesPdl.pos;
            self.currentGetTo.y += pebblesInput * 80; //keep up with fast paddle
            self.floatyMovement = false;
            self.lookPoint = ball != null ? ball.pos : self.player.DangerPos;

            //update image positions
            playerPdl.DrawImage();
            pebblesPdl.DrawImage();
            ball?.DrawImage();
        }


        public int PebblesAI(SSOracleBehavior self)
        {
            return self.player.input[0].y;
        }
    }
}
