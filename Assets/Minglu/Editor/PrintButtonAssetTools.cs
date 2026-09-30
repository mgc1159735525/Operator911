using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    // Lossless sprite-sheet extraction only: artwork and alpha come from the generated source.
    public static class PrintButtonAssetTools
    {
        const string Source = "ArtSource/UI/PrintButtons/print_buttons_source.png";
        const string Destination = "Assets/Minglu/Resources/Minglu/UI/PrintButtons";
        static readonly string[] Families = { "primary", "paper", "dark" };
        static readonly string[] States = { "normal", "hover", "pressed", "selected", "disabled" };
        [Serializable] public class Entry { public string file; public int x, y, width, height; public int pixelsPerUnit = 300; public int border = 16; }
        [Serializable] public class Manifest { public string source = Source; public string coordinates = "source pixels, top-left origin"; public List<Entry> sprites = new List<Entry>(); }

        [MenuItem("Minglu/美术/导出印刷按钮切图")]
        public static void Export()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(Source))) throw new Exception("Cannot decode button source atlas");
            if (texture.width != 1536 || texture.height != 1024) throw new Exception("Button source layout changed; review the crop grid before export");
            var pixels = texture.GetPixels32();
            int[] columns = { 0, 530, 1005, 1536 }, rows = { 0, 223, 402, 581, 760, 1024 };
            var manifest = new Manifest();
            Directory.CreateDirectory(Destination);
            for (int row = 0; row < States.Length; row++)
            for (int column = 0; column < Families.Length; column++)
            {
                int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
                for (int y = rows[row]; y < rows[row + 1]; y++)
                for (int x = columns[column]; x < columns[column + 1]; x++)
                {
                    if (pixels[(texture.height - 1 - y) * texture.width + x].a <= 8) continue;
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                }
                if (maxX < minX || maxY < minY) throw new Exception("Empty button source cell");
                minX = Math.Max(columns[column], minX - 2); maxX = Math.Min(columns[column + 1] - 1, maxX + 2);
                minY = Math.Max(rows[row], minY - 2); maxY = Math.Min(rows[row + 1] - 1, maxY + 2);
                int width = maxX - minX + 1, height = maxY - minY + 1;
                if (width < 350 || width > 490 || height < 100 || height > 160) throw new Exception("Unexpected button alpha bounds: " + width + "x" + height);
                var sprite = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var crop = new Color32[width * height];
                for (int y = 0; y < height; y++)
                    Array.Copy(pixels, (texture.height - 1 - maxY + y) * texture.width + minX, crop, y * width, width);
                sprite.SetPixels32(crop); sprite.Apply();
                string file = Destination + "/button_" + Families[column] + "_" + States[row] + ".png";
                File.WriteAllBytes(file, sprite.EncodeToPNG());
                manifest.sprites.Add(new Entry { file = file, x = minX, y = minY, width = width, height = height });
                UnityEngine.Object.DestroyImmediate(sprite);
            }
            UnityEngine.Object.DestroyImmediate(texture);
            File.WriteAllText("ArtSource/UI/PrintButtons/sprite-manifest.json", JsonUtility.ToJson(manifest, true));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var entry in manifest.sprites)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.file);
                if (sprite == null || sprite.border.x != 16 || sprite.pixelsPerUnit != 300) throw new Exception("Invalid button import: " + entry.file);
            }
            Debug.Log("MINGLU_BUTTON_ASSETS_SUCCESS: 15 lossless RGBA sprites, 3 families x 5 states, 9-slice import and manifest verified");
        }

        public static void BatchDeliver()
        {
            try { Export(); PrototypeTools.BatchBuild(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }

    public class PrintButtonAssetImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Minglu/UI/PrintButtons/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 300;
            importer.spriteBorder = new Vector4(16, 16, 16, 16);
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
