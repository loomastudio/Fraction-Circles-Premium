using UnityEngine;

namespace FractionCircles
{
    /// <summary>Small synthesiser that renders every sound effect and the music loop.</summary>
    public static class SoundSynth
    {
        public const int Rate = 44100;
        const float TwoPi = Mathf.PI * 2f;

        static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        static AudioClip ToClip(string name, float[] data, float peak = 0.9f)
        {
            Normalize(data, peak);
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Normalize(float[] d, float peak)
        {
            float max = 0.0001f;
            foreach (var v in d) max = Mathf.Max(max, Mathf.Abs(v));
            float k = peak / max;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }

        public static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

        // ---------------------------------------------------------------- effects

        /// <summary>Wooden knock: a few inharmonic damped partials plus a tiny noise transient.</summary>
        public static AudioClip Wood(string name, float freq, float length = 0.3f, float brightness = 1f, float noise = 0.5f)
        {
            var d = Buffer(length);
            float[] ratio = { 1f, 2.76f, 5.4f, 8.93f };
            float[] amp = { 1f, 0.55f * brightness, 0.28f * brightness, 0.12f * brightness };
            float[] decay = { 26f, 38f, 60f, 95f };
            var rng = new System.Random(7);
            float lp = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0;
                for (int m = 0; m < ratio.Length; m++)
                    s += amp[m] * Mathf.Exp(-decay[m] * t) * Mathf.Sin(TwoPi * freq * ratio[m] * t);
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += (n - lp) * 0.35f;
                s += lp * Mathf.Exp(-260f * t) * noise * 2f;
                d[i] = s;
            }
            return ToClip(name, d);
        }

        /// <summary>A soft "bloop": sine with an exponential pitch glide.</summary>
        public static AudioClip Glide(string name, float from, float to, float length, float decay, float harmonic = 0.15f)
        {
            var d = Buffer(length);
            float phase = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = from * Mathf.Pow(to / from, Mathf.Clamp01(t / length));
                phase += TwoPi * f / Rate;
                float env = (1f - Mathf.Exp(-t * 400f)) * Mathf.Exp(-t * decay);
                d[i] = (Mathf.Sin(phase) + harmonic * Mathf.Sin(phase * 2f)) * env;
            }
            return ToClip(name, d, 0.8f);
        }

