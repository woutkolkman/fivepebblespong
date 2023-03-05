using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class ShowMediaMovementBehavior
    {
        public Vector2 showMediaPos = new Vector2();
        private Vector2 idealShowMediaPos = new Vector2();
        private int consistentShowMediaPosCounter = 0;


        //returns true if done
        public bool Update(OracleBehavior self, Vector2 target, bool finish)
        {
            //at random intervals, recalibrate "projector"
            if (UnityEngine.Random.value < 0.033333335f)
            {
                idealShowMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                showMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
            }

            //finish calibration
            if (finish)
                idealShowMediaPos = target;

            this.Animate(self, finish);

            //target location reached, "projector" is calibrated
            return (finish && showMediaPos == target);
        }


        //used for animation, trying different positions for projectedimages (copied via dnSpy, no docs, sorry)
        private void Animate(OracleBehavior self, bool finish)
        {
            consistentShowMediaPosCounter += (int)Custom.LerpMap(Vector2.Distance(showMediaPos, idealShowMediaPos), 0f, 200f, 1f, 10f);
            Vector2 vector = new Vector2(UnityEngine.Random.value * self.oracle.room.PixelWidth, UnityEngine.Random.value * self.oracle.room.PixelHeight);

            if (!finish && ShowMediaScore(vector) + 40f < ShowMediaScore(idealShowMediaPos))
            {
                idealShowMediaPos = vector;
                consistentShowMediaPosCounter = 0;
            }
            vector = idealShowMediaPos + Custom.RNV() * UnityEngine.Random.value * 40f;
            if (!finish && ShowMediaScore(vector) + 20f < ShowMediaScore(idealShowMediaPos))
            {
                idealShowMediaPos = vector;
                consistentShowMediaPosCounter = 0;
            }
            if (consistentShowMediaPosCounter > 300 || finish)
            { //added "finish" to immediately move towards idealShowMediaPos
                showMediaPos = Vector2.Lerp(showMediaPos, idealShowMediaPos, 0.1f);
                showMediaPos = Custom.MoveTowards(showMediaPos, idealShowMediaPos, 10f);
            }

            float ShowMediaScore(Vector2 tryPos)
            {
                if (self.oracle.room.GetTile(tryPos).Solid)
                    return float.MaxValue;
                float num = Mathf.Abs(Vector2.Distance(tryPos, self.player.DangerPos) - 250f); //NOTE checks only singleplayer: "self.player"
                num -= Math.Min((float)self.oracle.room.aimap.getAItile(tryPos).terrainProximity, 9f) * 30f;
                if (self is SSOracleBehavior)
                    num -= Vector2.Distance(tryPos, (self as SSOracleBehavior).nextPos) * 0.5f;
                for (int i = 0; i < self.oracle.arm.joints.Length; i++)
                    num -= Mathf.Min(Vector2.Distance(tryPos, self.oracle.arm.joints[i].pos), 100f) * 10f;
                if (self.oracle.graphicsModule != null && (self.oracle.graphicsModule as OracleGraphics)?.umbCord?.coord != null)
                    for (int j = 0; j < (self.oracle.graphicsModule as OracleGraphics).umbCord.coord.GetLength(0); j += 3)
                        num -= Mathf.Min(Vector2.Distance(tryPos, (self.oracle.graphicsModule as OracleGraphics).umbCord.coord[j, 0]), 100f);
                return num;
            }
        }
    }
}
