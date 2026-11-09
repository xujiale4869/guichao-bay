using UnityEngine;

namespace Deepsea
{
    public sealed partial class SeaSunnyHarborAudioV36 : MonoBehaviour
    {
        private static float MidiToFreq(int midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

        private static AudioClip SynthesizeSunnyHarborStemClip(int stemIndex, string clipName)
        {
            const int sr = SampleRate;
            const float beat = 0.60f;      // 100 BPM 阳光海岸轻快律动
            const float eighth = 0.30f;
            const float barSec = 2.40f;    // 4/4 拍
            const int totalBars = 8;
            int totalSamples = LoopSamples + 2 * SampleRate;

            float[] left = new float[totalSamples];
            float[] right = new float[totalSamples];

            // Gmaj7 -> Cmaj9 -> Am9 -> D13sus -> Bm7 -> Em9 -> Am7 -> D9
            int[] bassNotes = { 43, 48, 45, 50, 47, 40, 45, 50 };
            int[][] arpBars =
            {
                new[] { 62, 66, 67, 71, 67, 66, 62, 67 },
                new[] { 64, 67, 71, 74, 71, 67, 64, 71 },
                new[] { 64, 67, 71, 72, 71, 67, 64, 69 },
                new[] { 60, 64, 67, 69, 67, 64, 62, 66 },
                new[] { 62, 66, 69, 74, 69, 66, 62, 69 },
                new[] { 62, 66, 67, 71, 67, 66, 64, 67 },
                new[] { 60, 64, 67, 72, 67, 64, 60, 67 },
                new[] { 60, 64, 66, 69, 66, 64, 62, 66 }
            };

            var melody = new (int bar, float beatOff, float durBeats, int note)[]
            {
                (0, 0.5f, 1.5f, 74), (0, 2.0f, 1.0f, 71), (0, 3.0f, 1.0f, 67),
                (1, 0.0f, 2.0f, 76), (1, 2.0f, 2.0f, 74),
                (2, 0.5f, 1.5f, 72), (2, 2.0f, 1.0f, 71), (2, 3.0f, 1.0f, 69),
                (3, 0.0f, 3.5f, 67),
                (4, 0.5f, 1.5f, 74), (4, 2.0f, 1.0f, 76), (4, 3.0f, 1.0f, 79),
                (5, 0.0f, 2.0f, 74), (5, 2.0f, 2.0f, 71),
                (6, 0.5f, 1.5f, 72), (6, 2.0f, 1.0f, 69), (6, 3.0f, 1.0f, 67),
                (7, 0.0f, 3.5f, 69)
            };

            for (int b = 0; b < totalBars; b++)
            {
                float bStart = b * barSec;
                int bass = bassNotes[b];
                int[] arp = arpBars[b];

                if (stemIndex == 0) // 白昼海湾：尼龙弦木吉他指弹 + 玫瑰木马林巴
                {
                    SynthUprightBass(left, right, sr, bStart, beat * 2.2f, bass, 0.21f);
                    SynthUprightBass(left, right, sr, bStart + beat * 2.5f, beat * 1.4f, bass + 7, 0.17f);
                    for (int s = 0; s < 8; s++)
                    {
                        SynthNylonGuitar(left, right, sr, bStart + s * eighth, eighth * 2.6f, arp[s], 0.20f, -0.25f + (s % 3) * 0.22f);
                    }
                    SynthRosewoodMarimba(left, right, sr, bStart + eighth * 2f, 0.9f, arp[2] + 12, 0.14f, 0.3f);
                    SynthRosewoodMarimba(left, right, sr, bStart + eighth * 5f, 0.9f, arp[3] + 12, 0.13f, 0.25f);
                }
                else if (stemIndex == 1) // 渔村与酒馆：尤克里里轻快伴奏 + 原木贝斯
                {
                    SynthUprightBass(left, right, sr, bStart, beat * 1.9f, bass, 0.22f);
                    SynthUprightBass(left, right, sr, bStart + beat * 2.0f, beat * 1.9f, bass + 7, 0.19f);
                    for (int s = 0; s < 8; s++)
                    {
                        SynthNylonGuitar(left, right, sr, bStart + s * eighth, eighth * 1.8f, arp[s] + 12, 0.15f, (s % 2 == 0) ? 0.25f : -0.25f);
                    }
                }
                else if (stemIndex == 2) // 篝火静夜：慢速温暖木吉他 + 拇指琴 Kalimba
                {
                    SynthNylonGuitar(left, right, sr, bStart, barSec * 0.95f, bass, 0.20f, -0.1f);
                    for (int s = 0; s < 8; s += 2)
                    {
                        SynthNylonGuitar(left, right, sr, bStart + s * eighth, eighth * 3.5f, arp[s], 0.17f, -0.2f);
                        SynthKalimba(left, right, sr, bStart + (s + 1) * eighth, eighth * 3.2f, arp[s + 1] + 12, 0.15f, 0.25f);
                    }
                }
                else if (stemIndex == 3) // 钓鱼咬钩：轻快有弹性的马林巴木琴律动
                {
                    for (int s = 0; s < 8; s++)
                    {
                        int nPick = arp[s] + (s % 2 == 1 ? 12 : 0);
                        SynthRosewoodMarimba(left, right, sr, bStart + s * eighth, eighth * 1.2f, nPick, 0.21f, (s % 2 == 0) ? -0.3f : 0.3f);
                    }
                }
            }

            if (stemIndex != 3)
            {
                foreach (var m in melody)
                {
                    float t0 = m.bar * barSec + m.beatOff * beat;
                    float dur = m.durBeats * beat;
                    if (stemIndex == 0)
                    {
                        SynthRosewoodMarimba(left, right, sr, t0, dur, m.note, 0.19f, 0.15f);
                    }
                    else if (stemIndex == 1)
                    {
                        SynthHarborAccordion(left, right, sr, t0, dur, m.note, 0.20f, 0.05f);
                    }
                    else if (stemIndex == 2)
                    {
                        SynthKalimba(left, right, sr, t0, dur * 1.2f, m.note + 12, 0.18f, 0.1f);
                    }
                }
            }

            FoldTails(ref left, ref right);
            ApplyWarmWoodAmbience(left, right, sr, .22f, true);
            return PackStereoClip(clipName, left, right, sr);
        }

        private static void SynthNylonGuitar(float[] l, float[] r, int sr, float startSec, float durSec, int midi, float amp, float pan)
        {
            int s0 = Mathf.Max(0, Mathf.RoundToInt(startSec * sr));
            int s1 = Mathf.Min(l.Length, s0 + Mathf.RoundToInt(durSec * sr));
            float w0 = 2f * Mathf.PI * MidiToFreq(midi) / sr;
            float panL = Mathf.Cos((pan + 1f) * Mathf.PI * 0.25f) * amp;
            float panR = Mathf.Sin((pan + 1f) * Mathf.PI * 0.25f) * amp;

            for (int i = s0; i < s1; i++)
            {
                int k = i - s0;
                float t = k / (float)sr;
                float att = Mathf.Min(1f, t / 0.006f);
                float ph = w0 * k;
                float thump = Mathf.Sin(ph * 0.5f) * Mathf.Exp(-t * 38f) * 0.18f;
                float sig = (
                    Mathf.Sin(ph) * Mathf.Exp(-t * 2.1f)
                    + 0.38f * Mathf.Sin(ph * 2f + 0.4f) * Mathf.Exp(-t * 4.8f)
                    + 0.14f * Mathf.Sin(ph * 3f + 1.1f) * Mathf.Exp(-t * 9.5f)
                    + thump
                ) * att * Mathf.Clamp01((s1 - i - 1) / (.04f * sr));
                l[i] += sig * panL;
                r[i] += sig * panR;
            }
        }

        private static void SynthRosewoodMarimba(float[] l, float[] r, int sr, float startSec, float durSec, int midi, float amp, float pan)
        {
            int s0 = Mathf.Max(0, Mathf.RoundToInt(startSec * sr));
            int s1 = Mathf.Min(l.Length, s0 + Mathf.RoundToInt(durSec * sr));
            float w0 = 2f * Mathf.PI * MidiToFreq(midi) / sr;
            float panL = Mathf.Cos((pan + 1f) * Mathf.PI * 0.25f) * amp;
            float panR = Mathf.Sin((pan + 1f) * Mathf.PI * 0.25f) * amp;

            for (int i = s0; i < s1; i++)
            {
                int k = i - s0;
                float t = k / (float)sr;
                float att = Mathf.Min(1f, t / 0.0035f);
                float ph = w0 * k;
                float sig = (
                    Mathf.Sin(ph) * Mathf.Exp(-t * 4.2f)
                    + 0.28f * Mathf.Sin(ph * 4f + 0.2f) * Mathf.Exp(-t * 18f)
                ) * att * Mathf.Clamp01((s1 - i - 1) / (.04f * sr));
                l[i] += sig * panL;
                r[i] += sig * panR;
            }
        }

        private static void SynthKalimba(float[] l, float[] r, int sr, float startSec, float durSec, int midi, float amp, float pan)
        {
            int s0 = Mathf.Max(0, Mathf.RoundToInt(startSec * sr));
            int s1 = Mathf.Min(l.Length, s0 + Mathf.RoundToInt(durSec * sr));
            float w0 = 2f * Mathf.PI * MidiToFreq(midi) / sr;
            float panL = Mathf.Cos((pan + 1f) * Mathf.PI * 0.25f) * amp;
            float panR = Mathf.Sin((pan + 1f) * Mathf.PI * 0.25f) * amp;

            for (int i = s0; i < s1; i++)
            {
                int k = i - s0;
                float t = k / (float)sr;
                float att = Mathf.Min(1f, t / 0.005f);
                float ph = w0 * k;
                float sig = (
                    Mathf.Sin(ph) * Mathf.Exp(-t * 2.5f)
                    + 0.16f * Mathf.Sin(ph * 2f) * Mathf.Exp(-t * 7f)
                    + 0.12f * Mathf.Sin(ph * 5.85f) * Mathf.Exp(-t * 24f)
                ) * att * Mathf.Clamp01((s1 - i - 1) / (.04f * sr));
                l[i] += sig * panL;
                r[i] += sig * panR;
            }
        }

        private static void SynthHarborAccordion(float[] l, float[] r, int sr, float startSec, float durSec, int midi, float amp, float pan)
        {
            int s0 = Mathf.Max(0, Mathf.RoundToInt(startSec * sr));
            int s1 = Mathf.Min(l.Length, s0 + Mathf.RoundToInt(durSec * sr));
            float freq = MidiToFreq(midi);
            float panL = Mathf.Cos((pan + 1f) * Mathf.PI * 0.25f) * amp;
            float panR = Mathf.Sin((pan + 1f) * Mathf.PI * 0.25f) * amp;

            for (int i = s0; i < s1; i++)
            {
                int k = i - s0;
                float t = k / (float)sr;
                float envIn = 0.5f - 0.5f * Mathf.Cos(Mathf.Min(1f, t / Mathf.Min(0.12f, durSec * 0.3f)) * Mathf.PI);
                float envOut = 0.5f - 0.5f * Mathf.Cos(Mathf.Min(1f, Mathf.Max(0f, (durSec - t) / Mathf.Min(0.18f, durSec * 0.35f))) * Mathf.PI);
                float env = envIn * envOut;

                float ph1 = 2f * Mathf.PI * (freq - 0.65f) * t;
                float ph2 = 2f * Mathf.PI * (freq + 0.65f) * t;
                float sig = (0.5f * (Mathf.Sin(ph1) + Mathf.Sin(ph2)) + 0.26f * Mathf.Sin(ph1 * 2f + 0.3f)) * 0.72f * env;
                l[i] += sig * panL;
                r[i] += sig * panR;
            }
        }

        private static void SynthUprightBass(float[] l, float[] r, int sr, float startSec, float durSec, int midi, float amp)
        {
            int s0 = Mathf.Max(0, Mathf.RoundToInt(startSec * sr));
            int s1 = Mathf.Min(l.Length, s0 + Mathf.RoundToInt(durSec * sr));
            float w0 = 2f * Mathf.PI * MidiToFreq(midi) / sr;
            for (int i = s0; i < s1; i++)
            {
                int k = i - s0;
                float t = k / (float)sr;
                float env = Mathf.Min(1f, t / 0.012f) * Mathf.Exp(-t * 2.8f);
                float ph = w0 * k;
                float sig = (Mathf.Sin(ph) + 0.32f * Mathf.Sin(ph * 2f) * Mathf.Exp(-t * 5.5f)) * env * amp * Mathf.Clamp01((s1 - i - 1) / (.045f * sr));
                l[i] += sig * 0.72f;
                r[i] += sig * 0.72f;
            }
        }

        private static void FoldTails(ref float[] l, ref float[] r)
        {
            for (int i = LoopSamples; i < l.Length; i++)
            {
                l[i - LoopSamples] += l[i];
                r[i - LoopSamples] += r[i];
            }
            System.Array.Resize(ref l, LoopSamples);
            System.Array.Resize(ref r, LoopSamples);
        }

        private static void ApplyWarmWoodAmbience(float[] l, float[] r, int sr, float wet, bool loop = false)
        {
            int[] delays = { Mathf.RoundToInt(0.019f * sr), Mathf.RoundToInt(0.043f * sr) };
            float[] gains = { 0.28f * wet, 0.18f * wet };
            var dryL = (float[])l.Clone();
            var dryR = (float[])r.Clone();
            for (int tap = 0; tap < delays.Length; tap++)
            {
                int d = delays[tap];
                float g = gains[tap];
                for (int i = loop ? 0 : d; i < l.Length; i++)
                {
                    int previous = i >= d ? i - d : l.Length + i - d;
                    l[i] += dryR[previous] * g;
                    r[i] += dryL[previous] * g * .95f;
                }
            }
        }

        private static AudioClip PackStereoClip(string name, float[] l, float[] r, int sr)
        {
            float peak = 0.001f;
            for (int i = 0; i < l.Length; i++) peak = Mathf.Max(peak, Mathf.Max(Mathf.Abs(l[i]), Mathf.Abs(r[i])));
            float norm = 0.85f / peak;
            float[] interleaved = new float[l.Length * 2];
            for (int i = 0; i < l.Length; i++)
            {
                interleaved[i * 2] = Mathf.Clamp(l[i] * norm, -1f, 1f);
                interleaved[i * 2 + 1] = Mathf.Clamp(r[i] * norm, -1f, 1f);
            }
            var clip = AudioClip.Create(name, l.Length, 2, sr, false);
            clip.SetData(interleaved, 0);
            return clip;
        }

        private static AudioClip SynthesizeWarmMarimbaSplashClip()
        {
            const int sr = SampleRate;
            int len = Mathf.RoundToInt(2.2f * sr);
            float[] l = new float[len];
            float[] r = new float[len];
            int[] notes = { 67, 71, 74 };
            for (int i = 0; i < notes.Length; i++)
            {
                SynthRosewoodMarimba(l, r, sr, i * 0.055f, 1.2f, notes[i], 0.19f, -0.25f + i * 0.18f);
                SynthNylonGuitar(l, r, sr, i * 0.055f, 1.4f, notes[i] - 12, 0.14f, 0.15f);
            }
            for (int drop = 0; drop < 3; drop++)
            {
                int offset = Mathf.RoundToInt((.02f + drop * .07f) * sr);
                int frames = Mathf.RoundToInt(.08f * sr);
                float frequency = 580 + drop * 290;
                for (int i = 0; i < frames; i++)
                {
                    float t = i / (float)sr;
                    float phase = 2 * Mathf.PI * frequency * (t + 4.25f * t * t);
                    float water = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * i / frames) * .16f;
                    l[offset + i] += water;
                    r[offset + i] += water;
                }
            }
            ApplyWarmWoodAmbience(l, r, sr, .20f);
            return PackStereoClip("SFX_WarmMarimbaSplash_Synth", l, r, sr);
        }

        static AudioClip SynthesizeGentleOceanSurfClip()
        {
            const int rate = 22050;
            var samples = new float[rate * 8];
            float slow = 0f, fast = 0f;
            // Filter one complete period first, so the second pass loops at steady state.
            for (int pass = 0; pass < 2; pass++)
            {
                var noise = new System.Random(3601);
                for (int i = 0; i < samples.Length; i++)
                {
                    float value = (float)(noise.NextDouble() * 2.0 - 1.0);
                    fast += (value - fast) * .12f;
                    slow += (fast - slow) * .007f;
                    float swell = .50f + .45f * Mathf.Sin(i * (Mathf.PI * 2 / samples.Length));
                    samples[i] = ((fast - slow) * .40f + slow * .30f) * swell;
                }
            }
            var clip = AudioClip.Create("Guichao Surf", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
