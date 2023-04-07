using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class RMGameStarter
    {
        public static RMGameStarter starter; //object gets created when player is in room
        public FPGame game;
        public static bool foundControllerReacted = false;
        public static bool startedProjector;
        bool playerLeft;

        public enum State
        {
            Stop,
            StartDialog,
            StartProjector,
            Started,
            StopDialog
        }
        public State state { get; set; }
        public State statePreviousRun = State.Stop;


        public RMGameStarter() { }
        ~RMGameStarter()
        {
            this.game?.Destroy();
        }


        public void StateMachine(MoreSlugcats.SSOracleRotBehavior self)
        {
            //check if slugcat is holding a gamecontroller
            Player p = Plugin.GetPlayer(self);

            //check if player is in front of projector/in the can
            Vector2 playAreaStart = new Vector2(1220, 800);
            Vector2 playAreaEnd = new Vector2(1800, 1380);
            if (p?.DangerPos != null)
                playerLeft = p.DangerPos.x < playAreaStart.x || p.DangerPos.x > playAreaEnd.x || p.DangerPos.y < playAreaStart.y || p.DangerPos.y > playAreaEnd.y;

            //get story progression
            bool rivTookCell = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken;
            bool rivEndgame = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.pebblesRivuletPostgame;
            bool moonHeartRestored = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.moonHeartRestored;
            int conversationsHad = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;
            int energySeenState = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.energySeenState; //1: cell not taken, 2: cell taken beforehand but not shown, 3: cell shown

            //TODO test without deathPersistentSaveData.theMark (doesn't normally take place in campaigns)

            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.Stop:
                    //player not in room, or no controller
                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.StartsWith("RM_AI") || playerLeft)
                        break;

                    //conversation active
                    if (self.conversation != null) //if leaving too early and comming back, conversation might not become null (RW bug)
                        break;
                    if (self.dialogBox != null && self.dialogBox.ShowingAMessage)
                        break;

                    //pebbles busy
                    if (self.holdingObject != null)
                        break;

                    //hasn't seen player yet
                    if (self.player == null || !self.hasNoticedPlayer)
                        break;

                    state = State.StartDialog;
                    break;

                //======================================================
                case State.StartDialog:
                    if (statePreviousRun != state)
                    {
                        if (!foundControllerReacted) {
                            foundControllerReacted = true;
                            self.dialogBox.Interrupt(self.Translate("I see you've found a controller. Let's check if it still works."), 10);
                        } else {
                            switch (UnityEngine.Random.Range(0, 3))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("Sure, let's play a game."), 10); break;
                                case 1: self.dialogBox.Interrupt(self.Translate("Sure, I can make some time."), 10); break;
                                case 2: self.dialogBox.Interrupt(self.Translate("A game is fine. I don't have a lot to occupy my time with anyway..."), 10); break;
                            }
                        }
                    }
                    if (p == null || playerLeft)
                        state = State.StopDialog;
                    if (!self.dialogBox.ShowingAMessage) //dialog finished
                        state = startedProjector ? State.Started : State.StartProjector;
                    break;

                //======================================================
                case State.StartProjector:
                    if (statePreviousRun != state) {
                        switch (UnityEngine.Random.Range(0, 3))
                        {
                            case 0: self.dialogBox.NewMessage(self.Translate("One moment. Let me turn the projector back on."), 10); break;
                            case 1: self.dialogBox.NewMessage(self.Translate("One moment. ...I hope you don't have epilepsy."), 10); break;
                            case 2: self.dialogBox.NewMessage(self.Translate("One moment. The projector is having some difficulties."), 10); break;
                        }
                        startedProjector = true;
                    }

                    self.lookPoint = self.OracleGetToPos; //look at projector
                    if (!self.dialogBox.ShowingAMessage) //dialog finished
                        state = State.Started;
                    break;

                //======================================================
                case State.Started:
                    if (game == null)
                        game = new Pong(self);

                    //flash images
                    Vector2 pos = new Vector2(UnityEngine.Random.value < 0.5f ? 1000 : -1000, UnityEngine.Random.value < 0.5f ? 1000 : -1000);

                    game?.Update(self);
                    game?.Draw(UnityEngine.Random.value < 0.1f ? pos : new Vector2());

                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.StartsWith("RM_AI") || playerLeft)
                        state = State.StopDialog;
                    break;

                //======================================================
                case State.StopDialog:
                    if (state != statePreviousRun)
                    {
                        game?.Destroy();
                        game = null;
                        if (statePreviousRun == State.StartDialog)
                        {
                            if (playerLeft) {
                                self.dialogBox.Interrupt(self.Translate("Yes, you'd better go. For your own sake."), 10); break;
                            } else {
                                switch (UnityEngine.Random.Range(0, 3))
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("I will take that as a no."), 10); break;
                                    case 2: self.dialogBox.Interrupt(self.Translate("I see you've changed your mind."), 10); break;
                                }
                            }
                        }
                        else
                        {
                            if (!rivTookCell)
                            {
                                switch (UnityEngine.Random.Range(0, 3))
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Follow one of my overseers, it will show you where the energy rail is."), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("An overseer will catch up to you, and show you where the mass rarefaction cell is."), 10); break;
                                    case 2: self.dialogBox.Interrupt(self.Translate("Please follow one of my overseers to the energy rail.<LINE>Pay close attention, and it will show you where to go."), 10); break;
                                }
                            }
                            else if (self.CheckEnergyCellPresence())
                            {
                                switch (UnityEngine.Random.Range(0, 3))
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Don't forget to bring the cell to Looks to the Moon."), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("The mass rarefaction cell, please take it to the structure in the far east.<LINE>It might still be of value there."), 10); break;
                                    case 2: self.dialogBox.Interrupt(self.Translate("Take that rarefaction cell with you to the far east.<LINE>Then for your own sake, never return here."), 10); break;
                                }
                            }
                            else if (rivTookCell && energySeenState == 3)
                            {
                                switch (UnityEngine.Random.Range(0, 3))
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Have you got the cell to Looks to the Moon?"), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("Have you been to Looks to the Moon yet?"), 10); break;
                                    case 2: self.dialogBox.Interrupt(self.Translate("The energy rail has shut down. Dit you get the cell to it's new owner?<LINE>...if she is somehow still alive."), 10); break;
                                }
                            }
                            else
                            {
                                switch (UnityEngine.Random.Range(0, 2))
                                {
                                    case 0: self.dialogBox.Interrupt(self.Translate("Thanks for playing. But again, you should leave.<LINE>Leave quickly while you still can."), 10); break;
                                    case 1: self.dialogBox.Interrupt(self.Translate("This structure is in a constant state of decay.<LINE>Please find a way out little creature."), 10); break;
                                }
                            }
                        }
                    }

                    if (!self.dialogBox.ShowingAMessage)
                        state = State.Stop;
                    break;
            }
            statePreviousRun = stateBeforeRun;
        }
    }
}