        /// <summary>Filtered noise sweep, like something sliding past.</summary>
        public static AudioClip Whoosh(string name, float length, float cutoffFrom, float cutoffTo)
        {
            var d = Buffer(length);
            var rng = new System.Random(3);
            float a = 0, b = 0;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)length / Rate;
                float cutoff = Mathf.Lerp(cutoffFrom, cutoffTo, t);
                float k = 1f - Mathf.Exp(-TwoPi * cutoff / Rate);
                float n = (float)(rng.NextDouble() * 2 - 1);
                a += (n - a) * k;
                b += (a - b) * k;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f));
                d[i] = b * env * env;
            }
            return ToClip(name, d, 0.6f);
        }

        /// <summary>Bell-like arpeggio for a completed whole.</summary>
        public static AudioClip Chime(string name, float[] notes, float step, float length)
        {
            var d = Buffer(length);
            for (int n = 0; n < notes.Length; n++)
                AddBell(d, Mathf.RoundToInt(n * step * Rate), Midi(notes[n]), 1f - n * 0.08f, 3.2f, false);
            return ToClip(name, d, 0.8f);
        }

        /// <summary>Marimba arpeggio, used for the "start" jingle.</summary>
        public static AudioClip Jingle(string name, float[] notes, float step, float length)
        {
            var d = Buffer(length);
            for (int n = 0; n < notes.Length; n++)
                AddMarimba(d, Mathf.RoundToInt(n * step * Rate), Midi(notes[n]), 1f, false);
            return ToClip(name, d, 0.8f);
        }

        // ---------------------------------------------------------------- music

        /// <summary>A calm, seamless 8-bar loop: marimba arpeggios, a soft pad and a round bass.</summary>
        public static AudioClip MusicLoop()
        {
            const float bpm = 92f;
            float beat = 60f / bpm;
            int bars = 8;
            var d = Buffer(bars * 4 * beat);

            int[][] chords =
            {
                new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
                new[] { 60, 64, 67 }, new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 },
            };
            int[] arp = { 0, 1, 2, 3, 2, 1, 2, 1 };
            int[] melody = { 76, -1, 79, 76, 74, -1, 72, -1, 72, -1, 76, 74, 72, -1, 69, -1,
                             69, -1, 72, 74, 76, -1, 74, -1, 74, -1, 71, 72, 74, -1, -1, -1 };

            for (int bar = 0; bar < bars; bar++)
            {
                var c = chords[bar];
                int[] tones = { c[0], c[1], c[2], c[0] + 12 };
                float barStart = bar * 4 * beat;

                for (int e = 0; e < 8; e++)
                {
                    int start = Mathf.RoundToInt((barStart + e * beat * 0.5f) * Rate);
                    float vel = e % 2 == 0 ? 0.42f : 0.3f;
                    AddMarimba(d, start, Midi(tones[arp[e]]), vel, true);
                }

                if (bar >= 4)
                {
                    for (int q = 0; q < 4; q++)
                    {
                        int note = melody[(bar - 4) * 8 + q * 2];
                        if (note < 0) continue;
                        AddBell(d, Mathf.RoundToInt((barStart + q * beat) * Rate), Midi(note + 12), 0.28f, 2.2f, true);
                    }
                }

                AddBass(d, Mathf.RoundToInt(barStart * Rate), Midi(c[0] - 24), 0.55f, beat * 2f);
                AddBass(d, Mathf.RoundToInt((barStart + 2 * beat) * Rate), Midi(c[0] - 24), 0.4f, beat * 2f);

                foreach (var tone in c)
                    AddPad(d, Mathf.RoundToInt(barStart * Rate), Midi(tone), 0.09f, 4 * beat);
            }

            Normalize(d, 0.7f);
            var clip = AudioClip.Create("Music", d.Length, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        // ---------------------------------------------------------------- voices

        static void AddSample(float[] d, int index, float v, bool wrap)
        {
            if (wrap) d[index % d.Length] += v;
            else if (index < d.Length) d[index] += v;
        }

        static void AddMarimba(float[] d, int start, float f, float vel, bool wrap)
        {
            int len = Mathf.RoundToInt(1.2f * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(TwoPi * f * t) * Mathf.Exp(-t * 5.5f)
                          + 0.35f * Mathf.Sin(TwoPi * f * 3.93f * t) * Mathf.Exp(-t * 22f)
                          + 0.15f * Mathf.Sin(TwoPi * f * 9.2f * t) * Mathf.Exp(-t * 60f);
                s *= 1f - Mathf.Exp(-t * 900f);
                AddSample(d, start + i, s * vel, wrap);
            }
        }

        static void AddBell(float[] d, int start, float f, float vel, float decay, bool wrap)
        {
            int len = Mathf.RoundToInt(2.2f * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(TwoPi * f * t)
                          + 0.45f * Mathf.Sin(TwoPi * f * 2.0f * t) * Mathf.Exp(-t * 2f)
                          + 0.25f * Mathf.Sin(TwoPi * f * 3.01f * t) * Mathf.Exp(-t * 4f)
                          + 0.12f * Mathf.Sin(TwoPi * f * 4.17f * t) * Mathf.Exp(-t * 7f);
                s *= Mathf.Exp(-t * decay) * (1f - Mathf.Exp(-t * 1500f));
                AddSample(d, start + i, s * vel, wrap);
            }
        }

        static void AddBass(float[] d, int start, float f, float vel, float length)
        {
            int len = Mathf.RoundToInt(length * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = (1f - Mathf.Exp(-t * 120f)) * Mathf.Exp(-t * 2.2f) * Mathf.Clamp01((length - t) * 20f);
                float s = Mathf.Sin(TwoPi * f * t) + 0.25f * Mathf.Sin(TwoPi * f * 2f * t);
                AddSample(d, start + i, s * env * vel, true);
            }
        }

        static void AddPad(float[] d, int start, float f, float vel, float length)
        {
            int len = Mathf.RoundToInt(length * Rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.4f) * Mathf.Clamp01((length - t) / 0.5f);
                float vib = 1f + 0.003f * Mathf.Sin(TwoPi * 5f * t);
                float p = f * vib * t;
                // Soft triangle-ish tone made of the first odd harmonics.
                float s = Mathf.Sin(TwoPi * p) - Mathf.Sin(TwoPi * 3f * p) / 9f + Mathf.Sin(TwoPi * 5f * p) / 25f;
                AddSample(d, start + i, s * env * vel, true);
            }
        }
    }
}
