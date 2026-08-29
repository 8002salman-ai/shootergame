using System;
using UnityEngine;

namespace Blackzone.Audio
{
    public enum AudioId { Fire, EnemyFire, Reload, Empty, Hit, Kill, Death, EnemyDeath, Click }

    /// <summary>
    /// Procedural SFX manager with weapon-specific fire sounds. All sounds are
    /// synthesized at runtime (no licensed audio files). Each weapon class gets
    /// a distinct audio profile for feel differentiation.
    ///
    /// Audio profiles:
    ///  - AR (pitch 1.0): punchy crack with mid-range body
    ///  - SMG (pitch 1.25): fast metallic chatter, shorter
    ///  - Shotgun (pitch 0.7): deep boom with reverb tail
    ///  - DMR (pitch 0.85): sharp crack with echo
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SourceCount = 12; // slightly more for overlapping shots
        private const int SampleRate = 44100;

        public static AudioManager Instance { get; private set; }

        private readonly AudioSource[] sources = new AudioSource[SourceCount];
        private readonly System.Random rng = new System.Random(1337);
        private readonly float[] scratch = new float[65536];
        private int nextSource;
        private float effectsVolume = 1f;
        private readonly AudioClip[] clips = new AudioClip[Enum.GetValues(typeof(AudioId)).Length];

        // Weapon-specific fire clips (indexed by approximate weapon class)
        private AudioClip fireAR;
        private AudioClip fireSMG;
        private AudioClip fireShotgun;
        private AudioClip fireDMR;

        public static void EnsureInstance(Transform parent)
        {
            if (Instance != null) return;
            var go = new GameObject("AudioManager");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<AudioManager>();
            Instance.Build();
        }

        private void Build()
        {
            for (int i = 0; i < SourceCount; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.volume = 1f;
                src.priority = 128; // normal priority
                sources[i] = src;
            }

            // Generic clips
            clips[(int)AudioId.Fire] = Noise(0.09f, 0.5f, 0.0f); // fallback
            clips[(int)AudioId.EnemyFire] = Noise(0.07f, 0.35f, 0.0f);
            clips[(int)AudioId.Reload] = ReloadSound();
            clips[(int)AudioId.Empty] = Click(0.03f, 500f);
            clips[(int)AudioId.Hit] = Tone(0.05f, 260f, 0.35f);
            clips[(int)AudioId.Kill] = Sweep(0.16f, 700f, 180f, 0.4f);
            clips[(int)AudioId.Death] = Noise(0.5f, 0.6f, 0.0f);
            clips[(int)AudioId.EnemyDeath] = Sweep(0.25f, 500f, 120f, 0.35f);
            clips[(int)AudioId.Click] = Click(0.02f, 1000f);

            // Weapon-specific fire sounds
            fireAR = AssaultRifleFire();
            fireSMG = SmgFire();
            fireShotgun = ShotgunFire();
            fireDMR = DmrFire();
        }

        /// <summary>
        /// Play a fire sound. Pitch is used to select weapon-specific profile:
        ///  0.7 = shotgun, 0.85 = DMR, 1.0 = AR, 1.25 = SMG
        /// </summary>
        public void Play(AudioId id, float pitch = 1f)
        {
            AudioClip clip;

            if (id == AudioId.Fire)
            {
                // Select weapon-specific clip based on pitch
                if (pitch < 0.75f) clip = fireShotgun;
                else if (pitch < 0.92f) clip = fireDMR;
                else if (pitch > 1.1f) clip = fireSMG;
                else clip = fireAR;
            }
            else
            {
                clip = clips[(int)id];
            }

            if (clip == null) return;
            var src = sources[nextSource];
            nextSource = (nextSource + 1) % SourceCount;
            src.pitch = Mathf.Clamp(pitch * (float)(0.94 + rng.NextDouble() * 0.12), 0.5f, 2f);
            src.volume = effectsVolume;
            src.PlayOneShot(clip);
        }

        public void SetMasterVolume(float v) => AudioListener.volume = Mathf.Clamp01(v);
        public void SetEffectsVolume(float v) => effectsVolume = Mathf.Clamp01(v);

        // ==============================================================
        // WEAPON-SPECIFIC FIRE SOUNDS
        // ==============================================================

        /// <summary>Assault rifle: punchy crack with mid-range body + slight reverb tail.</summary>
        private AudioClip AssaultRifleFire()
        {
            int samples = Mathf.RoundToInt(0.12f * SampleRate);
            var data = GetScratch(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-25f * t);

                // Initial crack (high freq noise burst)
                float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * env * 0.6f;

                // Mid-range body (low freq sine)
                float body = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-15f * t) * 0.4f;

                // Slight metallic ring
                float ring = Mathf.Sin(2f * Mathf.PI * 800f * t) * Mathf.Exp(-40f * t) * 0.15f;

