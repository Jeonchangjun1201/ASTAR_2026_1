using System.Collections.Generic;
using UnityEngine;
using SfxSynth = _TeamFolder.JCJ.Script.SfxSynth;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 재생할 큐. SfxSynth 클립을 감싼 가벼운 열거. 세션당 클립은 지연 생성 후 재사용.
    /// </summary>
    public enum TileSfx
    {
        CountdownTick,
        Go,
        Footstep,
        Jump,
        Trampoline,
        Whoosh,
        TileWarn,
        TileFall,
        BombTick,
        BombExplode,
        Confuse,
        Web,
        IceSkid,
        FallScream,
        Respawn,
        ColorCallAnnounce,
        ColorCallDrop,
        EliminatePeer,
        Fanfare,
        UiTick,
    }

    /// <summary>
    /// 타일 미니게임 중앙 오디오. 외부 클립 불필요 — SfxSynth로 생성.
    /// 씬 아무 곳에 두거나 TileGameManager가 자동 추가.
    /// </summary>
    [DisallowMultipleComponent]
    public class TileAudio : MonoBehaviour
    {
        public static TileAudio Instance { get; private set; }

        [Header("Volume")]
        [Range(0f, 1f)] public float sfxVolume   = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.22f;

        [Header("음악")]
        [Tooltip("라운드 종료 시 음악 볼륨을 이 비율로 낮춤.")]
        [Range(0f, 1f)] public float duckFraction = 0.35f;

        private AudioSource _sfx;
        private AudioSource _music;
        private readonly Dictionary<TileSfx, AudioClip> _clips = new();
        private Coroutine _duckCo;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.spatialBlend = 0f;

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.volume = musicVolume;

            if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null
                && Object.FindFirstObjectByType<AudioListener>() == null)
                Camera.main.gameObject.AddComponent<AudioListener>();

            StartMusic();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ───────────────────────────────
        public static void PlayStatic(TileSfx sfx, float volume = 1f, float pitch = 1f)
        {
            if (Instance == null) return;
            Instance.Play(sfx, volume, pitch);
        }

        public void Play(TileSfx sfx, float volume = 1f, float pitch = 1f)
        {
            var clip = GetClip(sfx);
            if (clip == null || _sfx == null) return;
            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume);
        }

        public void DuckMusic(float seconds)
        {
            // 이전 덕 루틴만 취소 — StopAllCoroutines는 이 싱글톤에 예약된 다른 코루틴도 죽임.
            if (_duckCo != null) StopCoroutine(_duckCo);
            _duckCo = StartCoroutine(DuckRoutine(seconds));
        }

        private System.Collections.IEnumerator DuckRoutine(float seconds)
        {
            if (_music == null) yield break;
            float start = _music.volume;
            float target = musicVolume * duckFraction;
            float t = 0f;
            while (t < 0.25f) { t += Time.unscaledDeltaTime; _music.volume = Mathf.Lerp(start, target, t / 0.25f); yield return null; }
            yield return new WaitForSecondsRealtime(seconds);
            t = 0f;
            while (t < 1f) { t += Time.unscaledDeltaTime; _music.volume = Mathf.Lerp(target, musicVolume, t); yield return null; }
        }

        // ── Clip cache / synthesis ───────────────────
        private AudioClip GetClip(TileSfx sfx)
        {
            if (_clips.TryGetValue(sfx, out var c) && c != null) return c;
            c = Make(sfx);
            _clips[sfx] = c;
            return c;
        }

        private static AudioClip Make(TileSfx sfx)
        {
            switch (sfx)
            {
                case TileSfx.CountdownTick:      return SfxSynth.MakeCountdownTick();
                case TileSfx.Go:                 return SfxSynth.MakeGoBeep();
                case TileSfx.Footstep:           return SfxSynth.MakeFootstep();
                case TileSfx.Jump:               return SfxSynth.MakeJump();
                case TileSfx.Trampoline:         return SfxSynth.MakeJump();           // 점프 클립 재사용
                case TileSfx.Whoosh:             return SfxSynth.MakeWhoosh();
                case TileSfx.TileWarn:           return SfxSynth.MakeUiTick();
                case TileSfx.TileFall:           return SfxSynth.MakeFootstep();
                case TileSfx.BombTick:           return SfxSynth.MakeCountdownTick();
                case TileSfx.BombExplode:        return SfxSynth.MakeWhoosh();
                case TileSfx.Confuse:            return SfxSynth.MakeStaminaOut();
                case TileSfx.Web:                return SfxSynth.MakeStaminaOut();
                case TileSfx.IceSkid:            return SfxSynth.MakeWhoosh();
                case TileSfx.FallScream:         return SfxSynth.MakeStaminaOut();
                case TileSfx.Respawn:            return SfxSynth.MakeChimePickup();
                case TileSfx.ColorCallAnnounce:  return SfxSynth.MakeGoBeep();
                case TileSfx.ColorCallDrop:      return SfxSynth.MakeWhoosh();
                case TileSfx.EliminatePeer:      return SfxSynth.MakeStaminaOut();
                case TileSfx.Fanfare:            return SfxSynth.MakeFanfare();
                case TileSfx.UiTick:             return SfxSynth.MakeUiTick();
            }
            return null;
        }

        // ── 앰비언트 베드(미로와 동일 8초 드론) ──
        private void StartMusic()
        {
            var clip = BuildAmbientBed();
            if (clip == null) return;
            _music.clip = clip;
            _music.volume = musicVolume;
            _music.Play();
        }

        private static AudioClip BuildAmbientBed()
        {
            const int sr = 44100;
            const float dur = 8f;
            int len = (int)(sr * dur);
            var samples = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)sr;
                float a = 0.12f * Mathf.Sin(2f * Mathf.PI * 110f * t);
                float b = 0.09f * Mathf.Sin(2f * Mathf.PI * 165f * t + Mathf.Sin(t * 0.2f));
                float c = 0.07f * Mathf.Sin(2f * Mathf.PI * 220f * t + Mathf.Sin(t * 0.13f) * 0.5f);
                // 느린 LFO로 잔잔하게 부풀었다 줄어드는 배경음을 만든다.
                float lfo = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / 4f);
                samples[i] = (a + b + c) * lfo;
            }
            var clip = AudioClip.Create("TileAmbient", len, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
