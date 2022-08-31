using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    class Pong : IFPGame
    {
        public List<ProjectedImage> Images;
        public enum ImgRepr
        {
            Player,
            Pebbles,
            Ball,
            Divider
        }
        public static readonly string[] ImgName = //must be <= ImgRepr
        {
            "listDivider2.png",
            "listDivider2.png",
            "buttonCircleB.png",
            "listDivider.png" //TODO images invalid
        };


        public Pong(SSOracleBehavior self)
        {
            this.Images = new List<ProjectedImage>();
            for (int i = 0; i < ImgName.Length; i++)
                this.Images.Add(self.oracle.myScreen.AddImage(ImgName[i]));

            FivePebblesPong.ME.Logger_p.LogInfo("Pong constructor called"); //TODO remove
        }


        ~Pong() //destructor
        {
            this.Destruct(); //if not done already
            FivePebblesPong.ME.Logger_p.LogInfo("Pong destructor called"); //TODO remove
        }


        //to immediately remove images
        public void Destruct()
        {
            for (int i = 0; i < this.Images.Count; i++)
            {
                if (Images[i] != null)
                {
                    Images[i].Destroy();
                    Images[i] = null;
                }
            }
            Images.RemoveRange(0, Images.Count);
        }


        public void Update(SSOracleBehavior self)
        {

        }


        public void Draw(SSOracleBehavior self)
        {
            for (int i = 0; i < this.Images.Count; i++)
                if (Images[i] == null)
                    return;

            Images[(int)ImgRepr.Player].setPos = new Vector2?(self.player.DangerPos);
            Images[(int)ImgRepr.Divider].setPos = new Vector2(490, 350); //TODO magic number
        }
    }
}
