using System;
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
        public static bool compliment = true;

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
            this.playerPdl = new PongPaddle(self, this, 20, 100, "FPP_Player", PlayerGraphics.SlugcatColor(0), 10, reloadImg: true); //TODO choose slugcat which carries gamecontroller
            this.playerPdl.pos = new Vector2(midX - paddleOffset, midY);

            this.pebblesPdl = new PongPaddle(self, this, 20, 100, "FPP_Pebbles", new Color(0.44705883f, 0.9019608f, 0.76862746f), reloadImg: true); //5P overseer color
            this.pebblesPdl.pos = new Vector2(midX + paddleOffset, midY);

            this.ball = new PongBall(self, this, 10, "FPP_Ball", reloadImg: true);

            this.line = new PongLine(self, false, lenY, 2, 18, Color.white, "FPP_Line", reloadImg: true);
            this.line.pos = new Vector2(midX, midY);
        }


        ~Pong() //destructor
        {
            this.Destroy(); //if not done already
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

            this.StateMachine(self);
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
                    if (compliment)
                        self.dialogBox.Interrupt(self.Translate("You're a talented little creature."), 10);
                    compliment = false;
                    break;

                //======================================================
                case State.PebblesWin:
                    state = State.GetReady;
                    playerLastWin = false;
                    break;

                //======================================================
                default:
                    state = State.GetReady;
                    break;
            }
            if (state != previousState)
                base.gameCounter = 0;
        }


        private float predY;
        private float randomOffsY;
        private bool newRandomOffsY;
        public int PebblesAI(SSOracleBehavior self)
        {
            //if (false) //player controlled
            //    return self.player.input[0].y;

            const int deadband = 5; //avoids paddle oscillation
            bool once = false;

            //https://stackoverflow.com/questions/61139016/how-to-predict-trajectory-of-ball-in-a-ping-pong-game-for-ai-paddle-prediction
            if (base.gameCounter % 4 == 0) //only execute every 4 frames
            {
                if (ball.velocityX > 0) //if ball moves towards Pebbles
                {
                    //height ball position area
                    float mHeight = base.lenY - (2 * ball.radius);

                    //deltaY per X
                    float slope = ball.velocityY / ball.velocityX;
                    if (float.IsInfinity(slope) || float.IsNegativeInfinity(slope))
                        slope = 0; //should never run with ballBounceAngle applied

                    //distance towards paddle
                    float deltaX = pebblesPdl.pos.x - pebblesPdl.width / 2 - ball.radius - ball.pos.x;

                    //predict intercept point without walls
                    float intercept = Math.Abs((ball.pos.y - ball.minY - ball.radius) + (deltaX * slope));
                    predY = (intercept % (2 * mHeight));

                    //remove walls and ballradius
                    if (predY > (mHeight))
                        predY = (2 * mHeight) - predY;
                    predY += ball.minY + ball.radius;

                    newRandomOffsY = true;
                }
                else
                { //ball moves away from Pebbles
                    predY = base.midY;
                    if (newRandomOffsY && UnityEngine.Random.value < 0.7f)
                        once = true;
                    newRandomOffsY = false;
                }
            }

            //at random interval a random offset from predicted ball intercept
            if (once || UnityEngine.Random.value < 0.001f)
            {
                once = false;
                float allowed = (pebblesPdl.height / 2) - deadband;
                randomOffsY = allowed * UnityEngine.Random.Range(-1f, 1f);
            }

            if (predY + randomOffsY > pebblesPdl.pos.y + deadband)
                return 1;
            if (predY + randomOffsY < pebblesPdl.pos.y - deadband)
                return -1;
            return 0;
        }
    }
}
