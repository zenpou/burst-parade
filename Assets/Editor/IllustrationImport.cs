using UnityEditor;
using UnityEngine;

public sealed class IllustrationImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/")||!assetPath.EndsWith(".png"))return;
        var texture=(TextureImporter)assetImporter;
        texture.textureType=TextureImporterType.Default;
        texture.npotScale=TextureImporterNPOTScale.None;
        texture.maxTextureSize=2048;texture.mipmapEnabled=false;
        texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
        bool pilot=assetPath.StartsWith("Assets/Resources/Pilot-");
        bool sprite=pilot||assetPath.StartsWith("Assets/Resources/Rival-");
        if(pilot){texture.maxTextureSize=1024;texture.mipmapEnabled=true;texture.filterMode=FilterMode.Trilinear;}
        texture.alphaSource=sprite?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
        texture.alphaIsTransparency=sprite;
        texture.textureCompression=TextureImporterCompression.CompressedHQ;
    }
}
