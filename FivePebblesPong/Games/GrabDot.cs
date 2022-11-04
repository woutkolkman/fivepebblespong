using System.Collections.Generic;
using UnityEngine;
using System;

namespace FivePebblesPong
{
    public class GrabDot : FPGame
    {
        public List<AbstractCreature> creatures;
        public PearlSelection p;
        public List<Vector2> pearlTargets;
        public Dot dot;
        public int score = -1;
        public int winScore = 8;
        public int scoreRadius = 50;


        public GrabDot(SSOracleBehavior self) : base(self) //dependent on CreatureViolenceHook and LizardGraphicsAddToContainerHook
        {
            minX += 20;
            maxX -= 20;
            minY += 20;
            maxY -= 20;

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


        public void SpawnCreature(SSOracleBehavior self)
        {
            //TODO spawning animation circle fading + pebbles pointing?

            WorldCoordinate pos = self.oracle.room.GetWorldCoordinate(new Vector2(midX, midY));
            EntityID newID = self.oracle.room.game.GetNewID();
            AbstractCreature newC = new AbstractCreature(self.oracle.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CyanLizard), null, pos, newID);
            creatures.Add(newC);
            newC.RealizeInRoom();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;

            if (PebblesGameStarter.starter != null)
                PebblesGameStarter.starter.gravity = false;

            if (gameCounter > 100 && dot == null)
            {
                dot = new Dot(self, this, 12, "FPP_Dot", color: Color.red, reloadImg: false) { alpha = 0.5f, adjustToBackground = true };
                score++;
            }

            if (dot != null && dot.Update(self))
            {
                dot.Destroy();
                dot = null;
            }

            //display score in rotating circle of pearls
            for (int i = 0; i < pearlTargets.Count && i < score; i++)
            {
                float offset = (2*(float)Math.PI / winScore) * i;
                float x = midX + scoreRadius * (float) Math.Sin(offset + (float) base.gameCounter / 20);
                float y = midY + scoreRadius * (float) Math.Cos(offset + (float) base.gameCounter / 20);
                pearlTargets[i] = new Vector2(x, y);
            }
            if (dot != null && score >= 0 && score < pearlTargets.Count)
                pearlTargets[score] = dot.pos + Vector2.zero;

            p.Update(self, pearlTargets);

            //TODO win

            if (gameCounter == 300) //TODO replace
                SpawnCreature(self);

            //TODO push all creatures away from entrances during game?
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            dot?.DrawImage(offset);
        }
    }
}
