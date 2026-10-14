using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FractionCircles
{
    public enum Sfx { Tap, Pick, Drop, Snap, Turn, Whole, PutAway, Clear, Spawn, Start, Plink, Toggle, Card }

    /// <summary>Plays the synthesised effects through a small voice pool, plus the looping music.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        AudioSource[] voices;
        int nextVoice;
        AudioSource music;
        float musicTarget;

        const float MusicVolume = 0.32f;

        public static AudioManager Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            if (FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
            return go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            I = this;
            voices = new AudioSource[10];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0;

            clips[Sfx.Tap] = SoundSynth.Glide("tap", 900, 700, 0.09f, 40f, 0.3f);
            clips[Sfx.Toggle] = SoundSynth.Glide("toggle", 600, 1100, 0.12f, 30f, 0.2f);
            clips[Sfx.Pick] = SoundSynth.Glide("pick", 380, 720, 0.12f, 28f, 0.2f);
            clips[Sfx.Drop] = SoundSynth.Wood("drop", 420, 0.28f, 0.7f, 0.6f);
            clips[Sfx.Snap] = SoundSynth.Wood("snap", 780, 0.25f, 1.1f, 0.9f);
            clips[Sfx.Card] = SoundSynth.Wood("card", 1150, 0.18f, 0.8f, 1.0f);
            clips[Sfx.Turn] = SoundSynth.Wood("turn", 1500, 0.12f, 0.5f, 0.7f);
            clips[Sfx.Plink] = SoundSynth.Chime("plink", new float[] { 84 }, 0f, 0.7f);
            clips[Sfx.Whole] = SoundSynth.Chime("whole", new float[] { 72, 76, 79, 84, 88 }, 0.075f, 1.8f);
            clips[Sfx.Start] = SoundSynth.Jingle("start", new float[] { 60, 64, 67, 72, 76, 79, 84 }, 0.055f, 1.4f);
            clips[Sfx.PutAway] = SoundSynth.Whoosh("putaway", 0.32f, 2500, 400);
            clips[Sfx.Spawn] = SoundSynth.Whoosh("spawn", 0.3f, 600, 3000);
            clips[Sfx.Clear] = SoundSynth.Whoosh("clear", 0.6f, 4000, 250);

            SettingsListener.Attach(gameObject, Apply);
        }

        IEnumerator Start()
        {
            // Rendering the music takes a moment; do it after the first frame is on screen.
            yield return null;
            if (Edition.Basic) yield break;
            music.clip = SoundSynth.MusicLoop();
            music.Play();
            Apply();
        }

        void Apply()
        {
            AudioListener.volume = Settings.Sound ? 1f : 0f;
            musicTarget = Settings.Music ? MusicVolume : 0f;
        }

        void Update()
        {
            music.volume = Mathf.MoveTowards(music.volume, musicTarget, Time.deltaTime * 0.5f);
        }

        /// <summary>Lower the music for a moment (e.g. under a jingle).</summary>
        public void Duck(float seconds) => StartCoroutine(DuckRoutine(seconds));

        IEnumerator DuckRoutine(float seconds)
        {
            musicTarget = Settings.Music ? MusicVolume * 0.35f : 0f;
            yield return new WaitForSeconds(seconds);
            Apply();
        }

        public static void Play(Sfx sfx, float volume = 1f, float pitch = 1f, float jitter = 0.04f)
        {
            if (I == null || !I.clips.TryGetValue(sfx, out var clip)) return;

            // Avoid machine-gun stacking of the same sound in one frame.
            if (I.lastPlayed.TryGetValue(sfx, out var last) && Time.time - last < 0.03f) return;
            I.lastPlayed[sfx] = Time.time;

            var v = I.voices[I.nextVoice];
            I.nextVoice = (I.nextVoice + 1) % I.voices.Length;
            v.clip = clip;
            v.volume = volume;
            v.pitch = pitch * (1f + Random.Range(-jitter, jitter));
            v.Play();
        }
    }
}
