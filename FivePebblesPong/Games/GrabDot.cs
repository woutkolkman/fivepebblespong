using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class GrabDot : FPGame
    {
        public List<AbstractCreature> creatures;
        public PearlSelection p;
        public List<Vector2> pearlTargets;
        public Dot dot;


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
                pearlTargets.Add(new Vector2(750, 90));

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

            p.Update(self, pearlTargets);

            if (gameCounter > 100 && dot == null)
                dot = new Dot(self, this, 12, "FPP_Dot", color: Color.red, reloadImg: false) { alpha = 0.6f };

            if (dot != null && dot.Update(self))
            {
                dot.Destroy();
                dot = null;
            }

            if (gameCounter == 300) //TODO replace
                SpawnCreature(self);

            //TODO circles which player must reach
            //TODO push all creatures away from entrances during game
        }


        public override void Draw(Vector2 offset)
        {
            //update image positions
            dot?.DrawImage(offset);
        }
    }
}
