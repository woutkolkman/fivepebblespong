using System.Collections.Generic;
using System;
using UnityEngine;

namespace FivePebblesPong
{
    public class Pipe : FPGameObject
    {
        public float movementSpeed = 5f;
        public PipeGraphic[] graphics = new PipeGraphic[6];
        public Vector2 flyRect;
        public readonly Vector2 rectSize;
        public float halfX => flyRect.x / 2;
        public float halfY => flyRect.y / 2;
        public int gameLenY;
        public bool passed = false;


        public Pipe(OracleBehavior self, FPGame game, Texture2D rect, Texture2D line, Vector2 rectSize, int height = 100, bool reloadImg = false) : base("")
        {
            pos = new Vector2(game.maxX, UnityEngine.Random.Range(game.minY + height/2, game.maxY - height/2));
            flyRect = new Vector2(rectSize.x, height);
            this.rectSize = rectSize;
            gameLenY = game.lenY;

            for (int i = 0; i < 2; i++)
                graphics[i] = new PipeGraphic(self, rect, "FPP_PipeRect", i == 0 && reloadImg);
            for (int i = 2; i < graphics.Length; i++)
                graphics[i] = new PipeGraphic(self, line, "FPP_PipeLine", i == 2 && reloadImg);
        }


        ~Pipe() //destructor
        {
            this.Destroy(); //if not done already
        }


        public override void Destroy()
        {
            for (int i = 0; i < graphics.Length; i++)
                graphics[i]?.Destroy();
            base.Destroy();
        }


        public bool Update(OracleBehavior self, Vector2 birdPos)
        {
            pos.x -= movementSpeed;

            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i] == null)
                    return false;

            Vector2 rectOffset = new Vector2(0, halfY + rectSize.y/2);
            graphics[0].pos = pos + rectOffset;
            graphics[1].pos = pos - rectOffset;
            Vector2 lineOffset = new Vector2(halfX - 5, halfY + rectSize.y + gameLenY/2 - 7);
            graphics[2].pos = pos + new Vector2(-lineOffset.x, lineOffset.y);
            graphics[3].pos = pos + lineOffset;
            graphics[4].pos = pos + new Vector2(-lineOffset.x, -lineOffset.y);
            graphics[5].pos = pos + new Vector2(lineOffset.x, -lineOffset.y);

            if (birdPos.x < pos.x - halfX || birdPos.x > pos.x + halfX)
                return false;
            if (birdPos.y < pos.y + halfY && birdPos.y > pos.y - halfY)
                return false;
            return true;
        }


        public override void DrawImage(Vector2 offset)
        {
            for (int i = 0; i < graphics.Length; i++)
                graphics[i]?.DrawImage(offset);
        }


        public class PipeGraphic : FPGameObject
        {
            public PipeGraphic(OracleBehavior self, Texture2D tex, string imageName, bool reloadImg = false) : base(imageName)
            {
                base.SetImage(self, tex, reloadImg);
            }
        }
    }
}
