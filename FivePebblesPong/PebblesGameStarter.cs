using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class PebblesGameStarter
    {
        //start/stop FPGames via state machine
        public static PebblesGameStarter starter; //object gets created when player is holding gamecontroller in pebbles room
        public static int pebblesNotFullyStartedCounter;
        public static bool pebblesCalibratedProjector;
        public static bool controllerInStomachReacted;

        public SSOracleBehavior.Action PreviousAction; //five pebbles action (from main game) before carrying gamecontroller
        public FPGame game;
        public enum State
        {
            Stop,
            StartDialog,
            SelectGame,
            Calibrate,
            Started,
            StopDialog
        }
        public State state { get; set; }
        public State statePreviousRun = State.Stop;
        public PearlSelection menu;


        public PebblesGameStarter() { }
        ~PebblesGameStarter() //destructor
        {
            game?.Destroy();
            menu?.Destroy();
        }


        //for ShowMediaMovementBehavior
        public int consistentShowMediaPosCounter = 0;
        public Vector2 showMediaPos = new Vector2();
        public Vector2 idealShowMediaPos = new Vector2();
        public int showMediaCounter = 0;


        public void StateMachine(SSOracleBehavior self, bool CarriesController)
        {
            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.Stop: //main game conversation is running
                    if (CarriesController && self.player.room.roomSettings.name.Equals("SS_AI") && PebblesGameStarter.pebblesNotFullyStartedCounter < 4)
                        state = State.StartDialog;
                    break;

                //======================================================
                case State.StartDialog:
                    if (statePreviousRun != state)
                    {
                        switch (PebblesGameStarter.pebblesNotFullyStartedCounter)
                        {
                            case 0: self.dialogBox.Interrupt(self.Translate(UnityEngine.Random.value < 0.5f ? "Well, a little game shouldn't hurt." : "Fine, I needed a break."), 10); break;
                            case 1: self.dialogBox.Interrupt(self.Translate("Have you made up your mind?"), 10); break;
                            case 2: self.dialogBox.Interrupt(self.Translate("You're just playing with that, aren't you.."), 10); break;
                            case 3:
                                self.dialogBox.Interrupt(self.Translate("I'll ignore that."), 10);
                                PebblesGameStarter.pebblesNotFullyStartedCounter++;
                                state = State.Stop;
                                break;
                        }
                    }
                    if (!CarriesController)
                        state = State.StopDialog;
                    if (!self.dialogBox.ShowingAMessage) //dialog finished
                        state = State.SelectGame;
                    break;

                //======================================================
                case State.SelectGame:
                    if (statePreviousRun != state)
                    {
                        menu = new PearlSelection(self);
                        self.dialogBox.Interrupt(self.Translate("Pick one."), 10);
                    }
                    menu?.Update(self);

                    if (menu != null) {
                        game = FivePebblesPong.GetNewFPGame(self, menu.pearlGrabbed);
                        if (menu.gameCounter == 2000)
                            self.dialogBox.Interrupt(self.Translate("You may pick one."), 10);
                    }

                    if (game != null)
                        state = State.Calibrate;
                    if (!CarriesController || !self.player.room.roomSettings.name.Equals("SS_AI"))
                        state = State.StopDialog;
                    if (state != State.SelectGame)
                    {
                        menu?.Destroy();
                        menu = null;
                    }
                    break;

                //======================================================
                case State.Calibrate: //calibratedProjector should be false if calibration should run
                    if (state != statePreviousRun)
                        game?.Update(self); //update once to optionally spawn game objects
                    if (!PebblesGameStarter.pebblesCalibratedProjector && game != null)
                    {
                        //at random intervals, recalibrate "projector"
                        if (UnityEngine.Random.value < 0.033333335f)
                        {
                            idealShowMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                            showMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                        }

                        //finish calibration after X frames
                        bool finish = false;
                        showMediaCounter++;
                        if (showMediaCounter > 100)
                        {
                            finish = true;
                            idealShowMediaPos = new Vector2(game.midX, game.midY);
                        }

                        ShowMediaMovementBehavior(self, ref consistentShowMediaPosCounter, ref showMediaPos, ref idealShowMediaPos, finish);

                        //target location reached, "projector" is calibrated
                        if (finish && showMediaPos == new Vector2(game.midX, game.midY))
                        {
                            PebblesGameStarter.pebblesCalibratedProjector = true;
                            showMediaCounter = 0;
                        }
                    }

                    if (PebblesGameStarter.pebblesCalibratedProjector)
                        state = State.Started;
                    game?.Draw(showMediaPos - new Vector2(game.midX, game.midY));
                    if (!CarriesController)
                        state = State.StopDialog;
                    break;

                //======================================================
                case State.Started:
                    self.movementBehavior = EnumExt_FPP.PlayGame;

                    game?.Update(self);
                    game?.Draw();

                    if (!CarriesController || !self.player.room.roomSettings.name.Equals("SS_AI"))
                        state = State.StopDialog;
                    break;

                //======================================================
                case State.StopDialog:
                    if (state != statePreviousRun)
                    {
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        game?.Destroy();
                        game = null;

                        if (!controllerInStomachReacted && self.player.objectInStomach != null && self.player.objectInStomach.type == EnumExt_FPP.GameController)
                        {
                            self.dialogBox.Interrupt(self.Translate(UnityEngine.Random.value < 0.5f ? "It's yours now, please keep it." : "That's also not edible."), 10);
                            controllerInStomachReacted = true;
                            if (statePreviousRun == State.StartDialog || statePreviousRun == State.SelectGame)
                                PebblesGameStarter.pebblesNotFullyStartedCounter++;
                        }
                        else if (statePreviousRun == State.StartDialog || statePreviousRun == State.SelectGame)
                        {
                            switch (UnityEngine.Random.Range(0, 3))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("You are not very decisive..."), 10); break;
                                case 1: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                                case 2: self.dialogBox.Interrupt(self.Translate("I will take that as a no."), 10); break;
                                case 3: self.dialogBox.Interrupt(self.Translate("I think you've dropped something..."), 10); break;
                            }
                            PebblesGameStarter.pebblesNotFullyStartedCounter++;
                        }
                        else
                        {
                            switch (UnityEngine.Random.Range(0, 2))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("Ok, where was I?"), 10); break;
                                case 1: self.dialogBox.Interrupt(self.Translate("Now, what was I saying?"), 10); break;
                                case 2: self.dialogBox.Interrupt(self.Translate("Break is over."), 10); break;
                            }
                        }
                    }

                    if (!self.dialogBox.ShowingAMessage)
                        state = State.Stop;
                    break;

                //======================================================
                default:
                    state = State.Stop;
                    break;
            }

            //handle states
            if (state != State.Stop && stateBeforeRun == State.Stop)
                PreviousAction = self.action;
            if (state == State.Stop && state != stateBeforeRun)
            {
                self.action = PreviousAction;
                self.restartConversationAfterCurrentDialoge = true;
            }
            if (state != State.Stop)
            {
                self.action = EnumExt_FPP.Gaming_Gaming;
                self.conversation.paused = true;
                self.restartConversationAfterCurrentDialoge = false;
            }

            statePreviousRun = stateBeforeRun;
        }


        //used for animation, trying different positions for projectedimages (basically copied via dnSpy and made static, no docs, sorry)
        public static void ShowMediaMovementBehavior(OracleBehavior self, ref int consistentShowMediaPosCounter, ref Vector2 showMediaPos, ref Vector2 idealShowMediaPos, bool finish)
        {
            consistentShowMediaPosCounter += (int)Custom.LerpMap(Vector2.Distance(showMediaPos, idealShowMediaPos), 0f, 200f, 1f, 10f);
            Vector2 vector = new Vector2(UnityEngine.Random.value * self.oracle.room.PixelWidth, UnityEngine.Random.value * self.oracle.room.PixelHeight);

            if (!finish && ShowMediaScore(vector) + 40f < ShowMediaScore(idealShowMediaPos)) {
                idealShowMediaPos = vector;
                consistentShowMediaPosCounter = 0;
            }
            vector = idealShowMediaPos + Custom.RNV() * UnityEngine.Random.value * 40f;
            if (!finish && ShowMediaScore(vector) + 20f < ShowMediaScore(idealShowMediaPos)) {
                idealShowMediaPos = vector;
                consistentShowMediaPosCounter = 0;
            }
            if (consistentShowMediaPosCounter > 300 || finish) { //added "finish" to immediately move towards idealShowMediaPos
                showMediaPos = Vector2.Lerp(showMediaPos, idealShowMediaPos, 0.1f);
                showMediaPos = Custom.MoveTowards(showMediaPos, idealShowMediaPos, 10f);
            }

            float ShowMediaScore(Vector2 tryPos)
            {
                if (self.oracle.room.GetTile(tryPos).Solid)
                    return float.MaxValue;
                float num = Mathf.Abs(Vector2.Distance(tryPos, self.player.DangerPos) - 250f);
                num -= Math.Min((float)self.oracle.room.aimap.getAItile(tryPos).terrainProximity, 9f) * 30f;
                if (self is SSOracleBehavior)
                    num -= Vector2.Distance(tryPos, (self as SSOracleBehavior).nextPos) * 0.5f;
                for (int i = 0; i < self.oracle.arm.joints.Length; i++)
                    num -= Mathf.Min(Vector2.Distance(tryPos, self.oracle.arm.joints[i].pos), 100f) * 10f;
                if (self.oracle.graphicsModule != null)
                    for (int j = 0; j < (self.oracle.graphicsModule as OracleGraphics).umbCord.coord.GetLength(0); j += 3)
                        num -= Mathf.Min(Vector2.Distance(tryPos, (self.oracle.graphicsModule as OracleGraphics).umbCord.coord[j, 0]), 100f);
                return num;
            }
        }
    }
}
