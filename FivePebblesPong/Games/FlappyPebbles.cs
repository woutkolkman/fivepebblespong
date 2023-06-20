using System;
using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class FlappyPebbles : FPGame
    {
        const float POS_OFFSET_SPEED = 16; //keep up with fast bird by altering getTo position
        public List<Pipe> pipes;
        public bool started = false;
        private bool prevInput = false;
        public float velocity, jumpStartV = 12f, gravityV = 1.5f;
        public int pipeInterval = 80, startInterval = 160;
        public Dot bird;

        //dimensions
        private Texture2D rect, line;
        private Vector2 rectSize = new Vector2(100, 30);
        public int pipeHeight = 110;


        public FlappyPebbles(SSOracleBehavior self) : base(self)
        {
            //prevent using a lot of memory by drawing a rect & line once
            rect = CreateGamePNGs.DrawRectangle((int) rectSize.x, (int) rectSize.y, 2, Color.green);
            line = CreateGamePNGs.DrawPerpendicularLine(false, lenY, 2, Color.green);

            base.maxY += 15;
            base.maxX += 200;
            base.minX -= 200;
            this.pipes = new List<Pipe>();
            bird = new Dot(self, this, 4, "FPP_PebblesPoint");
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
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            //room and behavior
            palette = started ? 25 : 26;
            self.movementBehavior = started ? Enums.SSPlayGame : SSOracleBehavior.MovementBehavior.Talk;

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

            //death
            bool dead = bird.pos.y > maxY || bird.pos.y < minY;
            for (int i = 0; i < pipes.Count; i++)
                dead |= pipes[i]?.Update(self, bird.pos) ?? false;
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
        }
    }
}
