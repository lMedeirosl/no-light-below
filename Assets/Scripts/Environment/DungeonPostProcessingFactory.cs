using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoLightBelow.Environment
{
    public static class DungeonPostProcessingFactory
    {
        public static Volume CreateOrUpdateGlobalVolume(Transform parent = null)
        {
            GameObject volumeObj = GameObject.Find("Global_PostProcessing_Volume");
            if (volumeObj == null)
            {
                volumeObj = new GameObject("Global_PostProcessing_Volume");
                if (parent != null) volumeObj.transform.SetParent(parent, false);
            }

            Volume volume = volumeObj.GetComponent<Volume>();
            if (volume == null) volume = volumeObj.AddComponent<Volume>();

            volume.isGlobal = true;
            volume.priority = 10f;

            // Create a runtime VolumeProfile
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Profile_DarkAndDirtyDungeon";

            // 1. Tonemapping (ACES Film-like curve)
            if (!profile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.mode.Override(TonemappingMode.ACES);

            // 2. Color Adjustments ("Dark & Dirty": cold, desaturated, high contrast)
            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.postExposure.Override(-0.18f); // Slightly darker overall
            colorAdj.contrast.Override(26f);       // Deep punchy blacks
            colorAdj.saturation.Override(-24f);    // Bleached, gritty medieval wash
            colorAdj.colorFilter.Override(new Color(0.92f, 0.94f, 0.98f)); // Cold subterranean tint

            // 3. Bloom (Soft glow for torch flames and glowing eyes)
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.threshold.Override(1.02f);
            bloom.intensity.Override(1.35f);
            bloom.scatter.Override(0.68f);
            bloom.tint.Override(new Color(1.0f, 0.88f, 0.65f)); // Warm amber halo

            // 4. Vignette (Dark shadows creeping on screen borders)
            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.intensity.Override(0.44f);
            vignette.smoothness.Override(0.48f);
            vignette.rounded.Override(true);
            vignette.color.Override(new Color(0.02f, 0.02f, 0.04f));

            // 5. Film Grain (Subtle gritty realism)
            if (!profile.TryGet<FilmGrain>(out var grain))
            {
                grain = profile.Add<FilmGrain>(true);
            }
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.18f);
            grain.response.Override(0.85f);

            // 6. Shadows, Midtones, Highlights (Split tone: cool abyssal shadows, warm torch highlights)
            if (!profile.TryGet<ShadowsMidtonesHighlights>(out var smh))
            {
                smh = profile.Add<ShadowsMidtonesHighlights>(true);
            }
            smh.shadows.Override(new Vector4(0.82f, 0.86f, 0.96f, -0.05f)); // Deep cold slate shadows
            smh.highlights.Override(new Vector4(1.06f, 0.96f, 0.82f, 0.04f)); // Warm torch highlights

            volume.profile = profile;

            Debug.Log("<color=#DAA520>[Post-Processing]</color> Perfil 'Dark & Dirty' URP (Bloom, Vignette, ACES, Cold Wash) aplicado com sucesso!");
            return volume;
        }
    }
}
