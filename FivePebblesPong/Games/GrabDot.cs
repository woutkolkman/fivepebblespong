using System;
using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public class GrabDot : FPGame
    {
        public AbstractCreature c;


        public GrabDot(SSOracleBehavior self) : base(self)
        {
            WorldCoordinate pos = self.oracle.room.GetWorldCoordinate(new Vector2(midX, midY));
            EntityID newID = self.oracle.room.game.GetNewID();
            c = new AbstractCreature(self.oracle.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CyanLizard), null, pos, newID);
            c.RealizeInRoom();
        }


        ~GrabDot() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            c?.realizedCreature.RemoveFromRoom();
            c?.Destroy();
            c = null;
        }
    }
}
