﻿using System;
using System.Drawing;
using UnityEngine;

namespace FivePebblesPong
{
    public class DoomRetro : FPGame
    {
        public DoomRetro(OracleBehavior self) : base(self)
        {
            
        }


        ~DoomRetro() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
        }


        public override void Update(OracleBehavior self)
        {
            base.Update(self);
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
        }
    }
}
