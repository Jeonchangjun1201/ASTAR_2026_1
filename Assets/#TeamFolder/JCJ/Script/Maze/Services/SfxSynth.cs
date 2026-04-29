using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 짧은 AudioClip을 절차 생성 — 외부 wav/mp3 없이도 타격감 있는 SFX.
    /// 호출자가 한 번 만들고 캐시. 모노 44.1kHz, 길이 대략 1.5초 이하.
    /// </summary>
    public static class SfxSynth
    {
        private const int SampleRate = 44100;

        // ── 공개 API ───────────────────

        /// <summary>짧은 고음 사인 비프 — 카운트다운 틱(3,2,1).</summary>
        public static AudioClip MakeCountdownTick() => MakeSineBeep(880f, 0.09f, 0.35f);

        /// <summary>더 크고 긴 비프 — GO! 시작.</summary>
        public static AudioClip MakeGoBeep() => MakeSineBeep(1320f, 0.28f, 0.55f);

        /// <summary>픽업·코인용 이음 상승 딩.</summary>
        public static AudioClip MakeChimePickup()
            => MakeTwoToneChime(988f /* B5 */, 1319f /* E6 */, 0.25f, 0.35f);

        /// <summary>Four-tone ascending sparkle — stamina orb.</summary>
        public static AudioClip MakeChimeStamina()
            => MakeArpeggio(new[] { 784f, 988f, 1175f, 1568f }, 0.07f, 0.35f);

        /// <summary>필터 노이즈 쿵 — 발소리.</summary>
        public static AudioClip MakeFootstep()
            => MakeFilteredNoiseThud(0.07f, 0.18f);

        /// <summary>점프 이탈용 더 묵직한 쿵.</summary>
        public static AudioClip MakeJump()
            => MakeFilteredNoiseThud(0.12f, 0.30f, lowpassCut: 350f);

        /// <summary>낮아지는 휙 소리 — 스프린트/피니시 꼬리.</summary>
        public static AudioClip MakeWhoosh()
            => MakeSweepNoise(1600f, 300f, 0.35f, 0.35f);

        /// <summary>승리 팡파르 아르페지오(장3화음+가산 6도).</summary>
        public static AudioClip MakeFanfare()
            => MakeArpeggio(new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.11f, 0.45f);

        /// <summary>스태미나 바닥 날 때 낮은 '연료 없음' 쿵.</summary>
        public static AudioClip MakeStaminaOut()
            => MakeSineBeep(180f, 0.22f, 0.40f, decay: 4f);

        /// <summary>랭크·HUD 핑용 날카로운 틱.</summary>
        public static AudioClip MakeUiTick()
            => MakeSineBeep(1760f, 0.06f, 0.28f, decay: 40f);

        // ── 합성 프리미티브 ───────────

        private static AudioClip MakeSineBeep(float freq, float duration, float volume, float decay = 8f)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[sampleCount];
            float twoPiF = Mathf.PI * 2f * freq;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = ADSR(t, duration, attack: 0.005f, decay: decay);
                samples[i] = Mathf.Sin(twoPiF * t) * env * volume;
            }
            return FinalizeClip("beep", samples);
        }

        private static AudioClip MakeTwoToneChime(float f1, float f2, float duration, float volume)
        {
            int total = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[total];
            int half = total / 2;

            for (int i = 0; i < half; i++)
            {
                float t = (float)i / SampleRate;
                float env = ADSR(t, duration * 0.5f, 0.003f, 12f);
                samples[i] = Mathf.Sin(Mathf.PI * 2f * f1 * t) * env * volume;
            }
            for (int i = half; i < total; i++)
            {
                float t = (float)(i - half) / SampleRate;
                float env = ADSR(t, duration * 0.5f, 0.003f, 12f);
                samples[i] = Mathf.Sin(Mathf.PI * 2f * f2 * t) * env * volume;
            }
            return FinalizeClip("chime", samples);
        }

        private static AudioClip MakeArpeggio(float[] freqs, float noteDur, float volume)
        {
            int noteSamples = Mathf.CeilToInt(noteDur * SampleRate);
            int total = noteSamples * freqs.Length;
            var samples = new float[total];

            for (int n = 0; n < freqs.Length; n++)
            {
                float f = freqs[n];
                int offset = n * noteSamples;
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = ADSR(t, noteDur, 0.004f, 8f);
                    float sine = Mathf.Sin(Mathf.PI * 2f * f * t);
                    float tri  = Mathf.Sin(Mathf.PI * 2f * f * 2f * t) * 0.25f;
                    samples[offset + i] = (sine + tri) * env * volume * 0.6f;
                }
            }
            return FinalizeClip("arp", samples);
        }

        private static AudioClip MakeFilteredNoiseThud(float duration, float volume, float lowpassCut = 650f)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[sampleCount];
            float prev = 0f;
            // 단일 극점 로우패스 필터로 노이즈를 둔탁하게 만든다.
            float rc = 1f / (Mathf.PI * 2f * lowpassCut);
            float dt = 1f / SampleRate;
            float alpha = dt / (rc + dt);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float env = ADSR(t, duration, 0.002f, 18f);
                float noise = Random.value * 2f - 1f;
                prev = prev + alpha * (noise - prev);
                samples[i] = prev * env * volume;
            }
            return FinalizeClip("thud", samples);
        }

        private static AudioClip MakeSweepNoise(float startFreq, float endFreq, float duration, float volume)
        {
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[sampleCount];
            float prev = 0f;
            float dt = 1f / SampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float u = t / duration;
                float f = Mathf.Lerp(startFreq, endFreq, u);
                float rc = 1f / (Mathf.PI * 2f * Mathf.Max(f, 50f));
                float alpha = dt / (rc + dt);

                float noise = Random.value * 2f - 1f;
                prev = prev + alpha * (noise - prev);

                float env = ADSR(t, duration, 0.01f, 4f);
                samples[i] = prev * env * volume;
            }
            return FinalizeClip("whoosh", samples);
        }

        /// <summary>간단 어택·감쇠 엔벨로프, [0,1]로 클램프.</summary>
        private static float ADSR(float t, float duration, float attack, float decay)
        {
            float a = Mathf.Clamp01(t / Mathf.Max(attack, 0.0001f));
            float releaseStart = Mathf.Max(0f, duration - 0.01f);
            float rel = t < releaseStart ? 1f : Mathf.InverseLerp(duration, releaseStart, t);
            float d = Mathf.Exp(-decay * t);
            return a * d * rel;
        }

        private static AudioClip FinalizeClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
