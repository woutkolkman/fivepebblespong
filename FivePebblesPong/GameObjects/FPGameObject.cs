using System.Collections.Generic;
using UnityEngine;

namespace FivePebblesPong
{
    public abstract class FPGameObject
    {
        public Vector2 pos;
        public string imageName;
        public ProjectedImage image;
        public bool adjustToBackground = false;
        
        
        public FPGameObject(string imageName)
        {
            this.imageName = (imageName.Equals("") ? "FPP_FPGameObject" : imageName);
        }


        //offset allows pebbles to move all images simultaneously
        public virtual void DrawImage() { DrawImage(new Vector2()); }
        public virtual void DrawImage(Vector2 offset)
        {
            if (image != null)
                image.setPos = new Vector2?(pos + offset + (adjustToBackground ? new Vector2(-7.5f, 15.5f) : new Vector2()));
        }


        //use this function if texture is generated via code
        public virtual void SetImage(OracleBehavior self, Texture2D texture, bool reload = false)
        {
            SetImage(self, new List<Texture2D> { texture }, 0, reload);
        }
        public virtual void SetImage(OracleBehavior self, List<Texture2D> textures, int cycleTime, bool reload = false)
        {
            //"reload": only reload image if image is not currently used

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
            }

            //create OracleProjectionScreen in case of no projectionscreen (at BSM)
            if (self.oracle.myScreen == null)
                self.oracle.myScreen = new OracleProjectionScreen(self.oracle.room, self);

            if ((self is SLOracleBehavior && !ModManager.MSC) || self is MoreSlugcats.SSOracleRotBehavior) {
                image = new MoonProjectedImageFromMemory(textures, names, cycleTime);
            } else {
                image = new ProjectedImageFromMemory(textures, names, cycleTime);
            }
            self.oracle.myScreen.images.Add(image);
            self.oracle.myScreen.room.AddObject(image);

            image.pos = prevPos;
        }


        //use this function if texture is read from .PNG
        public virtual void SetImage(OracleBehavior self, List<string> names, int cycleTime = 0)
        {
            Vector2 prevPos = new Vector2();
            if (image != null)
            {
                prevPos = image.pos;
                image.Destroy();
            }
            image = null;

            if (names == null)
                names = new List<string>();
            if (names.Count <= 0)
                names.Add(this.imageName);

            //create OracleProjectionScreen in case of no projectionscreen (at BSM)
            if (self.oracle.myScreen == null)
                self.oracle.myScreen = new OracleProjectionScreen(self.oracle.room, self);

            if ((self is SLOracleBehavior && !ModManager.MSC) || self is MoreSlugcats.SSOracleRotBehavior) {
                image = new MoonProjectedImage(names, cycleTime);
            } else {
                image = new ProjectedImage(names, cycleTime);
            }
            self.oracle.myScreen.images.Add(image);
            self.oracle.myScreen.room.AddObject(image);

            image.pos = prevPos;

            /*
             * If switching from single to cycled image, first load all new images, 
             * else ProjectedImage.LoadFile() will break loading prematurely (RW bug). 
             * For example:
             *   before:  base.SetImage(self, new List<string> { "FPP_Ball1" }, 0);
             *   after:   base.SetImage(self, new List<string> { "FPP_Ball2", "FPP_Ball1" }, 15);
             */
        }


        public virtual void Destroy()
        {
            image?.Destroy();
            image = null;
        }
    }


    public class ProjectedImageFromMemory : ProjectedImage
    {
        public ProjectedImageFromMemory(List<Texture2D> textures, List<string> imageNames, int cycleTime) : base(imageNames, cycleTime)
        {
            //depends on ProjectedImageCtorHook for correct base class construction

            if (textures == null || imageNames == null || textures.Count != imageNames.Count)
                return;

            for (int i = 0; i < textures.Count; i++)
            {
                //check could fail if a texture with the same name is loaded in the same program cycle (?)
                if (Futile.atlasManager.GetAtlasWithName(imageNames[i]) != null)
                    continue; //base game uses break (RW bug)

                textures[i].wrapMode = TextureWrapMode.Clamp;
                textures[i].anisoLevel = 0;
                textures[i].filterMode = FilterMode.Point;
                Futile.atlasManager.LoadAtlasFromTexture(imageNames[i], textures[i], false);
            }
        }
    }


    //force MoonProjection shader even without MSC enabled
    public class MoonProjectedImage : ProjectedImage
    {
        public MoonProjectedImage(List<string> imageNames, int cycleTime) : base(imageNames, cycleTime) { }
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite(this.imageNames[0], true);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["MoonProjection"];
            this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
        }
    }
    public class MoonProjectedImageFromMemory : ProjectedImageFromMemory
    {
        public MoonProjectedImageFromMemory(List<Texture2D> textures, List<string> imageNames, int cycleTime) : base(textures, imageNames, cycleTime) { }
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite(this.imageNames[0], true);
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["MoonProjection"];
            this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
        }
    }
}
