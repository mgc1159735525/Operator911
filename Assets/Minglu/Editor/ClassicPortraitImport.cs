using UnityEditor;
namespace Minglu.EditorTools
{
    public sealed class ClassicPortraitImport:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Art/Classic/")||!assetPath.EndsWith(".png"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=true;
            t.mipmapEnabled=false;t.isReadable=false;t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;t.textureCompression=TextureImporterCompression.Uncompressed;t.filterMode=UnityEngine.FilterMode.Bilinear;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;
        }
    }
}
