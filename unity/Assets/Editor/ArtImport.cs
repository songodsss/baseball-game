using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BaseballGame.EditorTools
{
    /// <summary>
    /// Sets import options for the art folders, so nobody has to change them by hand:
    /// pictures in Resources/Art/ become UI sprites at their own size, Resources/World/Textures/
    /// become tiling ballpark textures, and Resources/World/Characters/ become animated models.
    /// It also makes the sky material from the sky picture.
    /// </summary>
    public class ArtImport : AssetPostprocessor
    {
        const string ArtFolder = "Assets/Resources/Art/";
        const string WorldTextures = "Assets/Resources/World/Textures/";
        const string Characters = "Assets/Resources/World/Characters/";
        const string SkyTexture = WorldTextures + "sky.jpg";
        const string SkyMaterial = "Assets/Resources/World/Sky.mat";

        void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(WorldTextures))
            {
                bool sky = assetPath == SkyTexture;
                importer.textureType = TextureImporterType.Default;
                importer.wrapModeU = TextureWrapMode.Repeat;
                // A panorama wraps sideways only, and mipmaps would draw a seam where its edges meet.
                importer.wrapModeV = sky ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.mipmapEnabled = !sky;
                importer.anisoLevel = sky ? 1 : 4;
                importer.maxTextureSize = 2048;
                return;
            }

            if (!assetPath.StartsWith(ArtFolder)) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            if (assetPath.StartsWith(ArtFolder + "UI/button"))
            {
                // Kenney's button: keep the rounded corners and the bottom "depth" edge unstretched.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = new Vector4(12, 14, 12, 12); // left, bottom, right, top
                importer.SetTextureSettings(settings);
            }
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Characters)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Characters)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
                clip.loopTime = clip.name.EndsWith("Idle") || clip.name.EndsWith("Walk");
            importer.clipAnimations = clips;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Contains(SkyTexture)) EditorApplication.delayCall += EnsureSkyMaterial;
        }

        [InitializeOnLoadMethod]
        static void OnLoad() => EditorApplication.delayCall += EnsureSkyMaterial;

        /// <summary>
        /// The match camera draws the sky with this material. It's made here rather than at run time
        /// so the skybox shader goes into builds with it.
        /// </summary>
        static void EnsureSkyMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(SkyMaterial) != null) return;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTexture);
            var shader = Shader.Find("Skybox/Panoramic");
            if (texture == null || shader == null) return;
            var material = new Material(shader);
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Mapping", 1); // latitude-longitude layout
            material.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            material.SetFloat("_ImageType", 0); // full 360°
            material.SetFloat("_Exposure", 1.1f);
            AssetDatabase.CreateAsset(material, SkyMaterial);
            AssetDatabase.SaveAssets();
        }
    }
}
