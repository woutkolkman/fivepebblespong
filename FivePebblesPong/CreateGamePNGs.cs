using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using System.IO;

namespace FivePebblesPong
{
    public static class CreateGamePNGs
    {
        //25 pixels transparent from each edge (for projection shader to work properly)
        public const int EDGE_DIST = 25;


        public static Texture2D DrawRectangle(int width, int height, int thickness)
        {
            return DrawRectangle(width, height, thickness, new Color(1f, 1f, 1f));
        }
        public static Texture2D DrawRectangle(int width, int height, int thickness, Color color)
        {
            Texture2D texture = new Texture2D(width + (2*EDGE_DIST), height + (2*EDGE_DIST), TextureFormat.ARGB32, false);

            //transparent background
            FillTransparent(ref texture);

            //shape
            for (int x = EDGE_DIST; x < EDGE_DIST + width; x++) {
                for (int t = thickness; t > 0; t--)
                {
                    texture.SetPixel(x, EDGE_DIST + (t-1), color);
                    texture.SetPixel(x, EDGE_DIST + height - t, color);
                }
            }
            for (int y = EDGE_DIST; y < EDGE_DIST+height; y++) {
                for (int t = thickness; t > 0; t--)
                {
                    texture.SetPixel(EDGE_DIST + (t-1), y, color);
                    texture.SetPixel(EDGE_DIST + width - t, y, color);
                }
            }
            return texture;
        }


        public static Texture2D DrawCircle(int radius) { return DrawCircle(radius, radius, new Color(1f, 1f, 1f)); }
        public static Texture2D DrawCircle(int radius, int thickness) { return DrawCircle(radius, thickness, new Color(1f, 1f, 1f)); }
        public static Texture2D DrawCircle(int radius, int thickness, Color color)
        {
            int diam = 2 * radius;
            Texture2D texture = new Texture2D(diam + (2 * EDGE_DIST), diam + (2 * EDGE_DIST), TextureFormat.ARGB32, false);

            //transparent background
            FillTransparent(ref texture);

            //shape
            int x = EDGE_DIST+radius, y = EDGE_DIST + radius;
            for (int i = 0; i < 2; i++)
            {
                //https://stackoverflow.com/questions/30410317/how-to-draw-circle-on-texture-in-unity
                float rSquared = radius * radius;
                for (int u = x - radius; u < x + radius + 1; u++)
                    for (int v = y - radius; v < y + radius + 1; v++)
                        if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared)
                            texture.SetPixel(u, v, color);
                color = Color.clear;
                radius -= thickness;
            }
            return texture;
        }


        public static Texture2D DrawPerpendicularLine(bool horizontal, int length, int width, Color color) { return DrawPerpendicularLine(horizontal, length, width, 0, color); }
        public static Texture2D DrawPerpendicularLine(bool horizontal, int length, int width, int dashLength, Color color)
        {
            int texX = 2 * EDGE_DIST + (horizontal ? length : width);
            int texY = 2 * EDGE_DIST + (horizontal ? width : length);

            Texture2D texture = new Texture2D(texX, texY, TextureFormat.ARGB32, false);

            //transparent background
            FillTransparent(ref texture);

            bool dashed = (dashLength != 0);
            for (int l = 0; l < length; l++)
                if (!dashed || l % dashLength < dashLength/2)
                    for (int w = 0; w < width; w++)
                        texture.SetPixel(EDGE_DIST + (horizontal ? l : w), EDGE_DIST + (horizontal ? w : l), color);

            return texture;
        }


        public static void FillTransparent(ref Texture2D texture)
        {
            Color[] fillPixels = new Color[texture.width * texture.height];
            Color fillColor = Color.clear;
            for (int i = 0; i < fillPixels.Length; i++)
                fillPixels[i] = fillColor;
            texture.SetPixels(fillPixels);
        }


        public static void SavePNG(Texture2D texture, string name)
        {
            //overwrites existing files with same name
            string fileName = string.Concat(new object[]
            {
                Custom.RootFolderDirectory(),
                "Assets",
                Path.DirectorySeparatorChar,
                "Futile",
                Path.DirectorySeparatorChar,
                "Resources",
                Path.DirectorySeparatorChar,
                "Illustrations",
                Path.DirectorySeparatorChar,
                name,
                ".png"
            });
            PNGSaver.SaveTextureToFile(texture, fileName);
            FivePebblesPong.ME.Logger_p.LogInfo("Texture saved as " + name + ".png");
        }


        public static void LoadPNG(string fileName)
        {
            //allows loading regular PNGs to use them as sprites
            //load file via disposable projectedimage object
            ProjectedImage p = new ProjectedImage(new List<string> { fileName }, 0);
            p.Destroy();
        }


        public static Texture2D DrawDino(Color color)
        {
            const int width = 20;
            const int height = 22;
            int[] dinoArr = {
                0x1FE, 0x3FF, 0x37F, 0x3FF, 0x3FF, 0x3FF, 0x3E0, 0x3FC, 0x807C0, 0x81FC0, 0xC3FF0, 0xE7FD0,
                0xFFFC0, 0xFFFC0, 0x7FFC0, 0x3FF80, 0x1FF00, 0xFE00, 0x7600, 0x6200, 0x4200, 0x6300
            };
            return ArrayToTexture(ref dinoArr, width, height, color);
        }


        public static Texture2D DrawCactus(Color color)
        {
            const int width = 17;
            const int height = 22;
            int[] cactArr = {
                0x380, 0x7C0, 0x7C0, 0x7C6, 0x7CF, 0xC7CF, 0x1E7CF, 0x1E7CF, 0x1E7CF, 0x1E7CF, 0x1E7CF, 
                0x1FFFE, 0xFFFE, 0x7FFC, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0,
            };
            return ArrayToTexture(ref cactArr, width, height, color);
        }


        //max width is 32 (int)
        public static Texture2D ArrayToTexture(ref int[] arr, int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width + (2 * EDGE_DIST), height + (2 * EDGE_DIST), TextureFormat.ARGB32, false);

            //transparent background
            FillTransparent(ref texture);

            //shape
            for (int r = 0; r < height; r++)
                for (int c = 0; c < width; c++)
                    if ((arr[r] & (1 << c)) > 0)
                        texture.SetPixel(EDGE_DIST + width - c - 1, EDGE_DIST + height - r - 1, color);
            return texture;
        }
    }
}
