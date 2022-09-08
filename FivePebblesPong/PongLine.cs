using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class PongLine : FPGameObject
    {
        public PongLine(SSOracleBehavior self, FPGame game, string imageName) : base(imageName)
        {
            base.SetImage(self, CreateGamePNGs.DrawPerpendicularLine(false, game.maxY - game.minY, 2, 18, Color.white));
            base.pos = new Vector2(game.midX, game.midY);
        }
    }
}
