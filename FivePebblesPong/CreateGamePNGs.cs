using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using System.IO;
using System;

namespace FivePebblesPong
{
    public static class CreateGamePNGs
    {
        //25 pixels transparent from each edge (for projection shader to work properly)
        public const int EDGE_DIST = 25;


        //transparent border is added so projectionshader works correctly
        //crop, left-bottom-right-top, right and top need to be negative to crop
        public static Texture2D AddTransparentBorder(ref Texture2D texIn, int[] crop = null)
        {
            int width(int[] a) { return a[2] - a[0]; }
            int height(int[] a) { return a[3] - a[1]; }

            if (texIn == null || texIn.width <= 0 || texIn.height <= 0)
                return texIn;

            //get new sides of image with crop
            int[] offsets = { 0, 0, texIn.width, texIn.height }; //left-bottom-right-top
            if (crop != null && crop.Length >= 4)
                for (int i = 0; i < crop.Length; i++)
                    offsets[i] += crop[i];

            //check valid crop, if not valid --> ignore crop
            if (width(offsets) <= 0 || height(offsets) <= 0)
                offsets = new int[] { 0, 0, texIn.width, texIn.height }; //left-bottom-right-top

            Texture2D texOut = new Texture2D(width(offsets) + (2*EDGE_DIST), height(offsets) + (2*EDGE_DIST), TextureFormat.ARGB32, false);

            //transparent background
            FillTransparent(ref texOut);
            texOut.Apply();

            //copies via GPU
            UnityEngine.Graphics.CopyTexture(texIn, 0, 0, offsets[0], offsets[1], width(offsets), height(offsets), texOut, 0, 0, EDGE_DIST, EDGE_DIST);

            return texOut;
        }


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
            texture.Apply();
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
            texture.Apply();
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

            texture.Apply();
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


        public static Texture2D DrawDino(Color color, int walk = 0, bool shocked = false)
        {
            walk %= 3;
            const int width = 20;
            const int height = 22;
            int[] dinoArr = {
                0x1FE, 0x3FF, 0x37F, 0x3FF, 0x3FF, 0x3FF, 0x3E0, 0x3FC, 0x807C0, 0x81FC0, 0xC3FF0, 0xE7FD0, 
                0xFFFC0, 0xFFFC0, 0x7FFC0, 0x3FF80, 0x1FF00, 0xFE00, 0x7600, 0x6200, 0x4200, 0x6300
            };
            if (shocked)
                Array.Copy(new[] {0x31F, 0x35F, 0x31F, 0x3FF, 0x3FF, 0x3FF}, 0, dinoArr, 1, 6);
            if (walk == 1)
                Array.Copy(new[] { 0x7300, 0x6000, 0x4000, 0x6000 }, 0, dinoArr, 18, 4);
            if (walk == 2)
                Array.Copy(new[] { 0x6600, 0x3200, 0x200, 0x300 }, 0, dinoArr, 18, 4);

            return ArrayToTexture(ref dinoArr, width, height, color);
        }


        public static Texture2D DrawDinoDucking(Color color, int walk)
        {
            walk %= 2;
            const int width = 27;
            const int height = 12;
            int[] dinoArr = {
                0x4000000, 0x70FF1FE, 0x7FFFF7F, 0x3FFFFFF, 0x1FFFFFF, 0xFFFFFF, 0x7FFFE0, 0x3FF9FC, 
                0x391000, 0x319800, 0x200000, 0x300000
            };
            if (walk == 1)
                Array.Copy(new[] { 0x271000, 0x361800, 0x40000, 0x60000 }, 0, dinoArr, 8, 4);

            return ArrayToTexture(ref dinoArr, width, height, color);
        }


        public static Texture2D DrawCactus(Color color)
        {
            const int width = 17;
            const int height = 22;
            int[] cactArr = {
                0x380, 0x7C0, 0x7C0, 0x7C6, 0x7CF, 0xC7CF, 0x1E7CF, 0x1E7CF, 0x1E7CF, 0x1E7CF, 0x1E7CF, 
                0x1FFFE, 0xFFFE, 0x7FFC, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0, 0x7C0
            };
            return ArrayToTexture(ref cactArr, width, height, color);
        }


        public static Texture2D DrawBird(Color color, int fly)
        {
            fly %= 2;
            const int width = 21;
            const int height = 18;
            int[] bird1Arr = {
                0x2000, 0x3000, 0x3800, 0x19C00, 0x39E00, 0x7DF00, 0xFDF80, 0x1FFF80, 0x7FC0, 0x3FFF, 
                0x1FF8, 0xFFE, 0x7F0, 0x0, 0x0, 0x0, 0x0, 0x0
            };
            int[] bird2Arr = {
                0x0, 0x0, 0x0, 0x18000, 0x38000, 0x7C000, 0xFC000, 0x1FFF80, 0x7FC0, 0x3FFF, 0x1FF8, 
                0x1FFE, 0x1FF0, 0x1E00, 0x1C00, 0x1800, 0x1800, 0x1000
            };
            int[] arrayRef = (fly == 1 ? bird2Arr : bird1Arr);
            return ArrayToTexture(ref arrayRef, width, height, color);
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

            texture.Apply();
            return texture;
        }
    }
}
