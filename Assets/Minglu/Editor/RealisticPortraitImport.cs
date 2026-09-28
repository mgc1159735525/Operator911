using UnityEditor;
namespace Minglu.EditorTools
{
    public class RealisticPortraitImport:AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.EndsWith("hair-atlas-v153.png")&&!assetPath.EndsWith("neck-shoulders-v153.png")&&!assetPath.EndsWith("realistic-atlas-v10.png")&&!assetPath.EndsWith("bald-atlas-v11.png")&&!assetPath.EndsWith("hair-atlas-v11.png")&&!assetPath.EndsWith("expansion-atlas-v12.png"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
