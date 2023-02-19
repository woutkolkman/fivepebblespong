using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class SSGameStarter
    {
        //start/stop FPGames via state machine
        public static SSGameStarter starter; //object gets created when player is holding gamecontroller in pebbles room
        public static int notFullyStartedCounter;
        public static bool controllerInStomachReacted, controllerThrownReacted;

        public SSOracleBehavior.Action previousAction; //action (from main game) before carrying gamecontroller
        public SSOracleBehavior.SubBehavior previousSubBehavior; //subbehavior (from main game) before starting game
        public SSOracleBehavior.MovementBehavior previousMovementBehavior; //movementbehavior (from main game) before starting game
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


        public SSGameStarter() { }
        ~SSGameStarter() //destructor
        {
            game?.Destroy();
            menu?.Destroy();
        }


        //for ShowMediaMovementBehavior
        public static bool calibratedProjector;
        public int showMediaCounter = 0;
        public ShowMediaMovementBehavior calibrate = new ShowMediaMovementBehavior();


        //for palette
        public int previousPalette, defaultPalette;
        public float fadePalette = 0f;


        public void StateMachine(SSOracleBehavior self)
        {
            State stateBeforeRun = state;

            if (self.oracle.ID == Oracle.OracleID.SS)
                PebblesStates(self);
            if (self.oracle.ID.ToString().Equals("DM"))
                MoonStates(self);

            //bugfix where state immediately transitions to ThrowOut_KillOnSight in Spearmaster campaign
            bool prevenActionOverride = (self.oracle.ID == Oracle.OracleID.SS && self.player.slugcatStats.name.ToString().Equals("Spear"));

            //handle states
            if (state != State.Stop && stateBeforeRun == State.Stop)
            {
                previousAction = self.action;
                FivePebblesPong.ME.Logger_p.LogInfo("Save " + nameof(self.action) + ": " + self.action.ToString());
                previousSubBehavior = self.currSubBehavior;
                FivePebblesPong.ME.Logger_p.LogInfo("Save " + nameof(self.currSubBehavior) + ": " + self.currSubBehavior.ToString());
                previousMovementBehavior = self.movementBehavior;
                FivePebblesPong.ME.Logger_p.LogInfo("Save " + nameof(self.movementBehavior) + ": " + self.movementBehavior.ToString());
            }
            if (state == State.Stop && state != stateBeforeRun && !prevenActionOverride)
            {
                self.action = previousAction;
                FivePebblesPong.ME.Logger_p.LogInfo("Restore " + nameof(self.action) + ": " + self.action.ToString());
                self.currSubBehavior = previousSubBehavior;
                FivePebblesPong.ME.Logger_p.LogInfo("Restore " + nameof(self.currSubBehavior) + ": " + self.currSubBehavior.ToString());
                self.movementBehavior = previousMovementBehavior;
                FivePebblesPong.ME.Logger_p.LogInfo("Restore " + nameof(self.movementBehavior) + ": " + self.movementBehavior.ToString());
                self.restartConversationAfterCurrentDialoge = true;
            }
            if (state != State.Stop && !prevenActionOverride)
            {
                if (state != stateBeforeRun && self.action != Enums.Gaming_Gaming)
                    FivePebblesPong.ME.Logger_p.LogInfo("Set " + nameof(self.action) + ": " + Enums.Gaming_Gaming.ToString());
                self.action = Enums.Gaming_Gaming;
                if (self.conversation != null)
                    self.conversation.paused = true;
                self.restartConversationAfterCurrentDialoge = false;
            }
            statePreviousRun = stateBeforeRun;

            //change palette
            if (game != null && game.palette >= 0) {
                if (fadePalette < 0.1f && self.oracle?.room?.game?.cameras != null && self.oracle.room.game.cameras.Length > 0)
                    defaultPalette = (self.oracle.room.game.cameras[0].paletteBlend < 0.5f ? self.oracle.room.game.cameras[0].paletteA : self.oracle.room.game.cameras[0].paletteB);
                if (fadePalette < 1f) fadePalette += 0.05f;
                if (fadePalette > 1f) fadePalette = 1f;
                previousPalette = game.palette;
            } else {
                if (fadePalette > 0f) fadePalette -= 0.05f;
                if (fadePalette < 0f) fadePalette = 0f;
            }
            if (self.oracle?.room?.game?.cameras != null && fadePalette > 0.01f)
                for (int n = 0; n < self.oracle.room.game.cameras.Length; n++)
                    if (self.oracle.room.game.cameras[n].room == self.oracle.room && !self.oracle.room.game.cameras[n].AboutToSwitchRoom)
                        self.oracle.room.game.cameras[n].ChangeBothPalettes(defaultPalette, previousPalette, fadePalette);
        }


        private void PebblesStates(SSOracleBehavior self)
        {
            //check if slugcat is holding a gamecontroller
            Player p = FivePebblesPong.GetPlayer(self);

            switch (state)
            {
                //======================================================
                case State.Stop: //main game conversation is running
                    if (p?.room?.roomSettings != null /*player carries controller*/ && p.room.roomSettings.name.Equals("SS_AI") && SSGameStarter.notFullyStartedCounter < 4)
                        state = State.StartDialog;
                    break;

                //======================================================
                case State.StartDialog:
                    if (statePreviousRun != state)
                    {
                        //no Pebbles games during Spearmaster campaign
                        if (self.player.slugcatStats.name.ToString().Equals("Spear"))
                        {
                            self.dialogBox.Interrupt(self.Translate("No games. I am currently very busy."), 10);
                            if (SSGameStarter.notFullyStartedCounter < 4)
                                SSGameStarter.notFullyStartedCounter = 4;
                            state = State.Stop;
                            break;
                        }

                        switch (SSGameStarter.notFullyStartedCounter)
                        {
                            case 0: self.dialogBox.Interrupt(self.Translate(UnityEngine.Random.value < 0.5f ? "Well, a little game shouldn't hurt." : "Fine, I needed a break."), 10); break;
                            case 1: self.dialogBox.Interrupt(self.Translate("Have you made up your mind?"), 10); break;
                            case 2: self.dialogBox.Interrupt(self.Translate("You're just playing with that, aren't you.."), 10); break;
                            case 3:
                                self.dialogBox.Interrupt(self.Translate("I'll ignore that."), 10);
                                SSGameStarter.notFullyStartedCounter++;
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
                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.EndsWith("_AI"))
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
                    if (!SSGameStarter.calibratedProjector && game != null)
                    {
                        //finish calibration after X frames
                        bool finish = false;
                        showMediaCounter++;
                        if (showMediaCounter > 100)
                            finish = true;

                        //run animation, true ==> target location reached, "projector" is calibrated
                        if (calibrate.Update(self, new Vector2(game.midX, game.midY), finish))
                        {
                            SSGameStarter.calibratedProjector = true;
                            showMediaCounter = 0;
                        }
                    }

                    if (SSGameStarter.calibratedProjector)
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

                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.EndsWith("_AI"))
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
                                SSGameStarter.notFullyStartedCounter++;
                        }
                        else if (statePreviousRun == State.StartDialog || statePreviousRun == State.SelectGame)
                        {
                            if (controllerThrownReacted && !prevControllerThrownReacted) {
                                self.dialogBox.Interrupt(self.Translate("I think you've dropped something..."), 10);
                            } else {
                                switch (SSGameStarter.notFullyStartedCounter)
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("I will take that as a no."), 10); break;
                                    case 2: self.dialogBox.Interrupt(self.Translate("You are not very decisive."), 10); break;
                                }
                            }
                            SSGameStarter.notFullyStartedCounter++;
                        }
                        else
                        {
                            switch (UnityEngine.Random.Range(0, 3))
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
        }


        private void MoonStates(SSOracleBehavior self)
        {
            //check if slugcat is holding a gamecontroller
            Player p = FivePebblesPong.GetPlayer(self);

            bool wasAtPebbles = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad > 0;
            bool hasShownPearl = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.smPearlTagged;
            bool broadcasted = self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.altEnding;

            switch (state)
            {
                //======================================================
                case State.Stop: //main game conversation is running
                    if (p?.room?.roomSettings != null /*player carries controller*/ && p.room.roomSettings.name.Equals("DM_AI") && self.action.ToString().Equals("Moon_SlumberParty"))
                        state = State.StartDialog;
                    break;

                //======================================================
                case State.StartDialog:
                    if (statePreviousRun != state)
                    {
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0: self.dialogBox.Interrupt(self.Translate("Ok, one game. But please, hurry to Five Pebbles."), 10); break; //TODO depends on current state in campaign
                            case 1: self.dialogBox.Interrupt(self.Translate("I don't have much time left.<LINE>After this game, please be on your way."), 10); break;
                        }
                    }
                    if (p == null)
                        state = State.StopDialog;
                    if (!self.dialogBox.ShowingAMessage) //dialog finished
                        state = State.SelectGame;
                    break;

                //======================================================
                case State.SelectGame:
                    PebblesStates(self); //pebbles' state is identical
                    break;

                //======================================================
                case State.Calibrate:
                    PebblesStates(self); //pebbles' state is identical
                    break;

                //======================================================
                case State.Started:
                    PebblesStates(self); //pebbles' state is identical
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

                        if (statePreviousRun == State.StartDialog || statePreviousRun == State.SelectGame)
                        {
                            if (controllerThrownReacted && !prevControllerThrownReacted) {
                                self.dialogBox.Interrupt(self.Translate("The device should be durable.<LINE>You do not have to test it."), 10);
                            } else {
                                switch (UnityEngine.Random.Range(0, 3))
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("Ah, don't want to?"), 10); break;
                                    case 2: self.dialogBox.Interrupt(self.Translate("No obligations."), 10); break;
                                }
                            }
                            SSGameStarter.notFullyStartedCounter++;
                        }
                        else
                        {
                            switch (UnityEngine.Random.Range(0, 1)) {
                                case 0: self.UrgeAlong(); break; //TODO depends on state of campaign
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
        }
    }
}
