using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class HRGameStarter
    {
        public static HRGameStarter starter; //object gets created when player is close
        public bool movedToSide = false;
        public FPGame game;

        public enum State
        {
            Stop,
            Started,
            MoveToSide
        }
        public State state { get; set; }
        public State statePreviousRun = State.Stop;


        public HRGameStarter() { }
        ~HRGameStarter()
        {
            this.game?.Destroy();
        }


        //for ShowMediaMovementBehavior
        public static bool calibratedProjector;
        public int showMediaCounter = 0;
        public ShowMediaMovementBehavior calibrate;


        public void StateMachine(SSOracleBehavior self)
        {
            State stateBeforeRun = state;
            switch (state)
            {
                //======================================================
                case State.Stop:
                    game?.Destroy();
                    game = null;

                    if ((self.currSubBehavior as SSOracleBehavior.SSOracleRubicon).noticedPlayer)
                        break;

                    state = State.Started;
                    break;

                //======================================================
                case State.Started:
                    if (game == null)
                        game = Plugin.HRGetNewFPGame(self);

                    self.movementBehavior = Enums.SSPlayGame;

                    game?.Update(self);
                    game?.Draw();

                    if ((self.currSubBehavior as SSOracleBehavior.SSOracleRubicon).noticedPlayer)
                        state = State.MoveToSide;
                    break;

                //======================================================
                case State.MoveToSide:
                    if (game == null)
                        movedToSide = true;
                    if (calibrate == null)
                        calibrate = new ShowMediaMovementBehavior(new Vector2(game?.midX ?? 0, game?.midY ?? 0));
                    if (!movedToSide)
                    {
                        //finish calibration after X frames
                        bool finish = false;
                        showMediaCounter++;
                        if (showMediaCounter > 150)
                            finish = true;

                        //run animation, true ==> target location reached, "projector" is calibrated
                        if (calibrate.Update(self, new Vector2(0, 0), finish))
                        {
                            movedToSide = true;
                            showMediaCounter = 0;
                        }
                    }
                    if (movedToSide)
                        state = State.Stop;
                    game?.Draw(calibrate.showMediaPos - new Vector2(game.midX, game.midY));
                    break;
            }
            statePreviousRun = stateBeforeRun;
        }
    }
}
