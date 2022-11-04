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
                image.setPos = new Vector2?(pos + offset + (adjustToBackground ? new Vector2(-7, 15) : new Vector2()));
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

            image = (self is SLOracleBehavior ? 
                new ProjectedImageMoon(textures, names, cycleTime) : 
                new ProjectedImageFromMemory(textures, names, cycleTime)
            );
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

            if (self is SLOracleBehavior) {
                image = new ProjectedImageMoon(null, names, cycleTime);
                image.LoadFile();
                (image as ProjectedImageMoon).CalcGlowColor();
                self.oracle.myScreen.images.Add(image);
                self.oracle.myScreen.room.AddObject(image);
            } else {
                image = self.oracle.myScreen.AddImage(names, cycleTime);
            }

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
            if (image != null)
                image.Destroy();
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
                Futile.atlasManager.LoadAtlasFromTexture(imageNames[i], textures[i]);
            }
        }
    }


    //class was created because regular ProjectedImages don't work as well in moons room
    //if using this class with .PNG files, just pass null for textures and call LoadFile() and CalcGlowColor() after constructor
    public class ProjectedImageMoon : ProjectedImageFromMemory
    {
        public Color glowColor = Color.white;
        public float glowWidth, glowHeight;


        public ProjectedImageMoon(List<Texture2D> textures, List<string> imageNames, int cycleTime) : base(textures, imageNames, cycleTime)
        {
            if (textures != null && textures.Count > 0)
                CalcGlowColor();
        }


        public void CalcGlowColor()
        {
            FAtlas atlas = Futile.atlasManager.GetAtlasWithName(imageNames[currImg]);
            if (atlas == null)
                return;

            Texture2D texture = atlas._texture as Texture2D;

            //get glow color from texture, TODO not efficient
            Color? color = null;
            if (!texture.GetPixel(texture.width / 2, texture.height / 2).Equals(Color.clear))
                color = texture.GetPixel(texture.width / 2, texture.height / 2); //center of texture
            for (int i = CreateGamePNGs.EDGE_DIST + texture.width * CreateGamePNGs.EDGE_DIST; i < texture.GetPixels().Length && color == null; i++)
                if (!texture.GetPixels()[i].Equals(Color.clear))
                    color = texture.GetPixels()[i]; //any pixel from texture starting from EDGE_DIST
            this.glowColor = color ?? Color.white;
        }


        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite(this.imageNames[0], true);
            sLeaser.sprites[1] = new FSprite("Futile_White", true);
            sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["LightSource"];

            glowWidth = sLeaser.sprites[0].width - 2 * CreateGamePNGs.EDGE_DIST;
            glowHeight = sLeaser.sprites[0].height - 2 * CreateGamePNGs.EDGE_DIST;
            if (glowWidth < 15) glowWidth = 15;
            if (glowWidth > 50) glowWidth -= (glowWidth - 50) / 1.2f;
            if (glowHeight < 15) glowHeight = 15;
            if (glowHeight > 50) glowHeight -= (glowHeight - 50) / 1.2f;

            this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
        }


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName(this.imageNames[this.currImg]);
            for (int i = 0; i < 2; i++)
            {
                sLeaser.sprites[i].x = this.pos.x - camPos.x;
                sLeaser.sprites[i].y = this.pos.y - camPos.y;
                sLeaser.sprites[i].alpha = this.alpha;
            }
            sLeaser.sprites[1].x = this.pos.x - camPos.x;
            sLeaser.sprites[1].y = this.pos.y - camPos.y;
            sLeaser.sprites[1].color = this.glowColor;
            sLeaser.sprites[1].scaleX = (glowWidth);
            sLeaser.sprites[1].scaleY = (glowHeight);

            //base CosmeticSprite.DrawSprites() copy
            if (base.slatedForDeletetion || this.room != rCam.room)
                sLeaser.CleanSpritesAndRemove();
        }
    }
}
