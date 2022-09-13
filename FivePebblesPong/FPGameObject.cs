using System.Collections.Generic;
using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public abstract class FPGameObject
    {
        public Vector2 pos;
        public string imageName;
        public ProjectedImage image;


        public FPGameObject(string imageName)
        {
            this.imageName = (imageName.Equals("") ? "FPP_FPGameObject" : imageName);
        }


        public virtual void DrawImage()
        {
            if (image != null)
                image.setPos = new Vector2?(pos);
        }


        public virtual void SetImage(SSOracleBehavior self, Texture2D texture, bool reload = false)
        {
            //only reload image if image is not currently used

            //unload existing png
            bool exists = Futile.atlasManager.DoesContainAtlas(imageName);
            if (exists && reload)
                Futile.atlasManager.UnloadImage(imageName);

            //create png
            if (!exists || reload)
                CreateGamePNGs.SavePNG(texture, imageName);

            //load png
            this.image = self.oracle.myScreen.AddImage(imageName); //if image is invalid, execution is cancelled (by exception?)
        }


        public virtual void Destroy()
        {
            if (image != null)
                image.Destroy();
            image = null;
        }
    }
}
