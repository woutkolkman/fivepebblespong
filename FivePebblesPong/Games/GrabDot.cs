using System.Collections.Generic;
using UnityEngine;
using RWCustom;
using System;

namespace FivePebblesPong
{
    public class GrabDot : FPGame
    {
        public List<AbstractCreature> creatures;
        public PearlSelection p;
        public List<Vector2> pearlTargets;
        public Dot dot;
        private Spawnimation spawn;
        public int score = -1;
        public int winScore = 8;
        public int scoreRadius = 50;
        public static bool winReacted = false;


        public GrabDot(SSOracleBehavior self) : base(self) //dependent on CreatureViolenceHook and LizardGraphicsAddToContainerHook
        {
            minX += 21;
            maxX -= 21;
            minY += 21;
            maxY -= 21;

            creatures = new List<AbstractCreature>();

            p = new PearlSelection(self, addGrabbedPearls: true) { teleport = true };
            pearlTargets = new List<Vector2>();
            for (int i = 0; i < p.pearls.Count; i++)
                pearlTargets.Add(new Vector2(maxX, minY));

            //finish calibration early
            if (PebblesGameStarter.starter != null)
                PebblesGameStarter.starter.showMediaCounter = 100;

            base.palette = 23;
        }


        public override void Destroy()
        {
            base.Destroy(); //empty
            dot?.Destroy();

            foreach (AbstractCreature a in creatures)
            {
                a?.realizedCreature.LoseAllGrasps();
                a?.stuckObjects.Clear();
                a?.realizedCreature.RemoveFromRoom();
                a?.Destroy();
            }
            creatures.Clear();

            if (PebblesGameStarter.starter != null)
                PebblesGameStarter.starter.gravity = true;

            pearlTargets.Clear();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;

            if (PebblesGameStarter.starter != null)
                PebblesGameStarter.starter.gravity = false;

            if (gameCounter > 100 && dot == null && score < winScore)
            {
                score++;
                if (score < winScore)
                    dot = new Dot(self, this, 12, "FPP_Dot", color: new Color(0, 232, 230) /*cyan lizard icon*/, reloadImg: false) { alpha = 0.5f, adjustToBackground = true };
                if (score >= winScore && !winReacted)
                {
                    self.dialogBox.Interrupt(self.Translate("Nice!"), 10);
                    winReacted = true;
                }
                if (score == 1 || (score > 0 && UnityEngine.Random.value < 0.1f))
                {
                    spawn = new Spawnimation(self, this);
                    for (int i = p.pearls.Count - 4; i >= 0 && i < p.pearls.Count; i++)
                        spawn.pearls.Add(p.pearls[i]);
                }
            }

            if (dot != null && dot.Update(self))
            {
                dot.Destroy();
                dot = null;
            }

            //display score in rotating circle of pearls
            for (int i = 0; i < pearlTargets.Count && i < score; i++)
                pearlTargets[i] = GetPosInCircle(new Vector2(midX, midY), scoreRadius, i, winScore);
            if (dot != null && score >= 0 && score < pearlTargets.Count)
                pearlTargets[score] = dot.pos + Vector2.zero;

            p.Update(self, pearlTargets);

            //update lizard spawn animation
            if (spawn != null) {
                if (spawn.Update(self, this)) {
                    SpawnCreature(self, spawn.target);
                    spawn = null;
                }
            }

            //TODO push all creatures away from entrances during game?
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            dot?.DrawImage(offset);
        }


        public Vector2 GetPosInCircle(Vector2 center, int radius, int pearl, int maxPearls)
        {
            float offset = (2 * (float)Math.PI / maxPearls) * pearl;
            float x = center.x + radius * (float)Math.Sin(offset + (float)base.gameCounter / 20);
            float y = center.y + radius * (float)Math.Cos(offset + (float)base.gameCounter / 20);
            return new Vector2(x, y);
        }


        public void SpawnCreature(SSOracleBehavior self, Vector2 pos)
        {
            for (int j = 0; j < 15; j++)
                self.oracle.room.AddObject(new Spark(pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(0, 232, 230) /*cyan lizard icon*/, null, 30, 120));
            WorldCoordinate wPos = self.oracle.room.GetWorldCoordinate(pos);
            EntityID newID = self.oracle.room.game.GetNewID();
            AbstractCreature newC = new AbstractCreature(self.oracle.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CyanLizard), null, wPos, newID);
            creatures.Add(newC);
            newC.RealizeInRoom();
        }


        internal class Spawnimation
        {
            public int startCounter;
            public int transitionTime = 120;
            public List<PhysicalObject> pearls;
            public Vector2 target;
            public Vector2 pos;
            public int radius = 2;
            public int maxRadius = 15;


            public Spawnimation(SSOracleBehavior self, GrabDot game)
            {
                startCounter = game.gameCounter;
                pearls = new List<PhysicalObject>();
                target = new Vector2(UnityEngine.Random.Range(game.minX, game.maxX), UnityEngine.Random.Range(game.minY, game.maxY));
            }


            ~Spawnimation()
            {
                pearls.Clear();
            }


            public bool Update(SSOracleBehavior self, GrabDot game)
            {
                int remTime = transitionTime - (game.gameCounter - startCounter);
                pos = Vector2.Lerp(target, self.currentGetTo, (float)remTime / transitionTime);

                if (radius < maxRadius && transitionTime - remTime > 10)
                    radius++;
                if (remTime < radius)
                    radius = remTime;

                for (int i = 0; i < pearls.Count; i++) {
                    if (pearls[i].grabbedBy.Count <= 0) {
                        pearls[i].firstChunk.pos = game.GetPosInCircle(pos, radius, i, pearls.Count);
                        pearls[i].firstChunk.vel = new Vector2();
                    }
                }
                return remTime <= 0;
            }
        }
    }
}
