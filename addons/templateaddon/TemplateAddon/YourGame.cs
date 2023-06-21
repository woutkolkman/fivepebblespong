using System;
using System.Collections.Generic;
using UnityEngine;

namespace TemplateAddon
{
    public class YourGame : FivePebblesPong.FPGame
    {
        public FivePebblesPong.PongBall ball;


        public YourGame(OracleBehavior self) : base(self)
        {
            //spawn a ball projection in the middle of this room
            ball = new FivePebblesPong.PongBall(self, this, 15, "FPP_VeryNiceBall");
            ball.pos = new Vector2(midX, midY);
            ball.angle = 1f;
        }


        ~YourGame() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            ball?.Destroy();
        }


        public override void Update(OracleBehavior self)
        {
            //function is called 40 times a second

            base.Update(self);

            //update ball and change direction
            ball.Update();

            //set pebbles' behavior
            if (self is SSOracleBehavior)
                (self as SSOracleBehavior).movementBehavior = SSOracleBehavior.MovementBehavior.Talk;

            //read player input
            int pY = p?.input[0].y ?? 0;
            //and change palette
            palette = pY != 0 ? 25 : 26;

            //switch every 15 seconds
            if (base.gameCounter % 1200 > 600)
                return;

            //look at ball
            self.lookPoint = ball.pos;

            if (!(self is SSOracleBehavior))
                return;

            //nothing actually happens, you can program his movement here
            (self as SSOracleBehavior).movementBehavior = FivePebblesPong.Enums.SSPlayGame;

            //go to middle of room
            Vector2 middleOfRoom = new Vector2(midX, midY);
            (self as SSOracleBehavior).SetNewDestination(middleOfRoom); //moves handle closer occasionally
            (self as SSOracleBehavior).currentGetTo = middleOfRoom;
            (self as SSOracleBehavior).floatyMovement = false;
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            ball.DrawImage(offset);
        }
    }
}
