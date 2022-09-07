using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public abstract class FPGame
    {
        public int maxY, minY, maxX, minX; //playable field
        public int midY => minY + ((maxY - minY) / 2);
        public int midX => minX + ((maxX - minX) / 2);
        public int gameCounter;


        public FPGame(SSOracleBehavior self)
        {
            maxY = 640;
            minY = 60;
            maxX = 780;
            minX = 200;
        }


        //to immediately remove images
        public virtual void Destroy() { }


        public virtual void Update(SSOracleBehavior self)
        {
            this.gameCounter++;
        }
    }
}
