using System;
using UnityEngine;

namespace Blackzone.Audio
{
    public enum AudioId { Fire, EnemyFire, Reload, Empty, Hit, Kill, Death, EnemyDeath, Click }

    /// <summary>
    /// Procedural SFX manager: all Phase 1 sounds are synthesized at runtime
    /// (no licensed audio files), played through a small source pool.
    /// Master volume maps to AudioListener; effects volume scales each shot.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SourceCount = 8;
        private const int SampleRate = 44100;

        public static AudioManager Instance { get; private set; }

        private readonly AudioSource[] sources = new AudioSource[SourceCount];
        private readonly System.Random rng = new System.Random(1337);
        private readonly float[] scratch = new float[65536];
        private int nextSource;
        private float effectsVolume = 1f;
        private readonly AudioClip[] clips = new AudioClip[Enum.GetValues(typeof(AudioId)).Length];

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
                sources[i] = src;
            }

            clips[(int)AudioId.Fire] = Noise(0.09f, 0.5f, 0.0f);
            clips[(int)AudioId.EnemyFire] = Noise(0.07f, 0.35f, 0.0f);
            clips[(int)AudioId.Reload] = Clicks(new[] { 0.00f, 0.12f, 0.32f });
            clips[(int)AudioId.Empty] = Click(0.03f, 500f);
            clips[(int)AudioId.Hit] = Tone(0.05f, 260f, 0.35f);
            clips[(int)AudioId.Kill] = Sweep(0.16f, 700f, 180f, 0.4f);
            clips[(int)AudioId.Death] = Noise(0.5f, 0.6f, 0.0f);
            clips[(int)AudioId.EnemyDeath] = Sweep(0.25f, 500f, 120f, 0.35f);
            clips[(int)AudioId.Click] = Click(0.02f, 1000f);
        }

        public void Play(AudioId id, float pitch = 1f)
        {
            var clip = clips[(int)id];
            if (clip == null) return;
            var src = sources[nextSource];
            nextSource = (nextSource + 1) % SourceCount;
            src.pitch = Mathf.Clamp(pitch * (float)(0.94 + rng.NextDouble() * 0.12), 0.5f, 2f);
            src.volume = effectsVolume;
            src.PlayOneShot(clip);
        }

        public void SetMasterVolume(float v) => AudioListener.volume = Mathf.Clamp01(v);
        public void SetEffectsVolume(float v) => effectsVolume = Mathf.Clamp01(v);

        // --- Synthesis helpers ---

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

        private AudioClip Clicks(float[] times)
        {
            int samples = Mathf.RoundToInt(0.45f * SampleRate);
            var data = GetScratch(samples);
            for (int c = 0; c < times.Length; c++)
            {
                int start = Mathf.RoundToInt(times[c] * SampleRate);
                for (int i = start; i < Mathf.Min(samples, start + 1800); i++)
                {
                    int j = i - start;
                    float env = Mathf.Exp(-j / 400f);
                    data[i] += Mathf.Sin(2f * Mathf.PI * 900f * j / SampleRate) * 0.4f * env;
                }
            }
            return MakeClip("sfx_reload", data, samples);
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

        private float[] GetScratch(int samples)
        {
            if (samples > scratch.Length)
            {
                // Cap: resize scratch only when a clip is longer than the buffer.
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
