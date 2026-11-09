using System;
using UnityEngine;

namespace Deepsea
{
    [DefaultExecutionOrder(1000)]
    public sealed partial class SeaSunnyHarborAudioV36 : MonoBehaviour
    {
        internal enum Theme { Sailing, Village, Night }
        internal enum Harvest { Fish, Salvage, CrabPot, Bounty }

        const string ResourcesRoot = "SeaAudioV36/";
        internal const int SampleRate = 44100;
        internal const int LoopSamples = 846720;
        internal const string WaterShader = "Deepsea/SeaSurfaceSparkleHarborV27";
        static readonly string[] Names = {
            "BGM_0_SunnyBay_GuitarMarimba", "BGM_1_CozyTavern_HarborAccordion",
            "BGM_2_StarlitCampfire_Kalimba", "BGM_3_LivelyCatch_MarimbaGroove"
        };
        readonly AudioSource[] stems = new AudioSource[4];
        readonly AudioClip[] generated = new AudioClip[6];
        readonly int[] harvests = new int[4];
        SeaLifeRuntime game;
        AudioSource surf, flourish;
        Vector3 bed;
        bool dockside;
        float action, cueDuck;
        double startsAt;
        public float masterMusicVolume = .70f;
        internal Theme CurrentTheme { get; private set; }
        internal bool InReel { get; private set; }
        internal bool UsingBakedStems { get; private set; }
        internal bool UsingBakedFlourish { get; private set; }
        internal bool IsReady => stems[3] != null;
        internal bool Started => IsReady && AudioSettings.dspTime > startsAt + .10;
        internal float ActionGain => stems[3].volume;
        internal AudioSource Stem(int index) => stems[index];
        internal int HarvestCount(Harvest kind) => harvests[(int)kind];
        internal bool FlourishPlaying => flourish != null && flourish.isPlaying;

        internal static bool Enabled
        {
            get
            {
#if DEEPSEA_V36
                return true;
#else
                return false;
#endif
            }
        }

        internal static void Attach(SeaLifeRuntime runtime)
        {
            if (Enabled) runtime.gameObject.AddComponent<SeaSunnyHarborAudioV36>().game = runtime;
        }

        internal static void PlayHarvest(SeaLifeRuntime runtime, Harvest kind)
        {
            if (!Enabled) return;
            var audio = runtime.GetComponent<SeaSunnyHarborAudioV36>();
            if (audio == null || audio.flourish == null) return;
            audio.harvests[(int)kind]++;
            audio.flourish.Play();
            audio.cueDuck = .20f;
        }

        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            bool forceFallback = Array.IndexOf(args, "-village-v31-check") >= 0 &&
                                 Array.IndexOf(args, "-v36-audio-fallback") >= 0;
            var clips = new AudioClip[stems.Length];
            UsingBakedStems = !forceFallback;
            for (int i = 0; i < clips.Length && !forceFallback; i++)
            {
                clips[i] = Resources.Load<AudioClip>(ResourcesRoot + Names[i]);
                UsingBakedStems &= clips[i] != null && clips[i].samples == LoopSamples &&
                                   clips[i].channels == 2 && clips[i].frequency == SampleRate;
            }
            // Use one common renderer for all stems if a resource is missing or misaligned.
            for (int i = 0; i < stems.Length; i++)
            {
                if (!UsingBakedStems) clips[i] = generated[i] = SynthesizeSunnyHarborStemClip(i, Names[i]);
                stems[i] = NewSource(clips[i], true, 72);
            }
            var cue = forceFallback ? null : Resources.Load<AudioClip>(ResourcesRoot + "SFX_WarmMarimbaSplash");
            UsingBakedFlourish = cue != null && cue.channels == 2 && cue.frequency == SampleRate;
            if (!UsingBakedFlourish) cue = generated[4] = SynthesizeWarmMarimbaSplashClip();
            flourish = NewSource(cue, false, 62);
            flourish.volume = .33f;
            generated[5] = SynthesizeGentleOceanSurfClip();
            surf = NewSource(generated[5], true, 110);

            CurrentTheme = ResolveTheme();
            bed[(int)CurrentTheme] = 1f;
            ApplyVolumes();
            // Set the common clock after assets and fallback synthesis are complete.
            startsAt = AudioSettings.dspTime + .25;
            foreach (var source in stems) source.PlayScheduled(startsAt);
            surf.PlayScheduled(startsAt);
            Debug.Log("SEA_AUDIO_V36_READY bakedStems=" + UsingBakedStems + " bakedFlourish=" + UsingBakedFlourish);
        }

        AudioSource NewSource(AudioClip clip, bool loop, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = loop;
            source.priority = priority;
            source.spatialBlend = 0;
            source.ignoreListenerPause = false;
            return source;
        }

        Theme ResolveTheme()
        {
            float distance = SeaBayGameplayLoopV34.HorizontalSqr(game.boat.position, game.dockBerth);
            float dockRadius = dockside ? 7.0f : 5.5f;
            dockside = distance <= dockRadius * dockRadius;
            if (game.OnFoot || game.InRoom) return Theme.Village;
            if (game.GameplayV34.State.watch >= 2) return Theme.Night;
            return dockside ? Theme.Village : Theme.Sailing;
        }

        void Update()
        {
            if (!IsReady) return;
            CurrentTheme = ResolveTheme();
            InReel = game.fishingStage == 4;
            var target = Vector3.zero;
            target[(int)CurrentTheme] = 1f;
            float dt = Time.unscaledDeltaTime;
            bed = Vector3.MoveTowards(bed, target, dt * 1.05f);
            action = Mathf.MoveTowards(action, InReel ? 1f : 0f, dt * (InReel ? 5f : 2f));
            cueDuck = Mathf.MoveTowards(cueDuck, 0f, dt * .22f);
            ApplyVolumes();
        }

        void ApplyVolumes()
        {
            // A bounded linear blend keeps two equivalent parts from summing to a loud peak.
            float master = Mathf.Clamp01(masterMusicVolume) * (game.Paused ? .36f : 1f);
            float bedGain = master * (.77f - .34f * action) * (1f - cueDuck);
            for (int i = 0; i < 3; i++) stems[i].volume = bed[i] * bedGain;
            stems[3].volume = master * .43f * action;
            surf.volume = game.Paused ? .07f : game.InRoom ? .045f : .24f;
        }

        void OnDestroy()
        {
            foreach (var clip in generated)
                if (clip != null) Destroy(clip);
        }
    }
}
