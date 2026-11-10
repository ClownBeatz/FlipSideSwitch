// FlipsidePostFX.cs
// Adds bloom and a soft vignette to ECHO's neon world only.
// ARC's bright white world is left untouched so it does not wash out.
//
// How it works with the split screen:
//   A global Volume is put on a spare layer (31). Only CameraECHO gets that
//   layer in its Volume Mask and has Post Processing turned on, so the effect
//   stays inside the bottom half. Nothing needs to be set up in the scene.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class FlipsidePostFX
{
    private const int FxLayer = 31;
    private static VolumeProfile echoProfile;

    public static void ApplyToLevel()
    {
        Camera echoCam = null;
        foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.name.ToUpperInvariant().Contains("ECHO")) { echoCam = cam; break; }
        }
        if (echoCam == null) return;

        UniversalAdditionalCameraData data = echoCam.GetUniversalAdditionalCameraData();
        if (data == null) return;

        data.renderPostProcessing = true;
        data.volumeLayerMask |= 1 << FxLayer;

        var go = new GameObject("EchoPostFX") { layer = FxLayer };
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50f;
        volume.weight = 1f;
        volume.sharedProfile = GetProfile();
    }

    private static VolumeProfile GetProfile()
    {
        if (echoProfile != null) return echoProfile;

        echoProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        echoProfile.name = "EchoNeonProfile";

        Bloom bloom = echoProfile.Add<Bloom>(true);
        bloom.threshold.Override(0.72f);
        bloom.intensity.Override(1.4f);
        bloom.scatter.Override(0.68f);
        bloom.tint.Override(new Color(0.85f, 0.7f, 1f));
        bloom.highQualityFiltering.Override(true);

        Vignette vignette = echoProfile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.08f, 0.02f, 0.16f));
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.5f);

        return echoProfile;
    }
}
