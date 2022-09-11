using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public class GameController : Rock
    {
        public GameController(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject, abstractPhysicalObject.world) { }


        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            CreateGamePNGs.LoadPNG("GamepadIcon");

            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("GamepadIcon", true);
            sLeaser.sprites[0].scale = 0.25f;

            this.AddToContainer(sLeaser, rCam, null);
        }


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 a = Vector2.Lerp(base.firstChunk.lastPos, base.firstChunk.pos, timeStacker);
            if (this.vibrate > 0)
            {
                a += Custom.DegToVec(UnityEngine.Random.value * 360f) * 2f * UnityEngine.Random.value;
            }
            sLeaser.sprites[0].x = a.x - camPos.x;
            sLeaser.sprites[0].y = a.y - camPos.y;
            Vector3 v = Vector3.Slerp(this.lastRotation, this.rotation, timeStacker);
            sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), v);

            //blink when able to pick up
            if (this.blink > 0)
            {
                if (this.blink > 1 && UnityEngine.Random.value < 0.5f) {
                    sLeaser.sprites[0].color = base.blinkColor;
                } else {
                    sLeaser.sprites[0].color = this.color;
                }
            }

            //remove sprite
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }


        //palette applies color to sprites
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.color = new Color(0.44705883f, 0.9019608f, 0.76862746f); //5P overseer color
            sLeaser.sprites[0].color = this.color;
        }
    }
}
