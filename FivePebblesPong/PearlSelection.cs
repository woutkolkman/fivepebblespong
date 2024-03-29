﻿using System.Collections.Generic;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class PearlSelection : FPGame
    {
        public List<PhysicalObject> pearls;
        public int pearlGrabbed; //-1 if no pearl was grabbed
        public bool ignoreVector00; //if true, new Vector2() (default 0,0) is not assigned to pearl
        public bool teleport = false; //if true, pearls get instantly teleported to target
        public enum Type
        {
            SinusY,
            SinXCosY,
            Binary,
            Circle,
            SinXCosYTrain
        }
        public Type type;


        public PearlSelection(SSOracleBehavior self, bool addGrabbedPearls = false) : base(self)
        {
            this.pearlGrabbed = -1;
            this.pearls = new List<PhysicalObject>();

            RefreshPearlsInRoom(self, addGrabbedPearls);

            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0: type = Type.SinusY; break;
                case 1: type = Type.Binary; break;
                case 2: type = Type.Circle; break;
                case 3: type = Type.SinXCosYTrain; break;
                //case 3: type = Type.SinXCosY; break;
            }

            //prevent showing pearl dialog
            self.pearlPickupReaction = false;
        }


        ~PearlSelection() //destructor
        {
            pearls.Clear();
            base.Destroy(); //if not done already
        }


        public void RefreshPearlsInRoom(SSOracleBehavior self, bool addGrabbedPearls = false)
        {
            pearls.Clear();

            //gather pearls from current room
            for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++)
                for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++)
                    if (self.oracle.room.physicalObjects[i][j] is DataPearl && self.oracle.room.physicalObjects[i][j].grabbedBy.Count <= 0)
                        pearls.Add(self.oracle.room.physicalObjects[i][j]);

            //gather pearls from creature grasps
            if (addGrabbedPearls)
                foreach (AbstractCreature c in self.oracle.room.abstractRoom.creatures)
                    for (int i = 0; i < c.realizedCreature.grasps.Length; i++)
                        if (c.realizedCreature.grasps[i] != null && c.realizedCreature.grasps[i].grabbed is DataPearl)
                            pearls.Add(c.realizedCreature.grasps[i].grabbed);
        }


        public List<Vector2> GetPearlTargets()
        {
            List<Vector2> positions = new List<Vector2>();

            int pearlsUsed = pearls.Count;
            switch (type)
            {
                case (Type.SinusY):
                    if (pearlsUsed > 19)
                        pearlsUsed = 19;
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

                case (Type.SinXCosYTrain):
                case (Type.SinXCosY):
                    if (type == Type.SinXCosYTrain && pearlsUsed > 14)
                        pearlsUsed = 14;
                    base.minX = 230;
                    base.maxX = 750;
                    base.minY = 90;
                    base.maxY = 610;
                    for (int i = 0; i < pearlsUsed; i++)
                    {
                        float time = ((-i * 2 / (float) pearls.Count) + (float) base.gameCounter / (type == Type.SinXCosYTrain ? 500 : 2000));
                        double formX = Math.Sin(4 * Math.PI * time);
                        double formY = Math.Cos(3 * Math.PI * time);
                        float x = midX + ((lenX/2) * (float) formX);
                        float y = midY + ((lenY/2) * (float) formY);
                        positions.Add(new Vector2(x, y));
                    }
                    break;

                case (Type.Binary):
                    void AddPoints(int startX, bool add)
                    {
                        for (int i = 0; i < pearlsUsed/2; i++) {
                            float x = (i * (lenX / (pearlsUsed/2)));
                            x = add ? startX + x : startX - x;
                            float y = (base.gameCounter & (1 << 3 + i)) != 0 ? maxY : minY;
                            positions.Add(new Vector2(x, y));
                        }
                    }
                    if (pearlsUsed > 32)
                        pearlsUsed = 32;
                    base.minX = 240;
                    base.maxX = 760;
                    base.minY = 100;
                    base.maxY = 120;
                    AddPoints(minX, true);
                    base.minY = 600;
                    base.maxY = 580;
                    AddPoints(maxX - (lenX / (pearlsUsed/2)), false);
                    break;

                case (Type.Circle):
                    if (pearlsUsed > 12)
                        pearlsUsed = 12;
                    int rad = lenX / 2 - 50;
                    if (gameCounter > 120)
                        teleport = true;
                    for (int i = 0; i < pearlsUsed; i++)
                        positions.Add(GetPosInCircle(gameCounter/40f, new Vector2(midX, midY), rad, -i, pearlsUsed + 2));
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
                    if (pearls[i].grabbedBy[0].grabber is Player)
                        pearlGrabbed = i;
                    continue;
                }

                //don't set position if vector is 0,0
                if (ignoreVector00 && positions[i] == new Vector2())
                    continue;

                //if distance is small, hard set position so pearl does not "bounce"
                float dist = Vector2.Distance(pearls[i].firstChunk.pos, positions[i]);
                if (dist < 3f || teleport)
                {
                    pearls[i].firstChunk.setPos = positions[i];
                    pearls[i].firstChunk.vel = positions[i] - pearls[i].firstChunk.pos;
                    continue;
                }

                //set velocity
                const float minDamping = 1.1f;
                const float maxDamping = 1.9f;
                const float multiplier = 1.5f;
                float damping = maxDamping - dist/10;
                if (damping < minDamping)
                    damping = minDamping;
                pearls[i].firstChunk.vel.x /= (damping);
                pearls[i].firstChunk.vel.y /= (damping);
                pearls[i].firstChunk.vel += multiplier * Custom.DirVec(pearls[i].firstChunk.pos, positions[i]);
            }
        }


        public static Vector2 GetPosInCircle(float counter, Vector2 center, int radius, int pearl, int maxPearls)
        {
            float offset = (2 * (float)Math.PI / maxPearls) * pearl;
            float x = center.x + radius * (float)Math.Sin(offset + counter);
            float y = center.y + radius * (float)Math.Cos(offset + counter);
            return new Vector2(x, y);
        }
    }
}