                data[i] = crack + body + ring;
            }
            return MakeClip("fire_ar", data, samples);
        }

        /// <summary>SMG: fast metallic chatter, shorter and sharper.</summary>
        private AudioClip SmgFire()
        {
            int samples = Mathf.RoundToInt(0.07f * SampleRate);
            var data = GetScratch(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-35f * t);

                // Sharp metallic crack
                float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * env * 0.55f;

                // High metallic ping
                float ping = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-50f * t) * 0.25f;

                // Short body
                float body = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-30f * t) * 0.3f;

                data[i] = crack + ping + body;
            }
            return MakeClip("fire_smg", data, samples);
        }

        /// <summary>Shotgun: deep boom with long reverb tail and low-frequency thump.</summary>
        private AudioClip ShotgunFire()
        {
            int samples = Mathf.RoundToInt(0.25f * SampleRate);
            var data = GetScratch(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Initial boom (noise burst + low sine)
                float boomEnv = Mathf.Exp(-8f * t);
                float boom = (float)(rng.NextDouble() * 2.0 - 1.0) * boomEnv * 0.5f;
                float lowThump = Mathf.Sin(2f * Mathf.PI * 80f * t) * boomEnv * 0.6f;

                // Reverb tail (longer decay)
                float reverbEnv = Mathf.Exp(-4f * t);
                float reverb = (float)(rng.NextDouble() * 2.0 - 1.0) * reverbEnv * 0.15f;

                // Mid-range crack at the start
                float crackEnv = Mathf.Exp(-50f * t);
                float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * crackEnv * 0.4f;

                data[i] = boom + lowThump + reverb + crack;
            }
            return MakeClip("fire_shotgun", data, samples);
        }

        /// <summary>DMR: sharp precise crack with echo.</summary>
        private AudioClip DmrFire()
        {
            int samples = Mathf.RoundToInt(0.15f * SampleRate);
            var data = GetScratch(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;

                // Sharp initial crack
                float crackEnv = Mathf.Exp(-40f * t);
                float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * crackEnv * 0.5f;

                // High-frequency snap
                float snap = Mathf.Sin(2f * Mathf.PI * 600f * t) * Mathf.Exp(-30f * t) * 0.3f;

                // Echo/delay (simulated with second pulse)
                float echoTime = 0.03f;
                float echoEnv = t > echoTime ? Mathf.Exp(-12f * (t - echoTime)) : 0f;
                float echo = (float)(rng.NextDouble() * 2.0 - 1.0) * echoEnv * 0.2f;

                // Low body
                float body = Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-10f * t) * 0.35f;

                data[i] = crack + snap + echo + body;
            }
            return MakeClip("fire_dmr", data, samples);
        }

        // ==============================================================
        // OTHER SOUNDS
        // ==============================================================

        private AudioClip ReloadSound()
        {
            int samples = Mathf.RoundToInt(0.45f * SampleRate);
            var data = GetScratch(samples);

            // Magazine out click
            AddClick(data, samples, 0.00f, 900f, 0.3f, 0.003f);
            // Insert new mag thud
            AddClick(data, samples, 0.18f, 400f, 0.4f, 0.008f);
            // Bolt release click
            AddClick(data, samples, 0.35f, 1100f, 0.35f, 0.002f);

            return MakeClip("sfx_reload", data, samples);
        }

        private AudioClip Noise(float seconds, float volume, float lowpass)
        {
            int samples = Mathf.Max(64, Mathf.RoundToInt(seconds * SampleRate));
            var data = GetScratch(samples);
            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
                prev = lowpass <= 0f ? raw : prev + (raw - prev) * lowpass;
                float env = 1f - (float)i / samples;
                data[i] = prev * volume * env * env;
            }
            return MakeClip("sfx_noise", data, samples);
        }

        private AudioClip Tone(float seconds, float freq, float volume)
        {
            int samples = Mathf.Max(64, Mathf.RoundToInt(seconds * SampleRate));
            var data = GetScratch(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-6f * t / seconds);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
            }
            return MakeClip("sfx_tone", data, samples);
        }

        private AudioClip Sweep(float seconds, float from, float to, float volume)
        {
            int samples = Mathf.Max(64, Mathf.RoundToInt(seconds * SampleRate));
            var data = GetScratch(samples);
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(from, to, t);
                phase += 2f * Mathf.PI * freq / SampleRate;
                data[i] = Mathf.Sin(phase) * volume * (1f - t);
            }
            return MakeClip("sfx_sweep", data, samples);
        }

        private AudioClip Click(float seconds, float freq)
        {
            int samples = Mathf.Max(32, Mathf.RoundToInt(seconds * SampleRate));
            var data = GetScratch(samples);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-40f * t);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * env;
            }
            return MakeClip("sfx_click", data, samples);
        }

        // ==============================================================
        // HELPERS
        // ==============================================================

        private void AddClick(float[] data, int maxSamples, float time, float freq, float volume, float duration)
        {
            int start = Mathf.RoundToInt(time * SampleRate);
            int len = Mathf.RoundToInt(duration * SampleRate);
            for (int i = start; i < Mathf.Min(maxSamples, start + len); i++)
            {
                int j = i - start;
                float env = Mathf.Exp(-j / (len * 0.3f));
                data[i] += Mathf.Sin(2f * Mathf.PI * freq * j / SampleRate) * volume * env;
            }
        }

        private float[] GetScratch(int samples)
        {
            if (samples > scratch.Length)
            {
                Array.Clear(scratch, 0, scratch.Length);
                return scratch;
            }
            Array.Clear(scratch, 0, samples);
            return scratch;
        }

        private AudioClip MakeClip(string name, float[] data, int samples)
        {
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
