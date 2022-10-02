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
        public enum Type
        {
            SinusY,
            SinXCosY
        }
        public Type type;


        public PearlSelection(SSOracleBehavior self) : base()
        {
            this.pearlGrabbed = -1;
            this.pearls = new List<PhysicalObject>();

            //gather pearls from current room
            for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++)
                for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++)
                    if (self.oracle.room.physicalObjects[i][j] is PebblesPearl && self.oracle.room.physicalObjects[i][j].grabbedBy.Count <= 0)
                        pearls.Add(self.oracle.room.physicalObjects[i][j]);

            type = (UnityEngine.Random.value < 0.5f ? Type.SinusY : Type.SinXCosY);

            //prevent showing pearl dialog
            self.pearlPickupReaction = false;
        }


        ~PearlSelection() //destructor
        {
            pearls.Clear();
            base.Destroy(); //if not done already
        }


        public List<Vector2> GetPearlTargets()
        {
            List<Vector2> positions = new List<Vector2>();

            switch (type)
            {
                case (Type.SinusY):
                    int pearlsUsed = pearls.Count / 2;
                    base.minX = 240;
                    base.maxX = 760;
                    base.minY = 100;
                    base.maxY = 200;
                    for (int i = 0; i < pearlsUsed; i++)
                    {
                        float x = minX + (i * (lenX / pearlsUsed));
                        float y = midY + (lenY / 2) * (float) Math.Sin((x + base.gameCounter) / 50);
                        positions.Add(new Vector2(x, y));
                    }
                    break;

                case (Type.SinXCosY):
                    base.minX = 230;
                    base.maxX = 750;
                    base.minY = 90;
                    base.maxY = 610;
                    for (int i = 0; i < pearls.Count; i++)
                    {
                        float time = ((i * 2 / (float)pearls.Count) + (float) base.gameCounter / 2000);
                        double formX = Math.Sin(4 * Math.PI * time);
                        double formY = Math.Cos(3 * Math.PI * time);
                        float x = midX + ((lenX/2) * (float) formX);
                        float y = midY + ((lenY/2) * (float) formY);
                        positions.Add(new Vector2(x, y));
                    }
                    break;

                default:

                    break;
            }

            return positions;
        }


        public override void Update(SSOracleBehavior self)
        {
            Update(self, GetPearlTargets());
        }
        public void Update(SSOracleBehavior self, List<Vector2> positions)
        {
            base.Update(self);

            pearlGrabbed = -1;
            if (positions == null)
                return;

            //check if pearl is grabbed, else set position
            for (int i = 0; i < pearls.Count && i < positions.Count; i++)
            {
                if (pearls[i].grabbedBy.Count > 0)
                {
                    if (pearls[i].grabbedBy[0].grabber == self.player)
                        pearlGrabbed = i;
                    continue;
                }

                //if distance is small, hard set position so pearl does not "bounce"
                float dist = Vector2.Distance(pearls[i].firstChunk.pos, positions[i]);
                if (dist < 2f)
                {
                    pearls[i].firstChunk.pos = positions[i];
                    pearls[i].firstChunk.vel = new Vector2();
                    continue;
                }

                //set velocity
                float damping = 1f - dist/15;
                if (damping < 0)
                    damping = 0;
                pearls[i].firstChunk.vel.x /= (1.2f + damping);
                pearls[i].firstChunk.vel.y /= (1.1f + damping);
                pearls[i].firstChunk.vel += 1.5f * Custom.DirVec(pearls[i].firstChunk.pos, positions[i]);
            }
        }
    }
}
