using System.Collections.Generic;
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
            if (self is SLOracleBehavior)
            {
                image = ProjectedImageV2.AddImage(self.oracle.myScreen, names, cycleTime);

                //get glow color from texture, TODO not efficient
                Color? color = null;
                if (!textures[0].GetPixel(textures[0].width / 2, textures[0].height / 2).Equals(Color.clear))
                    color = textures[0].GetPixel(textures[0].width / 2, textures[0].height / 2); //center of texture
                for (int i = CreateGamePNGs.EDGE_DIST + textures[0].width * CreateGamePNGs.EDGE_DIST; i < textures[0].GetPixels().Length && color == null; i++)
                    if (!textures[0].GetPixels()[i].Equals(Color.clear))
                        color = textures[0].GetPixels()[i]; //any pixel from texture starting from EDGE_DIST
                (image as ProjectedImageV2).glowColor = color ?? Color.white;
            }
            else
            {
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
    public class ProjectedImageV2 : ProjectedImage
    {
        public Color glowColor = Color.white;
        public float glowWidth, glowHeight;


        public ProjectedImageV2(List<string> imageNames, int cycleTime) : base(imageNames, cycleTime) { }


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


        public static ProjectedImageV2 AddImage(ProjectionScreen self, List<string> names, int cycleTime)
        {
            self.images.Add(new ProjectedImageV2(names, cycleTime));
            self.room.AddObject(self.images[self.images.Count - 1]);
            return self.images[self.images.Count - 1] as ProjectedImageV2;
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
