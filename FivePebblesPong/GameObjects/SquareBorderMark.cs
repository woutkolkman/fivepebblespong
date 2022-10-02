using UnityEngine;

namespace FivePebblesPong
{
    public class SquareBorderMark : FPGameObject
    {
        public SquareBorderMark(OracleBehavior self, int width, int height, string imageName, Color? color = null, int thickness = 1, bool reloadImg = false) : base(imageName)
        {
            Color c = Color.white;
            if (color != null)
                c = (Color)color;
            base.SetImage(self, CreateGamePNGs.DrawRectangle(width, height, thickness, c), reloadImg);
        }


        ~SquareBorderMark() //destructor
        {
            base.Destroy(); //if not done already
        }
    }
}
