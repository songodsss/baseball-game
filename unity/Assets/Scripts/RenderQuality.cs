using UnityEngine;

namespace BaseballGame
{
    /// <summary>
    /// Settings that keep the picture sharp whatever quality level the project or phone starts on:
    /// full-size textures (lower levels halve them), smooth filtering, and the screen's own resolution.
    /// </summary>
    public static class RenderQuality
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
#if UNITY_2022_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = 0;
#else
            QualitySettings.masterTextureLimit = 0;
#endif
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            QualitySettings.resolutionScalingFixedDPIFactor = 1f;
            Application.targetFrameRate = 60;
        }
    }
}
