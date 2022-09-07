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


        public Pong(SSOracleBehavior self) : base(self)
        {
            this.playerPdl = new PongPaddle(self, this, 20, 100, "FPP_Player");
            this.playerPdl.pos = new Vector2(minX+30, midY);

            this.pebblesPdl = new PongPaddle(self, this, 20, 100, "FPP_Pebbles");
            this.pebblesPdl.pos = new Vector2(maxX-30, midY);

            this.ball = new PongBall(self, this, 10, "FPP_Ball");

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
            this.playerPdl.Destroy();
            this.pebblesPdl.Destroy();
            this.ball.Destroy();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self); //empty
            ball.Update();
            playerPdl.Update(0, self.player.input[0].y, ball);

            int pebblesInput = PebblesAI(self);
            pebblesPdl.Update(0, pebblesInput, ball);

            //move puppet and look at player/ball
            self.SetNewDestination(pebblesPdl.pos); //moves handle closer occasionally
            self.currentGetTo = pebblesPdl.pos;
            self.currentGetTo.y += pebblesInput * 80; //keep up with fast paddle
            self.floatyMovement = false;
            self.lookPoint = ball != null ? ball.pos : self.player.DangerPos;

            playerPdl.DrawImage();
            pebblesPdl.DrawImage();
            ball.DrawImage();
        }


        public int PebblesAI(SSOracleBehavior self)
        {
            return self.player.input[0].y;
        }
    }
}
