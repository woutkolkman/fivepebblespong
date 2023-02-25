using System;
using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class Pong : FPGame
    {
        public PongPaddle playerPdl;
        public PongPaddle pebblesPdl;
        public PongBall ball;
        public PongLine line;
        public SquareBorderMark border;
        public bool playerLastWin = true;
        public const float POS_OFFSET_SPEED = 13; //keep up with fast paddle by altering getTo position
        public const int GETREADY_WAIT = 120; //frames
        public int pebblesWin = 0;
        public int playerWin = 0;
        public PearlSelection scoreBoard;
        public List<Vector2> scoreCount;
        public const float SCORE_HEIGHT = 135;
        public static bool compliment = true;
        public static bool grabbedScoreReacted = false;
        public float ballAccel = 0.003f;
        public float startSpeed = 6f;
        public int pebblesUpdateRate = 8; //calculate ball trajectory every X frames
        public bool waterMoonReacted = false;


        public enum State
        {
            GetReady,
            Playing,
            PlayerWin,
            PebblesWin
        }
        public State state { get; set; }


        public Pong(OracleBehavior self) : base(self)
        {
            this.border = new SquareBorderMark(self, base.maxX - base.minX, base.maxY - base.minY, "FPP_Border", reloadImg: true);
            this.border.pos = new Vector2(midX, midY);

            base.maxX += 40; //ball can move offscreen
            base.minX -= 40; //ball can move offscreen

            this.CreatePaddles(self, 100, 100, 20);

            this.ball = new PongBall(self, this, 10, "FPP_Ball", reloadImg: true);
            ball.SetFlashing(true);

            this.line = new PongLine(self, false, lenY, 2, 18, Color.white, "FPP_Line", reloadImg: true);
            this.line.pos = new Vector2(midX, midY);

            if (self is SSOracleBehavior)
                scoreBoard = new PearlSelection(self as SSOracleBehavior);
            scoreCount = new List<Vector2>();
        }


        ~Pong() //destructor
        {
            this.Destroy(); //if not done already
        }


        public void CreatePaddles(OracleBehavior self, int playerPdlHeight, int pebblesPdlHeight, int pdlWidth)
        {
            float playerY = playerPdl != null ? playerPdl.pos.y : midY;
            float pebblesY = pebblesPdl != null ? pebblesPdl.pos.y : midY;
            playerPdl?.Destroy();
            pebblesPdl?.Destroy();
            int paddleOffset = 260;
            this.playerPdl = new PongPaddle(self, this, pdlWidth, playerPdlHeight, "FPP_Player", PlayerGraphics.SlugcatColor(p?.playerState?.slugcatCharacter ?? SlugcatStats.Name.White), 10, reloadImg: true);
            this.playerPdl.pos = new Vector2(midX - paddleOffset, playerY);
            Color paddleColor = (self.oracle.ID == Oracle.OracleID.SS ?
                new Color(0.44705883f, 0.9019608f, 0.76862746f) : //5P overseer color
                new Color(1f, 0.8f, 0.3f) //Moon overseer color
            );
            this.pebblesPdl = new PongPaddle(self, this, pdlWidth, pebblesPdlHeight, "FPP_Pebbles", paddleColor, reloadImg: true);
            this.pebblesPdl.pos = new Vector2(midX + paddleOffset, pebblesY);

            //reset random offset, else next ball could be missed
            this.randomOffsY = 0f;
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            this.playerPdl?.Destroy();
            this.pebblesPdl?.Destroy();
            this.ball?.Destroy();
            this.line?.Destroy();
            this.border?.Destroy();
            this.scoreCount.Clear();
            this.scoreBoard?.Destroy();
        }


        public override void Update(OracleBehavior self)
        {
            base.Update(self);

            //increase ball speed gradually
            ball.movementSpeed = startSpeed + (ballAccel * base.gameCounter);

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
            int pebblesInput = (self.oracle.ID == Oracle.OracleID.SS) ? PebblesAI() : MoonAI();
            if (pebblesPdl.Update(0, pebblesInput, ball)) //if ball is hit
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);
            if (playerPdl.Update(0, (p?.input[0].y ?? 0), ball)) //if ball is hit
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);

            //move puppet and look at player/ball
            if (self is SSOracleBehavior) {
                (self as SSOracleBehavior).SetNewDestination(pebblesPdl.pos); //moves handle closer occasionally
                (self as SSOracleBehavior).currentGetTo = pebblesPdl.pos;
                (self as SSOracleBehavior).currentGetTo.y += pebblesInput * pebblesPdl.movementSpeed * POS_OFFSET_SPEED; //keep up with fast paddle
                (self as SSOracleBehavior).floatyMovement = false;
            } else if (self is SLOracleBehavior) {
                (self as SLOracleBehavior).currentGetTo = pebblesPdl.pos;
                (self as SLOracleBehavior).currentGetTo.y += pebblesInput * pebblesPdl.movementSpeed * POS_OFFSET_SPEED; //keep up with fast paddle
            }
            self.lookPoint = (state == State.Playing) ? ball.pos : (p?.DangerPos ?? self.player?.DangerPos ?? new Vector2());

            //update score
            if (self is SSOracleBehavior)
                scoreBoard?.Update(self as SSOracleBehavior, scoreCount);
            if (!grabbedScoreReacted && playerWin <= 0 && scoreBoard != null && scoreBoard.pearlGrabbed != -1)
            {
                grabbedScoreReacted = true;
                if (self.oracle.ID == Oracle.OracleID.SS)
                    self.dialogBox.Interrupt(self.Translate("No cheating!"), 10);
                if (self.oracle.ID.ToString().Equals("DM"))
                    self.dialogBox.Interrupt(self.Translate("That won't lower my score, but I appreciate your creativity!"), 10);
            }
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            playerPdl.DrawImage(offset);
            pebblesPdl.DrawImage(offset);
            ball.DrawImage(offset);
            line.DrawImage(offset);
            border?.DrawImage(offset);
        }


        public State statePreviousRun = State.GetReady;
        private void StateMachine(OracleBehavior self)
        {
            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.GetReady:
                    if (statePreviousRun != state)
                        ball.SetFlashing(true);
                    if (base.gameCounter > GETREADY_WAIT) {
                        state = State.Playing;
                        ball.SetFlashing(false);
                    }
                    break;

                //======================================================
                case State.Playing:
                    if (ball.lastWallHit.x == minX) //check if wall is hit
                        state = State.PebblesWin;
                    if (ball.lastWallHit.x == maxX)
                        state = State.PlayerWin;
                    if (this.border != null && this.border.image != null) { //slowly remove border
                        this.border.image.alpha -= 0.01f;
                        if (this.border.image.alpha <= 0f)
                            this.border.Destroy();
                    }
                    break;

                //======================================================
                case State.PlayerWin:
                    state = State.GetReady;
                    playerLastWin = true;
                    scoreCount.Add(new Vector2(midX - 60 - 15 * (playerWin % 10), SCORE_HEIGHT + 15 * (playerWin / 10)));
                    playerWin++;
                    if (compliment) {
                        compliment = false;
                        if (self.oracle.ID == Oracle.OracleID.SS) {
                            if (pebblesWin < 10) {
                                self.dialogBox.Interrupt(self.Translate("You're a talented little creature."), 10);
                            } else {
                                self.dialogBox.Interrupt(self.Translate("Nice."), 10);
                            }
                        }
                        if (self.oracle.ID.ToString().Equals("DM") || self.oracle.ID.ToString().Equals("SL"))
                            self.dialogBox.Interrupt(self.Translate("Well done!"), 10);
                    }
                    break;

                //======================================================
                case State.PebblesWin:
                    state = State.GetReady;
                    playerLastWin = false;
                    scoreCount.Add(new Vector2(midX + 60 + 15 * (pebblesWin % 10), SCORE_HEIGHT + 15 * (pebblesWin / 10)));
                    pebblesWin++;
                    if (self.oracle.ID == Oracle.OracleID.SS) {
                        if (pebblesWin == 10) {
                            self.dialogBox.Interrupt(self.Translate("Let's make this somewhat fair."), 10);
                            this.CreatePaddles(self, 130, 70, 20);
                        }
                        if (pebblesWin == 15) {
                            self.dialogBox.Interrupt(self.Translate("Try again."), 10);
                            this.CreatePaddles(self, 200, 30, 20);
                            playerPdl.movementSpeed += 1f;
                        }
                    }
                    if (self.oracle.ID == Oracle.OracleID.SL && !waterMoonReacted && ball.pos.y < minY + 80) {
                        waterMoonReacted = true;
                        self.dialogBox.Interrupt(self.Translate("Sorry, the game has become a bit difficult now that there's water in this room."), 10);
                    }
                    break;

                //======================================================
                default:
                    state = State.GetReady;
                    break;
            }
            if (state != stateBeforeRun)
                base.gameCounter = 0;

            statePreviousRun = stateBeforeRun;
        }


        private float predY;
        private float randomOffsY;
        private bool newRandomOffsY;
        public int PebblesAI()
        {
            //if (false) //player controlled
            //    return self.player.input[0].y;

            const int deadband = 5; //avoids paddle oscillation
            bool once = false;

            //https://stackoverflow.com/questions/61139016/how-to-predict-trajectory-of-ball-in-a-ping-pong-game-for-ai-paddle-prediction
            if (base.gameCounter % pebblesUpdateRate == 0) //only execute every <pebblesUpdateRate> frames
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
            if (once || UnityEngine.Random.value < 0.002f)
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


        public float moonDifficulty = 0.7f;
        private bool moonInputDisabled;
        private int moonDelay;
        public int MoonAI()
        {
            int input = PebblesAI();

            if (state == State.PlayerWin && moonDifficulty < 1f)
                moonDifficulty += 0.1f;
            if (state == State.PebblesWin && moonDifficulty > 0.3f)
                moonDifficulty -= 0.1f;
            if (state == State.PlayerWin || state == State.PebblesWin)
                FivePebblesPong.ME.Logger_p.LogInfo("New moonDifficulty: " + moonDifficulty);

            if (this.gameCounter % 15 == 0)
            {
                moonInputDisabled = (UnityEngine.Random.value > moonDifficulty);
                if (ball.velocityX < 0) //ball moves away from puppet
                    moonDelay = 15 - (int)(15 * moonDifficulty);
            }

            if (ball.velocityX > 0 && moonDelay > 0) //ball moves towards puppet
                moonDelay--;
            if ((ball.velocityX > 0 && moonDelay > 0) || moonInputDisabled)
                input = 0;

            return input;
        }
    }
}
