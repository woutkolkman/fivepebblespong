﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class Pong : FPGame
    {
        public PongPaddle leftPdl, rightPdl;
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
        public float extraStartSpeed = 0f; //quicker up to speed when a round ends
        public int pebblesUpdateRate = 6; //calculate ball trajectory every X ticks
        public bool waterMoonReacted = false;
        private bool hrMode; //pebbles gets moved to left paddle
        public bool doubleAI = false; //double AI in Rubicon
        private PongAI rightPdlAI, leftPdlAI;


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
            hrMode = self.oracle?.room?.roomSettings?.name?.ToUpper().Equals("HR_AI") ?? false;
            if (hrMode)
                doubleAI = true;
            rightPdlAI = new PongAI(this, false);
            leftPdlAI = new PongAI(this, true);

            if (self is SSOracleBehavior) { //the only place where border acts as intended (fading)
                this.border = new SquareBorderMark(self, base.maxX - base.minX, base.maxY - base.minY, "FPP_Border", reloadImg: true);
                this.border.pos = new Vector2(midX, midY);
            }

            base.maxX += 40; //ball can move offscreen
            base.minX -= 40; //ball can move offscreen

            this.CreatePaddles(self, 100, 100, 20);

            this.ball = new PongBall(self, this, 10, "FPP_Ball", reloadImg: true);
            ball.SetFlashing(true);

            this.line = new PongLine(self, false, lenY, 2, 18, Color.white, "FPP_Line", reloadImg: true);
            this.line.pos = new Vector2(midX, midY);

            if (self is SSOracleBehavior && !hrMode)
                scoreBoard = new PearlSelection(self as SSOracleBehavior);
            scoreCount = new List<Vector2>();
        }


        ~Pong() //destructor
        {
            this.Destroy(); //if not done already
        }


        public void CreatePaddles(OracleBehavior self, int playerPdlHeight, int pebblesPdlHeight, int pdlWidth)
        {
            float playerY = leftPdl != null ? leftPdl.pos.y : midY;
            float pebblesY = rightPdl != null ? rightPdl.pos.y : midY;
            int paddleOffset = 260;

            leftPdl?.Destroy();
            Color paddleColor = (hrMode ?
                new Color(0.44705883f, 0.9019608f, 0.76862746f) : //5P overseer color
                PlayerGraphics.SlugcatColor(p?.playerState?.slugcatCharacter ?? SlugcatStats.Name.White)
            );
            int playerPaddleThickness = hrMode ? 2 : 10;
            this.leftPdl = new PongPaddle(self, this, pdlWidth, playerPdlHeight, "FPP_Player", paddleColor, playerPaddleThickness, reloadImg: true);
            this.leftPdl.pos = new Vector2(midX - paddleOffset, playerY);

            rightPdl?.Destroy();
            paddleColor = (self.oracle.ID == Oracle.OracleID.SS && !hrMode ?
                new Color(0.44705883f, 0.9019608f, 0.76862746f) : //5P overseer color
                new Color(1f, 0.8f, 0.3f) //Moon overseer color
            );
            int pebblesPaddleThickness = (self is MoreSlugcats.SSOracleRotBehavior ? 10 : 2);
            this.rightPdl = new PongPaddle(self, this, pdlWidth, pebblesPdlHeight, "FPP_Pebbles", paddleColor, pebblesPaddleThickness, reloadImg: true);
            this.rightPdl.pos = new Vector2(midX + paddleOffset, pebblesY);

            //reset random offset, else next ball could be missed
            if (rightPdlAI != null)
                rightPdlAI.randomOffsY = 0f;
            if (leftPdlAI != null)
                leftPdlAI.randomOffsY = 0f;
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            this.leftPdl?.Destroy();
            this.rightPdl?.Destroy();
            this.ball?.Destroy();
            this.line?.Destroy();
            this.border?.Destroy();
            this.scoreCount.Clear();
            this.scoreBoard?.Destroy();
        }


        private int leftPdlInput = 0;
        public override void Update(OracleBehavior self)
        {
            //Rubicon, move puppet and look at player/ball
            if (hrMode && self.oracle.ID == Oracle.OracleID.SS) {
                if (!(self is SSOracleBehavior))
                    return;
                self.lookPoint = (state == State.Playing || hrMode) ? ball.pos : (p?.DangerPos ?? self.player?.DangerPos ?? new Vector2());
                (self as SSOracleBehavior).SetNewDestination(leftPdl.pos); //moves handle closer occasionally
                (self as SSOracleBehavior).currentGetTo = leftPdl.pos;
                (self as SSOracleBehavior).currentGetTo.y += leftPdlInput * leftPdl.movementSpeed * POS_OFFSET_SPEED; //keep up with fast paddle
                (self as SSOracleBehavior).floatyMovement = false;
                return; //Pong.Update is now called twice a tick, so return to prevent speeding up the game
            }

            base.Update(self);

            //increase ball speed gradually
            ball.movementSpeed = startSpeed + extraStartSpeed + (ballAccel * base.gameCounter);

            this.StateMachine(self);
            if (state == State.GetReady) { //reset ball position and angle
                ball.pos = new Vector2(midX, midY);
                ball.angle = (playerLastWin ? Math.PI : 0); //pass ball to last winner
                ball.lastWallHit = new Vector2(); //reset wall hit
            }
            if (state == State.Playing)
                if (ball.Update()) //if wall is hit
                    self.oracle.room.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked, self.oracle.firstChunk);

            //update paddles
            int rightPdlInput = (self.oracle.ID == Oracle.OracleID.SS || hrMode) ? rightPdlAI.PebblesAI() : rightPdlAI.MoonAI();
            if (rightPdl.Update(0, rightPdlInput, ball)) //if ball is hit
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);
            leftPdlInput = doubleAI ? leftPdlAI.PebblesAI() : (p?.input[0].y ?? 0);
            if (leftPdl.Update(0, leftPdlInput, ball)) //if ball is hit
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);

            //move puppet and look at player/ball
            self.lookPoint = (state == State.Playing || hrMode) ? ball.pos : (p?.DangerPos ?? self.player?.DangerPos ?? new Vector2());
            if (self is SSOracleBehavior) {
                (self as SSOracleBehavior).SetNewDestination(rightPdl.pos); //moves handle closer occasionally
                (self as SSOracleBehavior).currentGetTo = rightPdl.pos;
                (self as SSOracleBehavior).currentGetTo.y += rightPdlInput * rightPdl.movementSpeed * POS_OFFSET_SPEED; //keep up with fast paddle
                (self as SSOracleBehavior).floatyMovement = false;
            } else if (self is SLOracleBehavior) {
                Hooks.SLOracleGetToPosOverride = rightPdl.pos; //updates in functions SLOracleBehaviorHasMark_OracleGetToPos_get and SLOracleBehaviorNoMark_OracleGetToPos_get
                Hooks.SLOracleGetToPosOverride.y += 16f /*SL specific*/ + rightPdlInput * rightPdl.movementSpeed * POS_OFFSET_SPEED; //keep up with fast paddle
                if (Hooks.SLOracleGetToPosOverride.y < 150f) //Moon doesn't like cold water
                    Hooks.SLOracleGetToPosOverride.y = 150f;
                SLGameStarter.forceFlightMode = true; //updates in function DefaultSLOracleBehavior
                SLGameStarter.moonLookPoint = self.lookPoint; //updates in function DefaultSLOracleBehavior
            }

            //update score
            if (self is SSOracleBehavior)
                scoreBoard?.Update(self as SSOracleBehavior, scoreCount);
            if (!grabbedScoreReacted && scoreBoard != null && scoreBoard.pearlGrabbed >= 0 &&
                scoreCount.Count > scoreBoard.pearlGrabbed && scoreCount[scoreBoard.pearlGrabbed].x > midX)
            {
                grabbedScoreReacted = true;

                //fixes nullpointerexception, because SSOracleRotBehavior.dialogBox is declared as 'new'
                HUD.DialogBox dialogBox = self.dialogBox;
                if (dialogBox == null && self is MoreSlugcats.SSOracleRotBehavior)
                    dialogBox = (self as MoreSlugcats.SSOracleRotBehavior).dialogBox;

                if (self.oracle.ID == Oracle.OracleID.SS)
                    dialogBox.Interrupt(self.Translate("No cheating!"), 10);
                if (self.oracle.ID.ToString().Equals("DM"))
                    dialogBox.Interrupt(self.Translate("That won't lower my score, but I appreciate your creativity!"), 10);
            }
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            leftPdl.DrawImage(offset);
            rightPdl.DrawImage(offset);
            ball.DrawImage(offset);
            line.DrawImage(offset);
            border?.DrawImage(offset);
        }


        public State statePreviousRun = State.GetReady;
        private void StateMachine(OracleBehavior self)
        {
            //fixes nullpointerexception, because SSOracleRotBehavior.dialogBox is declared as 'new'
            HUD.DialogBox dialogBox = self.dialogBox;
            if (dialogBox == null && self is MoreSlugcats.SSOracleRotBehavior)
                dialogBox = (self as MoreSlugcats.SSOracleRotBehavior).dialogBox;

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

                    if (state != State.Playing) { //adjust ball starting speed
                        float pct = 0.50f;
                        if (self.oracle.ID == Oracle.OracleID.SL || self.oracle.ID.ToString().Equals("DM"))
                            pct = 0.25f;
                        extraStartSpeed = (ball.movementSpeed - startSpeed) * pct;
                    }

                    if (this.border != null && this.border.image != null) { //slowly remove border
                        this.border.image.alpha -= 0.01f;
                        if (this.border.image.alpha <= 0f)
                            this.border.Destroy();
                    }

                    //reaction if all pearls are used
                    if (state != State.Playing && self is SSOracleBehavior && scoreBoard?.pearls?.Count > 0) {
                        if (scoreCount.Count + 1 == scoreBoard.pearls.Count)
                            scoreBoard.RefreshPearlsInRoom(self as SSOracleBehavior);
                        if (scoreCount.Count + 1 == scoreBoard.pearls.Count) {
                            if (self.oracle.ID == Oracle.OracleID.SS)
                                dialogBox.Interrupt(self.Translate("It looks like we used all pearls." + (playerWin == 0 ? " Git gud." : "")), 10);
                            if (self.oracle.ID.ToString().Equals("DM"))
                                dialogBox.Interrupt(self.Translate("Now that's a lot of playtime!"), 10);
                        }
                    }
                    break;

                //======================================================
                case State.PlayerWin:
                    state = State.GetReady;
                    playerLastWin = true;
                    scoreCount.Add(new Vector2(midX - 60 - 15 * (playerWin % 10), SCORE_HEIGHT + 15 * (playerWin / 10)));
                    playerWin++;
                    if (hrMode)
                        break;
                    if (compliment) {
                        compliment = false;
                        if (self.oracle.ID == Oracle.OracleID.SS) {
                            if (pebblesWin < 10) {
                                dialogBox.Interrupt(self.Translate("You're a talented little creature."), 10);
                            } else {
                                dialogBox.Interrupt(self.Translate("Nice."), 10);
                            }
                        }
                        if (self.oracle.ID.ToString().Equals("DM") || self.oracle.ID.ToString().Equals("SL"))
                            dialogBox.Interrupt(self.Translate("Well done!"), 10);
                    }
                    if (playerWin == 4 && (self.oracle.ID.ToString().Equals("DM") || self.oracle.ID.ToString().Equals("SL")))
                        dialogBox.Interrupt(self.Translate("You're getting better every game!"), 10);
                    break;

                //======================================================
                case State.PebblesWin:
                    state = State.GetReady;
                    playerLastWin = false;
                    scoreCount.Add(new Vector2(midX + 60 + 15 * (pebblesWin % 10), SCORE_HEIGHT + 15 * (pebblesWin / 10)));
                    pebblesWin++;
                    if (hrMode)
                        break;
                    if (self is SSOracleBehavior && //remove this if MoreSlugcats.SSOracleRotBehavior should also have this behavior
                        self.oracle.ID == Oracle.OracleID.SS) {
                        if (pebblesWin == 10) {
                            dialogBox.Interrupt(self.Translate("Let's make this somewhat fair."), 10);
                            this.CreatePaddles(self, 130, 70, 20);
                        }
                        if (pebblesWin == 15) {
                            dialogBox.Interrupt(self.Translate("Try again."), 10);
                            this.CreatePaddles(self, 200, 30, 20);
                            leftPdl.movementSpeed += 1f;
                        }
                    }
                    if (self.oracle.ID == Oracle.OracleID.SL && !waterMoonReacted && ball.pos.y < minY + 80) {
                        waterMoonReacted = true;
                        dialogBox.Interrupt(self.Translate("Sorry, the game has become a bit difficult now that there's water in this room."), 10);
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


        private class PongAI
        {
            public Pong game;
            private bool invert; //if true, paddle is on left side of screen


            public PongAI(Pong game, bool leftSide)
            {
                this.game = game;
                this.invert = leftSide;
            }


            private float predY;
            public float randomOffsY;
            private bool newRandomOffsY;
            public int PebblesAI()
            {
                //if (false) //player controlled
                //    return self.player.input[0].y;

                PongPaddle paddle = invert ? game.leftPdl : game.rightPdl;
                const int deadband = 5; //avoids paddle oscillation
                bool once = false;

                //https://stackoverflow.com/questions/61139016/how-to-predict-trajectory-of-ball-in-a-ping-pong-game-for-ai-paddle-prediction
                if (game.gameCounter % game.pebblesUpdateRate == 0) //only execute every <pebblesUpdateRate> frames
                {
                    if (invert ^ game.ball.velocityX > 0) //if ball moves towards paddle
                    {
                        //height ball position area
                        float mHeight = game.lenY - (2 * game.ball.radius);

                        //deltaY per X
                        float slope = game.ball.velocityY / game.ball.velocityX;
                        if (float.IsInfinity(slope) || float.IsNegativeInfinity(slope))
                            slope = 0; //should never run with ballBounceAngle applied

                        //distance towards paddle
                        float deltaX = paddle.pos.x - game.ball.pos.x;
                        float objRadius = paddle.width / 2 + game.ball.radius;
                        deltaX = invert ? deltaX + objRadius : deltaX - objRadius;

                        //predict intercept point without walls
                        float intercept = Math.Abs((game.ball.pos.y - game.ball.minY - game.ball.radius) + (deltaX * slope));
                        predY = (intercept % (2 * mHeight));

                        //remove walls and ballradius
                        if (predY > (mHeight))
                            predY = (2 * mHeight) - predY;
                        predY += game.ball.minY + game.ball.radius;

                        newRandomOffsY = true;
                    }
                    else
                    { //ball moves away from paddle
                        predY = game.midY;
                        if (newRandomOffsY && UnityEngine.Random.value < 0.7f)
                            once = true;
                        newRandomOffsY = false;
                    }
                }

                //at random interval a random offset from predicted ball intercept
                if (once || UnityEngine.Random.value < 0.002f) {
                    once = false;
                    float allowed = (paddle.height / 2) - deadband;
                    randomOffsY = allowed * UnityEngine.Random.Range(-1f, 1f);
                }

                if (predY + randomOffsY > paddle.pos.y + deadband)
                    return 1;
                if (predY + randomOffsY < paddle.pos.y - deadband)
                    return -1;
                return 0;
            }


            public float moonDifficulty = 0.7f;
            private bool moonInputDisabled;
            private int moonDelay;
            public int MoonAI()
            {
                int input = PebblesAI();

                if (game.state == State.PlayerWin && moonDifficulty < 1f)
                    moonDifficulty += 0.1f;
                if (game.state == State.PebblesWin && moonDifficulty > 0.3f)
                    moonDifficulty -= 0.12f;
                moonDifficulty = Mathf.Clamp(moonDifficulty, 0.3f, 1f);
                if (game.state == State.PlayerWin || game.state == State.PebblesWin)
                    Plugin.ME.Logger_p.LogInfo("New moonDifficulty: " + moonDifficulty);

                if (game.gameCounter % 15 == 0) {
                    moonInputDisabled = (UnityEngine.Random.value > moonDifficulty);
                    if (game.ball.velocityX < 0) //ball moves away from puppet
                        moonDelay = 15 - (int)(15 * moonDifficulty);
                }

                if (game.ball.velocityX > 0 && moonDelay > 0) //ball moves towards puppet
                    moonDelay--;
                if ((game.ball.velocityX > 0 && moonDelay > 0) || moonInputDisabled)
                    input = 0;

                return input;
            }
        }
    }
}
