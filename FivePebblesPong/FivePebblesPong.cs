﻿using BepInEx;
using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    //TODO multiplayer support???

    public static class EnumExt_FPP //dependency: EnumExtender.dll
    {
        //type for spawning controller
        public static AbstractPhysicalObject.AbstractObjectType GameController; //needs to be first in list

        //five pebbles action
        public static SSOracleBehavior.Action Gaming_Gaming;

        //five pebbles movement (controlled by FPGame subclass)
        public static SSOracleBehavior.MovementBehavior PlayGame;
    }


    [BepInPlugin("author.my_mod_id", "FivePebblesPong", "0.1.0")]	// (GUID, mod name, mod version)
    public class FivePebblesPong : BaseUnityPlugin
    {
        //for accessing logger https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference __me; //WeakReference still allows garbage collection
        public FivePebblesPong() { __me = new WeakReference(this); }
        public static FivePebblesPong ME => __me?.Target as FivePebblesPong;
        public BepInEx.Logging.ManualLogSource Logger_p => Logger;

        public static bool HasEnumExt => (int)EnumExt_FPP.GameController > 0; //returns true after EnumExtender initializes


        //called when mod is loaded, subscribe functions to methods of the game
        public void OnEnable()
        {
            Hooks.Apply();
            Logger.LogInfo("OnEnable()"); //TODO remove
        }


        static SSOracleBehavior.Action PreviousAction; //five pebbles action (from main game) before carrying gamecontroller
        private static FPGame Game;
        public enum State
        {
            Stop,
            StartDialog,
            SelectGame,
            Started,
            StopDialog
        }
        public static State state { get; set; }
        public static State statePreviousRun = State.Stop;
        public static int notFullyStartedCounter = 0; //TODO, value is not reset after player respawns; make state machine not static, but as a class?
        public static PearlSelection menu;


        public static void StateMachine(SSOracleBehavior self, bool CarriesController)
        {
            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.Stop: //main game conversation is running
                    if (CarriesController && notFullyStartedCounter < 4)
                        state = State.StartDialog;
                    break;

                //======================================================
                case State.StartDialog:
                    if (statePreviousRun != state)
                    {
                        switch (notFullyStartedCounter)
                        {
                            case 0: self.dialogBox.Interrupt(self.Translate("Well, a little game shouldn't hurt."), 10); break;
                            case 1: self.dialogBox.Interrupt(self.Translate("Have you made up your mind?"), 10); break;
                            case 2: self.dialogBox.Interrupt(self.Translate("I hope you're not mocking."), 10); break;
                            case 3:
                                self.dialogBox.Interrupt(self.Translate("I'll ignore that."), 10);
                                notFullyStartedCounter++;
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

                    int amountOfGames = 2; //increase counter when adding more games
                    if (menu != null)
                        menu.pearlGrabbed %= amountOfGames;
                    if (menu != null) {
                        switch (menu.pearlGrabbed)
                        {
                            case 0: Game = new Pong(self); break;
                            case 1: break; //TODO add bricks game
                            //add new FPGames here
                        }
                    }

                    if (Game != null)
                        state = State.Started;
                    if (!CarriesController)
                        state = State.StopDialog;
                    if (state != State.SelectGame) {
                        menu?.Destroy();
                        menu = null;
                    }
                    break;

                //======================================================
                case State.Started:
                    self.movementBehavior = EnumExt_FPP.PlayGame;

                    Game?.Update(self);

                    if (!CarriesController)
                        state = State.StopDialog;
                    break;

                //======================================================
                case State.StopDialog:
                    if (state != statePreviousRun)
                    {
                        Game?.Destroy();
                        Game = null;

                        if (statePreviousRun == State.StartDialog || statePreviousRun == State.SelectGame) {
                            switch (UnityEngine.Random.Range(0, 3))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("You are not very decisive..."), 10); break;
                                case 1: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                                case 2: self.dialogBox.Interrupt(self.Translate("I'll take that as a no."), 10); break;
                                case 3: self.dialogBox.Interrupt(self.Translate("I think you've dropped something..."), 10); break;
                            }
                            notFullyStartedCounter++;
                        } else {
                            self.dialogBox.Interrupt(self.Translate("Ok, where was I?"), 10);
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
    }
}
