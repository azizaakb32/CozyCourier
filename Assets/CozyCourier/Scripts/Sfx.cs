using System.Collections.Generic;
using UnityEngine;

namespace CozyCourier
{
    /// <summary>Koddan yaratiladigan yumshoq ovozlar va tinch fon "qo'ng'iroqcha" musiqasi.</summary>
    public class Sfx : MonoBehaviour
    {
        AudioSource src;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        float musicT;
        static readonly float[] Penta = { 261.6f, 293.7f, 329.6f, 392f, 440f, 523.3f, 587.3f, 659.3f };

        void Awake()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
        }

        AudioClip Tone(float freq, float dur, bool square = false)
        {
            string key = freq + "_" + dur + "_" + square;
            if (cache.TryGetValue(key, out var c)) return c;
            int rate = 44100, n = Mathf.CeilToInt(rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Min(1f, t / 0.02f) * Mathf.Exp(-t * 5f / dur);
                float s = Mathf.Sin(2f * Mathf.PI * freq * t);
                if (square) s = Mathf.Sign(s) * 0.6f;
                data[i] = s * env;
            }
            c = AudioClip.Create(key, n, 1, rate, false);
            c.SetData(data, 0);
            cache[key] = c;
            return c;
        }

        public void Chime(float freq, float dur, float vol = 0.25f) { if (Save.Sfx) src.PlayOneShot(Tone(freq, dur), vol); }
        /// <summary>Musiqa qo'ng'irog'i (ovoz effektlari sozlamasiga bog'liq emas).</summary>
        void Note(float freq, float dur, float vol) => src.PlayOneShot(Tone(freq, dur), vol);

        public void Arp(params float[] freqs)
        {
            for (int i = 0; i < freqs.Length; i++) StartCoroutine(Later(i * 0.11f, freqs[i]));
        }

        System.Collections.IEnumerator Later(float delay, float f)
        {
            yield return new WaitForSecondsRealtime(delay);
            Chime(f, 0.5f, 0.22f);
        }

        public void Whistle()
        {
            if (!Save.Sfx) return;
            src.PlayOneShot(Tone(2300, 0.14f, true), 0.12f);
            StartCoroutine(WhistleLater());
        }

        System.Collections.IEnumerator WhistleLater()
        {
            yield return new WaitForSecondsRealtime(0.18f);
            src.PlayOneShot(Tone(2300, 0.14f, true), 0.12f);
        }

        void Update()
        {
            if (!Save.Music) return;
            musicT -= Time.unscaledDeltaTime;
            if (musicT <= 0)
            {
                musicT = 1.4f + Random.value * 1.2f;
                float n = Penta[Random.Range(0, Penta.Length)];
                Note(n, 2.4f, 0.12f);
                if (Random.value < 0.4f) Note(n * 1.5f, 2.8f, 0.07f);
            }
        }
    }
}
