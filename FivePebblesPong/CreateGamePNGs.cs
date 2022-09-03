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
            Texture2D texture = new Texture2D(width + (2*EDGE_DIST), height + (2*EDGE_DIST), TextureFormat.ARGB32, false);

            //transparent background
            Color[] fillPixels = new Color[texture.width * texture.height];
            for (int i = 0; i < fillPixels.Length; i++)
                fillPixels[i] = Color.clear;
            texture.SetPixels(fillPixels);

            //shape
            for (int x = EDGE_DIST; x < EDGE_DIST + width; x++) {
                for (int t = thickness; t > 0; t--)
                {
                    texture.SetPixel(x, EDGE_DIST + (t-1), new Color(1f, 1f, 1f));
                    texture.SetPixel(x, EDGE_DIST + height - t, new Color(1f, 1f, 1f));
                }
            }
            for (int y = EDGE_DIST; y < EDGE_DIST+height; y++) {
                for (int t = thickness; t > 0; t--)
                {
                    texture.SetPixel(EDGE_DIST + (t-1), y, new Color(1f, 1f, 1f));
                    texture.SetPixel(EDGE_DIST + width - t, y, new Color(1f, 1f, 1f));
                }
            }
            return texture;
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
