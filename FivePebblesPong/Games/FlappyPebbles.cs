using System;
using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class FlappyPebbles : FPGame
    {
        const float POS_OFFSET_SPEED = 13; //keep up with fast paddle by altering getTo position
        public List<PongPaddle> pipes;
        public bool started = false;
        private bool prevInput = false;
        public float velocity, jumpStartV, gravityV;
        public Vector2 pos;
        public Dot dot;


        public FlappyPebbles(SSOracleBehavior self) : base(self)
        {
            base.maxY += 15;
            this.pipes = new List<PongPaddle>();
            this.jumpStartV = 16f;
            this.gravityV = 2.2f;
            this.pos = new Vector2(minX + lenX / 3, midY);
            dot = new Dot(self, this, 4, "FPP_Bird");
        }


        ~FlappyPebbles() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            dot?.Destroy();
            for (int i = 0; i < pipes.Count; i++)
                pipes[i]?.Destroy();
            pipes?.Clear();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            //room and puppet
            palette = started ? 25 : 26;
            self.lookPoint = started ? new Vector2(maxX, midY) : (p?.DangerPos ?? self.player?.DangerPos ?? new Vector2());

            //read player input
            int pY = p?.input[0].y ?? 0;

            bool jump = false;
            bool input = pY != 0;

            //start/stop game and check input
            if (!prevInput && input) {
                jump = true;
                if (!started)
                    Reset();
                started = true;
            }
            prevInput = input;
            if (!started)
                return;

            //positioning and hitboxes
            if (jump) {
                velocity = jumpStartV;
                self.oracle.room.PlaySound(SoundID.MENU_Checkbox_Check, self.oracle.firstChunk);
            }
            pos.y += velocity;
            velocity -= gravityV;
            if (pos.y > maxY || pos.y < minY) {
                started = false;
                self.oracle.room.PlaySound(SoundID.HUD_Game_Over_Prompt, self.oracle.firstChunk);
            }
            dot.pos = pos;
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            dot.DrawImage(offset);

            for (int i = 0; i < pipes.Count; i++)
                pipes[i]?.DrawImage(offset);
        }


        public void Reset()
        {
            for (int i = 0; i < pipes.Count; i++)
                pipes[i]?.Destroy();
            pipes?.Clear();
            pos.y = midY;
            dot.pos = pos;
            base.gameCounter = 0;
        }
    }
}
