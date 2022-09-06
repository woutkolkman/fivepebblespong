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
            this.playerPdl.pos = new Vector2(minX, midY);

            this.pebblesPdl = new PongPaddle(self, this, 20, 100, "FPP_Pebbles");
            this.pebblesPdl.pos = new Vector2(maxX, midY);

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
            playerPdl.Update(self.player.input[0].x, self.player.input[0].y);
            pebblesPdl.Update(0, 0);
            ball.Update();

            //move pebbles and look at player/ball
            self.currentGetTo = pebblesPdl.pos;
            self.floatyMovement = false;
            self.lookPoint = ball != null ? ball.pos : self.player.DangerPos;

            playerPdl.DrawImage();
            pebblesPdl.DrawImage();
            ball.DrawImage();
        }
    }
}
