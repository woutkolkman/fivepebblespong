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
        public virtual void SetImage(OracleBehavior self, Texture2D texture, bool reload = false)
        {
            SetImage(self, new List<Texture2D> { texture }, 0, reload);
        }
        public virtual void SetImage(OracleBehavior self, List<Texture2D> textures, int cycleTime, bool reload = false)
        {
            Vector2 prevPos = new Vector2();
            if (image != null) {
                prevPos = image.pos;
                image.Destroy();
            }
            image = null;

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

            //create OracleProjectionScreen in case of no projectionscreen (at BSM)
            if (self.oracle.myScreen == null)
                self.oracle.myScreen = new OracleProjectionScreen(self.oracle.room, self);

            //load png(s), if image is invalid, exception(?) occurs
            if (self is SLOracleBehavior) {
                image = ProjectedImageBloom.AddImage(self.oracle.myScreen, names, cycleTime);
            } else {
                image = self.oracle.myScreen.AddImage(names, cycleTime);
            }
            image.pos = prevPos;
        }


        public virtual void Destroy()
        {
            if (image != null)
                image.Destroy();
            image = null;
        }
    }


    //class was created because regular ProjectedImages don't work as well in moons room
    public class ProjectedImageBloom : ProjectedImage
    {
        public ProjectedImageBloom(List<string> imageNames, int cycleTime) : base(imageNames, cycleTime) { }


        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite(this.imageNames[0], true);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Projection"];
            this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Bloom")); //default is "Foreground"
        }


        public static ProjectedImageBloom AddImage(ProjectionScreen self, List<string> names, int cycleTime)
        {
            self.images.Add(new ProjectedImageBloom(names, cycleTime));
            self.room.AddObject(self.images[self.images.Count - 1]);
            return self.images[self.images.Count - 1] as ProjectedImageBloom;
        }
    }
}
