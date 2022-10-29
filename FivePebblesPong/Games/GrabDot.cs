using System;
using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class GrabDot : FPGame
    {
        public AbstractCreature c;
        public PearlSelection p;
        public List<Vector2> pearlTargets;


        public GrabDot(SSOracleBehavior self) : base(self) //dependent on CreatureViolenceHook
        {
            WorldCoordinate pos = self.oracle.room.GetWorldCoordinate(new Vector2(midX, midY));
            EntityID newID = self.oracle.room.game.GetNewID();
            c = new AbstractCreature(self.oracle.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CyanLizard), null, pos, newID);
            c.RealizeInRoom();

            p = new PearlSelection(self, addGrabbedPearls: true) { teleport = true };
            pearlTargets = new List<Vector2>();
            for (int i = 0; i < p.pearls.Count; i++)
                pearlTargets.Add(new Vector2(750, 90));
        }


        ~GrabDot() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            base.Destroy(); //empty

            c?.realizedCreature.LoseAllGrasps();
            c?.realizedCreature.RemoveFromRoom();
            c?.Destroy();
            c = null;

            if (PebblesGameStarter.starter != null)
                PebblesGameStarter.starter.gravity = true;

            pearlTargets.Clear();
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);

            if (PebblesGameStarter.starter != null)
                PebblesGameStarter.starter.gravity = false;

            if (gameCounter > 1)
                p.Update(self, pearlTargets);
        }
    }
}
