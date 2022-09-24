using System;
using UnityEngine;

namespace FivePebblesPong
{
    public class MoonDino : FPGame
    {
        public MoonDino(SLOracleBehavior self) : base()
        {
            base.minX = 0;
            base.maxX = 0;
            base.minY = 0;
            base.maxY = 0; //TODO

            FivePebblesPong.ME.Logger_p.LogInfo("MoonDino ctor"); //TODO remove
        }


        ~MoonDino() //destructor
        {
            this.Destroy(); //if not done already
            FivePebblesPong.ME.Logger_p.LogInfo("MoonDino deconstructor"); //TODO remove
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            //this.playerPdl?.Destroy();
        }


        public override void Update(SLOracleBehavior self)
        {
            base.Update(self);
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            //playerPdl.DrawImage(offset);
        }
    }
}
