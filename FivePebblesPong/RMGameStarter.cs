using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class RMGameStarter
    {
        public static RMGameStarter starter; //object gets created when player is in room
        public FPGame game;

        public enum State
        {
            Stop,
            StartDialog,
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
            Player p = FivePebblesPong.GetPlayer(self);

            Vector2 playAreaStart = new Vector2(1220, 800);
            Vector2 playAreaEnd = new Vector2(1800, 1380);
            Vector2 loc = p?.DangerPos != null ? p.DangerPos : new Vector2();
            bool playerLeft = loc.x < playAreaStart.x || loc.x > playAreaEnd.x || loc.y < playAreaStart.y || loc.y > playAreaEnd.y;

            //TODO test without deathPersistentSaveData.theMark (doesn't normally take place in campaigns)

            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.Stop:
                    //player not in room, or no controller
                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.Equals("RM_AI") || playerLeft)
                        break;

                    //conversation active
                    if (self.conversation != null) //if leaving too early, conversation might not become null (RW bug)
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
                        self.dialogBox.Interrupt(self.Translate("Sure, let's play a game."), 10);
                    if (p == null || playerLeft)
                        state = State.StopDialog;
                    if (!self.dialogBox.ShowingAMessage) //dialog finished
                        state = State.Started;
                    break;

                //======================================================
                case State.Started:
                    if (game == null)
                        game = new Pong(self);

                    game?.Update(self);
                    game?.Draw();

                    if (p?.room?.roomSettings == null || !p.room.roomSettings.name.Equals("RM_AI") || playerLeft)
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
                            switch (UnityEngine.Random.Range(0, 1))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("Don't want to? That's ok."), 10); break;
                            }
                        }
                        else
                        {
                            switch (UnityEngine.Random.Range(0, 1))
                            {
                                case 0: self.dialogBox.Interrupt(self.Translate("Thanks for playing. But again, you should leave.<LINE>Leave quickly while you still can."), 10); break;
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
