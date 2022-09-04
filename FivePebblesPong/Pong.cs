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


        public Pong(SSOracleBehavior self) : base(self)
        {
            this.playerPdl = new PongPaddle(self, this, 25, 100, "FPP_Player");
            this.playerPdl.pos = new Vector2(minX, midY);

            pebblesPdl = new PongPaddle(self, this, 25, 100, "FPP_Pebbles");
            pebblesPdl.pos = new Vector2(maxX, midY);

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
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self); //empty
            playerPdl.Update(self.player.input[0].x, self.player.input[0].y);
            pebblesPdl.Update(0, 0);

            playerPdl.DrawImage();
            pebblesPdl.DrawImage();
        }
    }
}
