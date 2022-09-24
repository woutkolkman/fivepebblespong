using System.Collections.Generic;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class PearlSelection : FPGame
    {
        public List<PhysicalObject> pearls;
        public int pearlGrabbed;


        public PearlSelection(SSOracleBehavior self) : base()
        {
            base.minX = 240;
            base.maxX = 760;
            base.minY = 100;
            base.maxY = 200;
            this.pearlGrabbed = -1;
            this.pearls = new List<PhysicalObject>();

            //gather pearls from current room
            for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++)
                for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++)
                    if (self.oracle.room.physicalObjects[i][j] is PebblesPearl &&
                        self.oracle.room.physicalObjects[i][j].grabbedBy.Count <= 0)
                        pearls.Add(self.oracle.room.physicalObjects[i][j]);

            //prevent showing pearl dialog
            self.pearlPickupReaction = false;
        }


        ~PearlSelection() //destructor
        {
            pearls.Clear();
            base.Destroy(); //if not done already
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            float dTh = 2.5f; //damping
            float pTh = 1.5f; //propotional

            //check if pearl is grabbed, else set position
            pearlGrabbed = -1;
            for (int i = 0; i < pearls.Count; i++) {
                if (pearls[i].grabbedBy.Count > 0)
                {
                    if (pearls[i].grabbedBy[0].grabber == self.player)
                        pearlGrabbed = i;
                    continue;
                }
                //generate sinus
                float x = minX + (i * (lenX / pearls.Count));
                float y = midY + (lenY/2) * (float) Math.Sin((x + base.gameCounter)/50);

                //set velocity
                pearls[i].bodyChunks[0].vel.x /= dTh;
                pearls[i].bodyChunks[0].vel.y /= dTh;
                pearls[i].bodyChunks[0].vel += pTh * Custom.DirVec(pearls[i].bodyChunks[0].pos, new Vector2(x, y));
            }
        }
    }
}
