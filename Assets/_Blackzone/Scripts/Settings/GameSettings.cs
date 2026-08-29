using UnityEngine;

namespace Blackzone.Settings
{
    /// <summary>
    /// Persistent user settings (PlayerPrefs). Loaded once at boot; the settings
    /// screen mutates and saves values through this class only.
    /// </summary>
    public static class GameSettings
    {
        private const string KeySensitivity = "bz_sensitivity";
        private const string KeyAdsSensitivity = "bz_ads_sensitivity";
        private const string KeyMasterVolume = "bz_master_volume";
        private const string KeyEffectsVolume = "bz_effects_volume";
        private const string KeyQuality = "bz_quality";

        public static float Sensitivity { get; private set; } = 1f;
        public static float AdsSensitivity { get; private set; } = 0.7f;
        public static float MasterVolume { get; private set; } = 1f;
        public static float EffectsVolume { get; private set; } = 1f;

        /// <summary>0 = LOW (30fps), 1 = MEDIUM (45fps), 2 = HIGH (60fps).</summary>
        public static int Quality { get; private set; } = 1;

        public static void Load()
        {
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeySensitivity, 1f), 0.1f, 3f);
            AdsSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeyAdsSensitivity, 0.7f), 0.1f, 3f);
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMasterVolume, 1f));
            EffectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyEffectsVolume, 1f));
            Quality = Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, Application.isMobilePlatform ? 1 : 2), 0, 2);
        }

        public static void SetSensitivity(float v)
        {
            Sensitivity = Mathf.Clamp(v, 0.1f, 3f);
            PlayerPrefs.SetFloat(KeySensitivity, Sensitivity);
            PlayerPrefs.Save();
        }

        public static void SetAdsSensitivity(float v)
        {
            AdsSensitivity = Mathf.Clamp(v, 0.1f, 3f);
            PlayerPrefs.SetFloat(KeyAdsSensitivity, AdsSensitivity);
            PlayerPrefs.Save();
        }

        public static void SetMasterVolume(float v)
        {
            MasterVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
            PlayerPrefs.Save();
        }

        public static void SetEffectsVolume(float v)
        {
            EffectsVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeyEffectsVolume, EffectsVolume);
            PlayerPrefs.Save();
        }

        public static void SetQuality(int q)
        {
            Quality = Mathf.Clamp(q, 0, 2);
            PlayerPrefs.SetInt(KeyQuality, Quality);
            PlayerPrefs.Save();
            QualityApplier.Apply(Quality);
        }
    }
}
