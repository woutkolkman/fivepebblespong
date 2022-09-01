using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public abstract class FPGame
    {
        public List<ProjectedImage> Images { get; set; }
        public const int MAX_Y = 600;
        public const int MIN_Y = 100;
        public const int MID_Y = MIN_Y + ((MAX_Y - MIN_Y) / 2);
        public const int MAX_X = 740;
        public const int MIN_X = 240;
        public const int MID_X = MIN_X + ((MAX_X - MIN_X) / 2);


        public FPGame(SSOracleBehavior self) { }


        //to immediately remove images
        public virtual void Destruct()
        {
            for (int i = 0; i < this.Images.Count; i++)
            {
                if (this.Images[i] != null)
                {
                    this.Images[i].Destroy();
                    this.Images[i] = null;
                }
            }
            this.Images.RemoveRange(0, this.Images.Count);
        }


        public virtual void Update(SSOracleBehavior self) { }
        public virtual void Draw(SSOracleBehavior self) { }
    }
}
