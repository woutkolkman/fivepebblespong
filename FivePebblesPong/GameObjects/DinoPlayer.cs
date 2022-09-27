using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class DinoPlayer : FPGameObject
    {
        public int width, height, ground;
        public float velocity;


        public DinoPlayer(OracleBehavior self, Color color, string imageName, bool reloadImg = false) : base(imageName)
        {
            this.width = 10; //20;
            this.height = 22;

            //TODO dino walkcycle without sound? also dino death image?
            base.SetImage(self, CreateGamePNGs.DrawDino(color), reloadImg);
        }


        ~DinoPlayer() //destructor
        {
            base.Destroy(); //if not done already
        }


        bool onGround => (pos.y - height/2 <= ground + 0.001f);
        public void Update(int input)
        {
            if (input > 0 && onGround)
                velocity = 10f;

            pos.y += velocity;

            if (onGround) {
                pos.y = ground + height/2;
            } else {
                velocity -= 1f;
            }
        }
    }
}
