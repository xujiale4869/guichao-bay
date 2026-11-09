using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Deepsea
{
    public sealed partial class SeaSeamlessWorldAndNauticalUIV29
    {
        [Serializable]
        sealed class AudioEvidenceV36
        {
            public string waterShader;
            public bool bakedStems, bakedFlourish;
            public int sampleRate, loopSamples, stemDriftSamples;
            public float dayRms, villageRms, nightRms, cueRms, reelGain, reelNightGain, reelRms, reelEntrySeconds;
            public int fish, salvage, crabPot, bounty;
        }

        float v36HarvestRms, v36ReelGain, v36ReelNightGain, v36ReelRms, v36ReelEntrySeconds;

        static float ListenerRmsV36()
        {
            var samples = new float[1024];
            AudioListener.GetOutputData(samples, 0);
            float sum = 0;
            foreach (float sample in samples) sum += sample * sample;
            return Mathf.Sqrt(sum / samples.Length);
        }

        static IEnumerator MeasureListenerRmsV36(Action<float> record)
        {
            // The first requested mixer buffer can be empty. Observe several audio updates.
            float end = Time.realtimeSinceStartup + .22f;
            float peak = 0;
            do
            {
                peak = Mathf.Max(peak, ListenerRmsV36());
                yield return null;
            }
            while (Time.realtimeSinceStartup < end);
            record(peak);
        }

        IEnumerator ReviewAdaptiveAudioV36(bool fallback)
        {
            var audio = game.GetComponent<SeaSunnyHarborAudioV36>();
            var material = waterFilter.GetComponent<Renderer>().sharedMaterial;
            var canonical = Resources.Load<Material>("SeaHarborV27/WaterShaderKeep");
            Check(canonical != null && material.shader == canonical.shader &&
                  material.shader.name == SeaSunnyHarborAudioV36.WaterShader &&
                  game.GetComponent<ApplyMistCapeEmeraldWaterV35>() == null,
                  "v36 live surface uses the original v34 harbor shader and binder");
            Check(audio != null && audio.IsReady, "v36 mix attached to the actual sea life runtime");
            if (audio == null || !audio.IsReady) yield break;

            var proof = new AudioEvidenceV36
            {
                waterShader = material.shader.name,
                bakedStems = audio.UsingBakedStems,
                bakedFlourish = audio.UsingBakedFlourish,
                sampleRate = audio.Stem(0).clip.frequency,
                loopSamples = audio.Stem(0).clip.samples
            };
            Check(audio.UsingBakedStems == !fallback && audio.UsingBakedFlourish == !fallback,
                  "v36 uses the expected authored or fallback audio");
            game.DriveInput = Vector2.zero;
            game.PlaceBoat(new Vector3(9.2f, SeaLifeRuntime.Sea, 5.65f), 255);
            yield return new WaitForSecondsRealtime(1.55f);
            Check(audio.Started && audio.CurrentTheme == SeaSunnyHarborAudioV36.Theme.Sailing &&
                  audio.Stem(0).volume > .45f && audio.Stem(3).volume < .01f,
                  "v36 daytime offshore music audible with no reeling");
            yield return MeasureListenerRmsV36(value => proof.dayRms = value);
            Check(proof.dayRms > .002f && proof.dayRms < .7f, "v36 sailing produces a bounded listener signal");
            int baseSample = audio.Stem(0).timeSamples;
            for (int i = 0; i < 4; i++)
            {
                var stem = audio.Stem(i);
                Check(stem.isPlaying && stem.loop && stem.clip.channels == 2 &&
                      stem.clip.samples == SeaSunnyHarborAudioV36.LoopSamples &&
                      stem.clip.frequency == SeaSunnyHarborAudioV36.SampleRate,
                      "v36 playing synced PCM stem " + i);
                int drift = Mathf.Abs(stem.timeSamples - baseSample);
                proof.stemDriftSamples = Mathf.Max(proof.stemDriftSamples,
                    Mathf.Min(drift, SeaSunnyHarborAudioV36.LoopSamples - drift));
            }
            Check(baseSample > 0 && proof.stemDriftSamples <= 2048, "v36 all four DSP playheads remain within one short audio buffer");

            game.PlaceBoat(game.dockBerth, 90);
            yield return new WaitForSecondsRealtime(.32f);
            Check(audio.Stem(0).volume > .04f && audio.Stem(1).volume > .04f,
                  "v36 arriving at the berth crossfades both existing playheads");
            game.ToggleBoard();
            yield return new WaitForSecondsRealtime(1.35f);
            Check(game.OnFoot && audio.CurrentTheme == SeaSunnyHarborAudioV36.Theme.Village &&
                  audio.Stem(1).volume > .45f && audio.Stem(0).volume < .02f,
                  "v36 actual landing resolves to village music");
            yield return MeasureListenerRmsV36(value => proof.villageRms = value);
            Check(proof.villageRms > .002f && proof.villageRms < .7f, "v36 village has an audible listener mix");

            game.ToggleBoard();
            game.PlaceBoat(new Vector3(9.2f, SeaLifeRuntime.Sea, 5.65f), 255);
            game.GameplayV34.CycleWatch();
            yield return new WaitForSecondsRealtime(1.55f);
            Check(game.GameplayV34.State.watch == 2 && audio.CurrentTheme == SeaSunnyHarborAudioV36.Theme.Night &&
                  audio.Stem(2).volume > .45f && audio.Stem(1).volume < .02f,
                  "v36 real dusk watch selects the night stem offshore");
            yield return MeasureListenerRmsV36(value => proof.nightRms = value);
            Check(proof.nightRms > .002f && proof.nightRms < .7f, "v36 night has an audible listener mix");
            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();
            game.GameplayV34.CycleWatch();

            if (fallback)
            {
                SeaSunnyHarborAudioV36.PlayHarvest(game, SeaSunnyHarborAudioV36.Harvest.Fish);
                yield return new WaitForSecondsRealtime(.25f);
                proof.cueRms = ListenerRmsV36();
                Check(audio.FlourishPlaying && proof.cueRms > .002f, "v36 fallback flourish plays through the mixer");
            }
            else
            {
                proof.cueRms = v36HarvestRms;
                proof.reelGain = v36ReelGain;
                proof.reelNightGain = v36ReelNightGain;
                proof.reelRms = v36ReelRms;
                proof.reelEntrySeconds = v36ReelEntrySeconds;
                proof.fish = audio.HarvestCount(SeaSunnyHarborAudioV36.Harvest.Fish);
                proof.salvage = audio.HarvestCount(SeaSunnyHarborAudioV36.Harvest.Salvage);
                proof.crabPot = audio.HarvestCount(SeaSunnyHarborAudioV36.Harvest.CrabPot);
                proof.bounty = audio.HarvestCount(SeaSunnyHarborAudioV36.Harvest.Bounty);
                Check(proof.fish == 1 && proof.salvage == 1 && proof.crabPot == 1 && proof.bounty == 1,
                    "v36 one flourish per retained fish, successful tow, pot and bounty");
            }
            File.WriteAllText(Path.Combine(evidence, "audio-v36.json"), JsonUtility.ToJson(proof, true));

            if (fallback) yield break;
            game.NoticeTime = 0;
            customSailZoom = 21.5f;
            var oldAngle = game.SetWaterReviewAngleV35(Quaternion.Euler(45, -18, 0), customSailZoom);
            yield return new WaitForSeconds(.85f);
            yield return Capture("v36-preview");
            game.SetWaterReviewAngleV35(oldAngle);
        }

        IEnumerator ReviewHarvestCueV36()
        {
            yield return new WaitForSecondsRealtime(.25f);
            var audio = game.GetComponent<SeaSunnyHarborAudioV36>();
            v36HarvestRms = ListenerRmsV36();
            Check(audio != null && audio.FlourishPlaying && v36HarvestRms > .002f,
                  "v36 retained catch starts the authored marimba and water cue");
        }

        IEnumerator ReviewReelingAudioV36()
        {
            var audio = game.GetComponent<SeaSunnyHarborAudioV36>();
            Check(audio != null && audio.IsReady && !audio.InReel && audio.ActionGain < .01f,
                  "v36 bite alert alone leaves action stem down");
            float began = Time.realtimeSinceStartup;
            game.Interact();
            Check(game.fishingStage == 4, "v36 the real E interaction enters reeling");
            if (audio == null || !audio.IsReady) yield break;
            while (Time.realtimeSinceStartup - began < .80f && game.fishingStage == 4)
            {
                yield return null;
                v36ReelGain = audio.ActionGain;
                v36ReelNightGain = audio.Stem(2).volume;
                v36ReelRms = Mathf.Max(v36ReelRms, ListenerRmsV36());
                if (audio.InReel && v36ReelGain > .24f && v36ReelNightGain < .42f && v36ReelRms > .002f) break;
            }
            v36ReelEntrySeconds = Time.realtimeSinceStartup - began;
            Debug.Log("SEA_AUDIO_V36_REEL stage=" + game.fishingStage + " action=" + v36ReelGain +
                      " night=" + v36ReelNightGain + " rms=" + v36ReelRms + " seconds=" + v36ReelEntrySeconds);
            Check(game.fishingStage == 4 && audio.InReel && v36ReelGain > .24f &&
                  v36ReelNightGain < .42f && v36ReelRms > .002f,
                  "v36 actual reeling quickly raises the action stem and ducks the night bed");
        }
    }
}
