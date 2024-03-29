﻿using UnityEngine;

namespace FivePebblesPong
{
    public class PongLine : FPGameObject
    {
        public PongLine(OracleBehavior self, bool horizontal, int length, int width, int dashLength, Color color, string imageName, bool reloadImg = false) : base(imageName)
        {
            base.SetImage(self, CreateGamePNGs.DrawPerpendicularLine(horizontal, length, width, dashLength, color), reloadImg);
        }


        ~PongLine() //destructor
        {
            base.Destroy(); //if not done already
        }
    }
}
