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


        //offset allows pebbles to move all images simultaneously
        public virtual void DrawImage() { DrawImage(new Vector2()); }
        public virtual void DrawImage(Vector2 offset)
        {
            if (image != null)
                image.setPos = new Vector2?(pos + offset);
        }


        //"reload": only reload image if image is not currently used
        public virtual void SetImage(SSOracleBehavior self, Texture2D texture, bool reload = false)
        {
            SetImage(self, new List<Texture2D> { texture }, 0, reload);
        }
        public virtual void SetImage(SSOracleBehavior self, List<Texture2D> textures, int cycleTime, bool reload = false)
        {
            if (textures.Count <= 0)
                return;
            List<string> names = new List<string>();

            for (int i = 0; i < textures.Count; i++)
            {
                //add number to textures if they are used in an animation
                names.Add(imageName + (i > 0 ? i.ToString() : ""));

                //unload existing png
                bool exists = Futile.atlasManager.DoesContainAtlas(names[i]);
                if (exists && reload)
                    Futile.atlasManager.UnloadImage(names[i]);

                //create png
                if (!exists || reload)
                    CreateGamePNGs.SavePNG(textures[i], names[i]);
            }

            //load png(s)
            this.image = self.oracle.myScreen.AddImage(names, cycleTime); //if an image is invalid, execution is cancelled (by exception?)
        }


        public virtual void Destroy()
        {
            if (image != null)
                image.Destroy();
            image = null;
        }
    }
}
