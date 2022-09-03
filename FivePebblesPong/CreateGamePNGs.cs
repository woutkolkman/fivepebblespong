using System.Collections.Generic;
using BepInEx;
using System;
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
        }
    }
}
