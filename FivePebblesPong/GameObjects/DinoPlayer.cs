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
        public float velocity, jumpStartV, gravityV;
        public Color color { get; }
        public enum Animation
        {
            Standing,
            Walking,
            Ducking,
            Dead
        }
        public Animation curAnim;


        public DinoPlayer(OracleBehavior self, Color color, string imageName) : base(imageName)
        {
            this.width = 10; //20;
            this.height = 22; //frequently overwritten
            this.jumpStartV = 8f;
            this.gravityV = 0.8f;

            this.color = color;
            SetImage(self);

            SetAnimation(Animation.Standing);
        }


        ~DinoPlayer() //destructor
        {
            base.Destroy(); //if not done already
        }


        public void SetImage(OracleBehavior self)
        {
            List<Texture2D> textures = new List<Texture2D>() {
                CreateGamePNGs.DrawDino(this.color),                 //imageName
                CreateGamePNGs.DrawDino(this.color, walk: 1),        //imageName + 1
                CreateGamePNGs.DrawDino(this.color, walk: 2),        //imageName + 2
                CreateGamePNGs.DrawDinoDucking(this.color, 0),       //imageName + 3
                CreateGamePNGs.DrawDinoDucking(this.color, 1),       //imageName + 4
                CreateGamePNGs.DrawDino(this.color, shocked: true)   //imageName + 5
            };
            base.SetImage(self, textures, 15, false);
        }


        bool onGround => (pos.y - height/2 <= ground + 0.001f);
        int prevInput;
        public void Update(int input)
        {
            if (input > 0 && onGround)
                velocity = jumpStartV;

            pos.y += velocity;

            if (input != prevInput)
                SetAnimation(input < 0 ? Animation.Ducking : Animation.Walking);
            prevInput = input;

            if (onGround) {
                pos.y = ground + height/2;
                velocity = 0f;
            } else {
                velocity -= gravityV;
            }
        }


        public void SetAnimation(Animation a)
        {
            height = 22;
            switch(a) {
                case (Animation.Standing):
                    image.imageNames = new List<string> { imageName };
                    image.currImg = 0;
                    break;

                case (Animation.Walking):
                    image.imageNames = new List<string> { imageName + "1", imageName + "2" };
                    break;

                case (Animation.Ducking):
                    image.imageNames = new List<string> { imageName + "3", imageName + "4" };
                    height = 12;
                    break;

                case (Animation.Dead):
                    image.imageNames = new List<string> { imageName + "5" };
                    image.currImg = 0;
                    break;
            }
            curAnim = a;
        }
    }
}
