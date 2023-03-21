﻿using UnityEngine;

namespace FivePebblesPong
{
    public abstract class FPGame
    {
        public int maxY, minY, maxX, minX; //playable field
        public int midY => minY + ((maxY - minY) / 2);
        public int midX => minX + ((maxX - minX) / 2);
        public int lenX => maxX - minX;
        public int lenY => maxY - minY;
        public int gameCounter;
        public int palette = -1;
        public Player p;


        public FPGame(OracleBehavior self)
        {
            //default SS_AI & DM_AI
            minX = 200;
            maxX = 780;
            minY = 60;
            maxY = 640;

            if (self?.oracle?.room?.game != null && self.oracle.room.roomSettings != null)
            {
                if (self.oracle.room.roomSettings.name.StartsWith("RM_AI")) {
                    minX = 1220;
                    maxX = 1800;
                    minY = 800;
                    maxY = 1380;
                }
                if (self.oracle.room.roomSettings.name.Equals("SL_AI")) {
                    minX = 1240;
                    maxX = 1820;
                    minY = 40;
                    maxY = 620;
                }
            }

            if (self != null)
                p = FivePebblesPong.GetPlayer(self);
        }


        //to immediately remove images
        public virtual void Destroy() { }

        public virtual void Update(SSOracleBehavior self) { this.Update(self as OracleBehavior); }
        public virtual void Update(SLOracleBehavior self) { this.Update(self as OracleBehavior); }
        public virtual void Update(OracleBehavior self) {
            this.gameCounter++;
            if (this.gameCounter < 0)
                this.gameCounter = 0;

            p = FivePebblesPong.GetPlayer(self);
        }


        //separate function to allow pebbles to move all images simultaneously
        public virtual void Draw(Vector2 offset) { }
        public virtual void Draw() { Draw(new Vector2()); }
    }
}
