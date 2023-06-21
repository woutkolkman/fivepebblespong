using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyPebbles
{
    public class FlappyPebbles : FivePebblesPong.FPGame
    {
        const float POS_OFFSET_SPEED = 16; //keep up with fast bird by altering getTo position
        public List<Pipe> pipes;
        public bool started = false;
        private bool prevInput = false;
        public float velocity, jumpStartV = 12f, gravityV = 1.5f;
        public int pipeInterval = 80, startInterval = 160;
        public FivePebblesPong.Dot bird;

        //dimensions
        private Texture2D rect, line;
        private Vector2 rectSize = new Vector2(100, 30);
        public int pipeHeight = 110;

        //scoreboard
        public FivePebblesPong.PearlSelection scoreBoard;
        public List<Vector2> scoreCount;
        public const float SCORE_HEIGHT = 135;


        public FlappyPebbles(SSOracleBehavior self) : base(self)
        {
            //prevent using a lot of memory by drawing a rect & line once
            rect = FivePebblesPong.CreateGamePNGs.DrawRectangle((int) rectSize.x, (int) rectSize.y, 2, Color.green);
            line = FivePebblesPong.CreateGamePNGs.DrawPerpendicularLine(false, lenY, 2, Color.green);

            base.maxY += 15;
            base.maxX += 200;
            base.minX -= 200;
            this.pipes = new List<Pipe>();
            bird = new FivePebblesPong.Dot(self, this, 4, "FPP_PebblesPoint");

            scoreBoard = new FivePebblesPong.PearlSelection(self as SSOracleBehavior);
            scoreCount = new List<Vector2>();
            Reset();
        }


        ~FlappyPebbles() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            bird?.Destroy();
            for (int i = 0; i < pipes.Count; i++)
                pipes[i]?.Destroy();
            pipes?.Clear();
            this.scoreCount.Clear();
            this.scoreBoard?.Destroy();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            //room and behavior
            palette = started ? 25 : 26;
            self.movementBehavior = started ? FivePebblesPong.Enums.SSPlayGame : SSOracleBehavior.MovementBehavior.Talk;
            if (bird.image != null)
                bird.image.alpha = started ? 0f : 1f;

            //update score
            scoreBoard?.Update(self, scoreCount);

            //read player input
            int pY = p?.input[0].y ?? 0;

            bool jump = false;
            bool input = pY != 0;

            //start/stop game and check input
            if (!prevInput && input) {
                jump = true;
                if (!started && base.gameCounter > 40) {
                    Reset();
                    started = true;
                }
            }
            prevInput = input;
            if (!started)
                return;

            //positioning and hitboxes
            if (jump) {
                velocity = jumpStartV;
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);
            }
            bird.pos.y += velocity;
            velocity -= gravityV;

            //death & points
            bool dead = bird.pos.y > maxY || bird.pos.y < minY;
            for (int i = 0; i < pipes.Count; i++) {
                if (pipes[i] == null)
                    continue;
                dead |= pipes[i].Update(self, bird.pos);

                //pipe passed?
                if (pipes[i].passed || pipes[i].pos.x > bird.pos.x)
                    continue;
                pipes[i].passed = true;
                scoreCount.Add(new Vector2(midX + 50 + 15 * (scoreCount.Count % 10), SCORE_HEIGHT + 15 * (scoreCount.Count / 10)));
                self.oracle.room.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, self.oracle.firstChunk);
                self.oracle.room.PlaySound(SoundID.Mouse_Light_Flicker, self.oracle.firstChunk);

                if (scoreCount.Count >= scoreBoard?.pearls?.Count)
                    scoreBoard.RefreshPearlsInRoom(self);
                if (scoreCount.Count >= scoreBoard?.pearls?.Count) {
                    if (base.gameCounter < startInterval)
                        scoreCount.Clear();
                    base.gameCounter = 0; //short period of no pipes
                }
            }
            if (dead) {
                started = false;
                self.oracle.room.PlaySound(SoundID.HUD_Game_Over_Prompt, self.oracle.firstChunk);
                base.gameCounter = 0;
            }

            //delete pipe if it left the screen
            for (int i = 0; i < pipes.Count; i++) {
                if (pipes[i]?.pos.x < minX) {
                    pipes[i]?.Destroy();
                    pipes.RemoveAt(i);
                }
            }

            //placing pipes
            if (gameCounter >= startInterval && gameCounter % pipeInterval == 0)
                pipes.Add(new Pipe(self, this, rect, line, rectSize, height: pipeHeight));

            //pebbles puppet
            self.lookPoint = new Vector2(maxX, midY);
            self.SetNewDestination(bird.pos); //moves handle closer occasionally
            self.currentGetTo = bird.pos;
            if (base.gameCounter > 60 || pipes.Count > 0) //after pebbles reached the position
                self.oracle.firstChunk.pos = bird.pos;
            self.currentGetTo.y += velocity * POS_OFFSET_SPEED; //keep up with fast bird
            self.floatyMovement = false;
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            bird.DrawImage(offset);

            for (int i = 0; i < pipes.Count; i++)
                pipes[i]?.DrawImage(offset);
        }


        public void Reset()
        {
            for (int i = 0; i < pipes.Count; i++)
                pipes[i]?.Destroy();
            pipes?.Clear();
            bird.pos = new Vector2(minX + lenX / 3, midY);
            base.gameCounter = 0;
            this.scoreCount.Clear();
        }
    }
}
