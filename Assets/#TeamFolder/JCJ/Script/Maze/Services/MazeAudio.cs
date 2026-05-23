using UnityEngine;

//  미로 모드 효과음을 재생하는 오디오 유틸.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 게임에서 재생할 절차적 효과음 종류.
    /// </summary>
    public enum MazeSfx
    {
        CountdownTick,
        Go,
        Pickup,
        StaminaOrb,
        StaminaOut,
        Jump,
        Footstep,
        Whoosh,
        Fanfare,
        UiTick
    }

    /// <summary>
    /// 싱글톤 오디오. SFX는 <see cref="JcjSoundPlayback"/> 경유, BGM 전용 AudioSource와 게임 이벤트 자동 구독.
    /// 클립은 <see cref="SfxSynth"/> 생성이라 에셋 불필요.
    /// </summary>
    public class MazeAudio : MonoBehaviour
    {
        public static MazeAudio Instance { get; private set; }

        [Header("씬 믹스 트림 (설정창 MASTER/BGM/VFX와 곱해짐)")]
        [Tooltip("씬별 BGM 트림. 최종 = MASTER×BGM(설정)×이 값")]
        [Range(0f, 1f)] [SerializeField] private float _musicBedTrim = 0.12f;
        [Tooltip("씬별 SFX 트림. 최종 = MASTER×VFX(설정)×이 값")]
        [Range(0f, 1f)] [SerializeField] private float _sfxTrim = 1f;

        /// <summary>씬 SFX 트림 — <see cref="JcjSoundPlayback"/> VFX 버스에 곱해진다.</summary>
        public float SceneVfxTrim => _sfxTrim;

        [Header("Behaviour")]
        [Tooltip("GameStateManager 이벤트(카운트다운, 픽업, 종료)를 자동 구독. 끄면 외부에서 직접 Play를 호출해야 함.")]
        [SerializeField] private bool _autoHookGameEvents = true;

        private AudioSource _music;

        // 캐시 클립(첫 사용 시 생성).
        private AudioClip _tickClip;
        private AudioClip _goClip;
        private AudioClip _pickupClip;
        private AudioClip _staminaOrbClip;
        private AudioClip _staminaOutClip;
        private AudioClip _jumpClip;
        private AudioClip _footstepClip;
        private AudioClip _whooshClip;
        private AudioClip _fanfareClip;
        private AudioClip _uiTickClip;
        private AudioClip _musicBed;

        private bool _subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.volume = ResolveBgmVolume();

            EnsureAudioListener();
        }

        private void Start()
        {
            if (_musicBedTrim > 0f)
            {
                _musicBed ??= BuildAmbientBed();
                if (_musicBed != null)
                {
                    _music.clip = _musicBed;
                    _music.volume = ResolveBgmVolume();
                    _music.Play();
                }
            }
            if (_autoHookGameEvents) HookEvents();
        }

        private void OnDestroy()
        {
            UnhookEvents();
            if (Instance == this) Instance = null;
        }

        // ───────── Public API ─────────

        public static void Play(MazeSfx sfx, float volumeScale = 1f, float pitch = 1f)
        {
            if (Instance == null) return;
            Instance.PlayInternal(sfx, volumeScale, pitch);
        }

        private void PlayInternal(MazeSfx sfx, float volumeScale, float pitch)
        {
            var clip = GetClip(sfx);
            if (clip == null) return;
            JcjSoundPlayback.PlayVfx(clip, volumeScale, pitch, _sfxTrim);
        }

        /// <summary>설정창 MASTER×BGM 변경 시 재생 중 BGM 볼륨을 갱신한다.</summary>
        public void ApplyUserVolumeSettings()
        {
            if (_music == null || !_music.isPlaying) return;
            var gsm = GameStateManager.Instance;
            float mul = gsm != null && gsm.CurrentState == GameState.Finished ? 0.3f : 1f;
            _music.volume = ResolveBgmVolume() * mul;
        }

        private float ResolveBgmVolume() => JcjAudioVolume.EffectiveBgm * _musicBedTrim;

        private AudioClip GetClip(MazeSfx sfx) => sfx switch
        {
            MazeSfx.CountdownTick => _tickClip        ??= SfxSynth.MakeCountdownTick(),
            MazeSfx.Go            => _goClip          ??= SfxSynth.MakeGoBeep(),
            MazeSfx.Pickup        => _pickupClip      ??= SfxSynth.MakeChimePickup(),
            MazeSfx.StaminaOrb    => _staminaOrbClip  ??= SfxSynth.MakeChimeStamina(),
            MazeSfx.StaminaOut    => _staminaOutClip  ??= SfxSynth.MakeStaminaOut(),
            MazeSfx.Jump          => _jumpClip        ??= SfxSynth.MakeJump(),
            MazeSfx.Footstep      => _footstepClip    ??= JcjFootstepAudio.GetClip() ?? SfxSynth.MakeFootstep(),
            MazeSfx.Whoosh        => _whooshClip      ??= SfxSynth.MakeWhoosh(),
            MazeSfx.Fanfare       => _fanfareClip     ??= SfxSynth.MakeFanfare(),
            MazeSfx.UiTick        => _uiTickClip      ??= SfxSynth.MakeUiTick(),
            _ => null
        };

        // ───────── Event wiring ─────────

        private void HookEvents()
        {
            if (_subscribed) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null)
            {
                // GameStateManager가 생성될 때까지 다음 프레임에 다시 연결을 시도한다.
                Invoke(nameof(HookEvents), 0.1f);
                return;
            }

            if (gsm.Countdown != null)
            {
                gsm.Countdown.OnTick += HandleCountdownTick;
                gsm.Countdown.OnGo   += HandleGo;
            }
            if (gsm.Score != null)
                gsm.Score.OnScoreChanged += HandleScoreChanged;
            if (gsm.Rank != null)
                gsm.Rank.OnPlayerFinished += HandlePlayerFinished;

            gsm.OnStateChanged += HandleStateChanged;
            _subscribed = true;
        }

        private void UnhookEvents()
        {
            if (!_subscribed) return;
            var gsm = GameStateManager.Instance;
            if (gsm != null)
            {
                if (gsm.Countdown != null)
                {
                    gsm.Countdown.OnTick -= HandleCountdownTick;
                    gsm.Countdown.OnGo   -= HandleGo;
                }
                if (gsm.Score != null)
                    gsm.Score.OnScoreChanged -= HandleScoreChanged;
                if (gsm.Rank != null)
                    gsm.Rank.OnPlayerFinished -= HandlePlayerFinished;
                gsm.OnStateChanged -= HandleStateChanged;
            }
            _subscribed = false;
        }

        private void HandleCountdownTick(int remaining) => PlayInternal(MazeSfx.CountdownTick, 1f, 1f);
        private void HandleGo()                         => PlayInternal(MazeSfx.Go, 1f, 1f);
        private void HandleScoreChanged(string n, int delta, int total)
        {
            // 코인과 오브 모두 점수 이벤트로 들어오므로 보상 크기로 효과음을 구분한다.
            PlayInternal(delta >= 10 ? MazeSfx.Pickup : MazeSfx.StaminaOrb, 1f, Random.Range(0.96f, 1.05f));
        }
        private void HandlePlayerFinished(string name, int rank)
        {
            PlayInternal(MazeSfx.Fanfare, 1f, rank == 1 ? 1.0f : (rank == 2 ? 0.9f : 0.82f));
        }
        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Finished)
            {
                // 결과 연출 중 배경 음악 볼륨 낮춤.
                if (_music != null && _music.isPlaying)
                    _music.volume = ResolveBgmVolume() * 0.3f;
            }
            else if (state == GameState.Countdown || state == GameState.Playing)
            {
                if (_music != null && _music.isPlaying)
                    _music.volume = ResolveBgmVolume();
            }
        }

        // ── 앰비언트 뮤직 베드 ───────────────

        private static AudioClip BuildAmbientBed()
        {
            // 느리고 공기감 있는 드론 — 디튠 사인 2 + 필터 노이즈 패드, 8초 루프.
            const int sr = 44100;
            const float duration = 8f;
            int total = Mathf.CeilToInt(duration * sr);
            var buffer = new float[total];

            float f1 = 110f;          // A2
            float f2 = 164.81f;       // E3 (perfect fifth)
            float f3 = 220f;          // A3

            float prev = 0f;
            float rc = 1f / (Mathf.PI * 2f * 800f);
            float dt = 1f / sr;
            float alpha = dt / (rc + dt);

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sr;
                float lfo = 0.5f + 0.5f * Mathf.Sin(Mathf.PI * 2f * 0.1f * t);
                float tone = 0.18f * Mathf.Sin(Mathf.PI * 2f * f1 * t)
                           + 0.12f * Mathf.Sin(Mathf.PI * 2f * f2 * t)
                           + 0.09f * Mathf.Sin(Mathf.PI * 2f * f3 * t);
                float noise = (Random.value * 2f - 1f) * 0.04f;
                prev = prev + alpha * (noise - prev);

                // 루프 이음새가 튀지 않도록 시작과 끝을 부드럽게 크로스페이드한다.
                float fade = 1f;
                if (t < 0.5f)         fade = t / 0.5f;
                else if (t > duration - 0.5f) fade = (duration - t) / 0.5f;

                buffer[i] = (tone * lfo + prev) * fade * 0.6f;
            }

            var clip = AudioClip.Create("MazeAmbientBed", total, 1, sr, false);
            clip.SetData(buffer, 0);
            return clip;
        }

        private static void EnsureAudioListener()
        {
            if (Object.FindFirstObjectByType<AudioListener>() != null) return;
            var cam = Camera.main;
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
        }
    }
}
