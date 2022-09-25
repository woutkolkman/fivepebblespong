using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class Dino : FPGame
    {
        public PongLine temp; //TODO remove
        public float imageAlpha = 0.4f;


        public Dino(SLOracleBehavior self) : base()
        {
            base.minY = 40;
            base.maxY = 620;
            base.minX = 1240;
            base.maxX = 1820;

            this.temp = new PongLine(self, false, lenY, 2, 18, Color.white, "FPP_Line", reloadImg: true);
            this.temp.pos = new Vector2(midX, midY);

            FivePebblesPong.ME.Logger_p.LogInfo("ctor"); //TODO remove
        }


        ~Dino() //destructor
        {
            this.Destroy(); //if not done already
            FivePebblesPong.ME.Logger_p.LogInfo("dtor"); //TODO remove
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            temp?.Destroy(); //TODO remove
        }


        public override void Update(SLOracleBehavior self)
        {
            base.Update(self);
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            temp.DrawImage(offset);
            //TODO assign imageAlpha
        }
    }
}
