﻿using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class Pong : FPGame
    {
        public enum ImgRepr {
            Player, Pebbles, Ball, Divider
        }
        public static readonly string[] ImgName = { //must be <= ImgRepr
            //"listDivider2", "listDivider2", "buttonCircleB", "listDivider"
            "AIimg3b", "AIimg3b", "AIimg3b", "AIimg3b"
        };


        public Pong(SSOracleBehavior self) : base(self)
        {
            base.Images = new List<ProjectedImage>();

            for (int i = 0; i < ImgName.Length; i++)
                base.Images.Add(self.oracle.myScreen.AddImage(ImgName[i]));

            FivePebblesPong.ME.Logger_p.LogInfo("Pong constructor called"); //TODO remove
        }


        ~Pong() //destructor
        {
            this.Destruct(); //if not done already
            FivePebblesPong.ME.Logger_p.LogInfo("Pong destructor called"); //TODO remove
        }


        public override void Update(SSOracleBehavior self)
        {
            base.Update(self);
        }


        public override void Draw(SSOracleBehavior self)
        {
            base.Draw(self);

            for (int i = 0; i < base.Images.Count; i++)
                if (base.Images[i] == null)
                    return;

            base.Images[(int)ImgRepr.Player].setPos = new Vector2?(self.player.DangerPos);
            base.Images[(int)ImgRepr.Divider].setPos = new Vector2(MID_X, MID_Y);
        }
    }
}