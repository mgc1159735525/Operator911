using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public sealed class PaintedPortraitImport:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Minglu/Resources/Minglu/Art/Painted/")||!assetPath.EndsWith(".png"))return;
            var t=(TextureImporter)assetImporter;
            t.textureType=TextureImporterType.Default;t.alphaIsTransparency=true;
            t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;
            t.maxTextureSize=4096;t.textureCompression=TextureImporterCompression.Uncompressed;
            t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;
        }
    }
}
