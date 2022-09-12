using System;
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


        public Breakout(SSOracleBehavior self) : base(self)
        {
            base.minX += 15;
            this.paddle = new PongPaddle(self, this, 20, 100, "FPP_Player");
            this.paddle.pos = new Vector2(minX, midY);
            this.paddle.maxX = minX + lenX / 3;

            this.lineMid = new PongLine(self, false, lenY, 2, 0, Color.white, "FPP_Line");
            this.lineMid.pos = new Vector2(this.paddle.maxX, midY);
            this.lineEnd = new PongLine(self, false, lenY, 2, 18, Color.red, "FPP_Line2");
            this.lineEnd.pos = new Vector2(this.paddle.minX, midY);

            this.ball = new PongBall(self, this, 10, "FPP_Ball");
            this.ball.pos = new Vector2(midX, midY);
            this.ball.movementSpeed = 8f;
            this.ball.paddleBounceAngle = 1.1;
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

            //update image positions
            ball.DrawImage();
            paddle.DrawImage();
            lineMid.DrawImage();
            lineEnd.DrawImage();
        }
    }
}
