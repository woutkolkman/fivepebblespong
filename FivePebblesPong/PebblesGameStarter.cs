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
        public static bool controllerInStomachReacted, controllerThrownReacted;

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
        public bool gravity = true;


        public PebblesGameStarter() { }
        ~PebblesGameStarter() //destructor
        {
            game?.Destroy();
            menu?.Destroy();
        }


        //for ShowMediaMovementBehavior
        public static bool pebblesCalibratedProjector;
        public int showMediaCounter = 0;
        public ShowMediaMovementBehavior calibrate = new ShowMediaMovementBehavior();


        //for palette
        public int defaultPalette = 25; //25 is active when slugcat is present, 26 is active while working
        public int previousPalette;
        public float fadePalette = 0f;


        public void StateMachine(SSOracleBehavior self)
        {
            //check if slugcat is holding a gamecontroller
            Player p = FivePebblesPong.GetPlayer(self);

            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.Stop: //main game conversation is running
                    if (p?.room?.roomSettings != null /*player carries controller*/ && p.room.roomSettings.name.Equals("SS_AI") && PebblesGameStarter.pebblesNotFullyStartedCounter < 4)
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
                        //only occurs during Gourmand, hide current ProjectedImage out of sight when starting game
                        if (self.currSubBehavior is SSOracleBehavior.SSOracleMeetGourmand && (self.currSubBehavior as SSOracleBehavior.SSOracleMeetGourmand).showImage != null)
                            (self.currSubBehavior as SSOracleBehavior.SSOracleMeetGourmand).showImage.pos = new Vector2(-250, 0);
                    }
                    if (p == null)
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

                    if (menu != null) {
                        menu.Update(self);
                        game = FivePebblesPong.GetNewFPGame(self, menu.pearlGrabbed);
                        if (menu.gameCounter == 2000)
                            self.dialogBox.Interrupt(self.Translate("You may pick one."), 10);
                    }

                    if (game != null)
                        state = State.Calibrate;
                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.Equals("SS_AI"))
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
                        //finish calibration after X frames
                        bool finish = false;
                        showMediaCounter++;
                        if (showMediaCounter > 100)
                            finish = true;

                        //run animation, true ==> target location reached, "projector" is calibrated
                        if (calibrate.Update(self, new Vector2(game.midX, game.midY), finish))
                        {
                            PebblesGameStarter.pebblesCalibratedProjector = true;
                            showMediaCounter = 0;
                        }
                    }

                    if (PebblesGameStarter.pebblesCalibratedProjector)
                        state = State.Started;
                    game?.Draw(calibrate.showMediaPos - new Vector2(game.midX, game.midY));
                    if (p == null)
                        state = State.StopDialog;
                    break;

                //======================================================
                case State.Started:
                    self.movementBehavior = Enums.PlayGame;

                    game?.Update(self);
                    game?.Draw();

                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.Equals("SS_AI"))
                        state = State.StopDialog;
                    break;

                //======================================================
                case State.StopDialog:
                    if (state != statePreviousRun)
                    {
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        game?.Destroy();
                        game = null;

                        //if controller was thrown, custom dialog will start
                        bool prevControllerThrownReacted = controllerThrownReacted;
                        if (!controllerThrownReacted)
                            for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++)
                                for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++)
                                    if (self.oracle.room.physicalObjects[i][j] is GameController && (self.oracle.room.physicalObjects[i][j] as GameController).thrownBy is Player)
                                        controllerThrownReacted = true;

                        if (!controllerInStomachReacted && self.player?.objectInStomach != null && (self.player.objectInStomach.type == Enums.GameControllerPebbles || self.player.objectInStomach.type == Enums.GameControllerMoon))
                        {
                            //NOTE checks only singleplayer: "self.player"
                            self.dialogBox.Interrupt(self.Translate(UnityEngine.Random.value < 0.5f ? "It's yours now, please keep it." : "That's also not edible."), 10);
                            controllerInStomachReacted = true;
                            if (statePreviousRun == State.StartDialog || statePreviousRun == State.SelectGame)
                                PebblesGameStarter.pebblesNotFullyStartedCounter++;
                        }
                        else if (statePreviousRun == State.StartDialog || statePreviousRun == State.SelectGame)
                        {
                            if (controllerThrownReacted && !prevControllerThrownReacted) {
                                self.dialogBox.Interrupt(self.Translate("I think you've dropped something..."), 10);
                            } else {
                                switch (PebblesGameStarter.pebblesNotFullyStartedCounter)
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("I will take that as a no."), 10); break;
                                    case 2: self.dialogBox.Interrupt(self.Translate("You are not very decisive."), 10); break;
                                }
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
                self.action = Enums.Gaming_Gaming;
                self.conversation.paused = true;
                self.restartConversationAfterCurrentDialoge = false;
            }
            statePreviousRun = stateBeforeRun;

            //change palette
            if (game != null && game.palette >= 0) {
                if (fadePalette < 1f) fadePalette += 0.05f;
                if (fadePalette > 1f) fadePalette = 1f;
                previousPalette = game.palette;
            } else {
                if (fadePalette > 0f) fadePalette -= 0.05f;
                if (fadePalette < 0f) fadePalette = 0f;
            }
            for (int n = 0; n < self.oracle.room.game.cameras.Length; n++)
                if (self.oracle.room.game.cameras[n].room == self.oracle.room && !self.oracle.room.game.cameras[n].AboutToSwitchRoom)
                    self.oracle.room.game.cameras[n].ChangeBothPalettes(defaultPalette, previousPalette, fadePalette);
        }
    }
}
