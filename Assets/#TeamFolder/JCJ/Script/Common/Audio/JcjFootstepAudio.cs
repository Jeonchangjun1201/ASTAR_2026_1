using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 발소리 루프 — 걸을 때 Quick footsteps를 연속 재생, 멈추면 정지.
    /// <see cref="JcjSoundPlayback"/> → SoundManager VFX 루프 소스.
    /// </summary>
    public static class JcjFootstepAudio
    {
        private const string ResourcesClipName = "Quick footsteps";

        private static AudioClip _cachedClip;
        private static bool _walking;
        private static float _volumeScale = 0.7f;
        private static float _pitch = 1f;
        private static float? _sceneTrim;

        public static AudioClip GetClip()
        {
            if (_cachedClip != null) return _cachedClip;
            _cachedClip = Resources.Load<AudioClip>(ResourcesClipName);
            if (_cachedClip == null)
                Debug.LogWarning($"[JcjFootstepAudio] Resources/{ResourcesClipName} 클립을 찾을 수 없습니다.");
            return _cachedClip;
        }

        /// <summary>지상 이동 중이면 루프 재생, 아니면 즉시 정지.</summary>
        public static void SetWalking(bool walking, float volumeScale = 0.7f, float pitch = 1f, float? sceneTrim = null)
        {
            _volumeScale = volumeScale;
            _pitch = pitch;
            _sceneTrim = sceneTrim;

            if (!walking)
            {
                if (_walking)
                    JcjSoundPlayback.StopVfxLoop();
                _walking = false;
                return;
            }

            var clip = GetClip();
            if (clip == null) return;

            _walking = true;
            float trim = sceneTrim ?? JcjSoundPlayback.ActiveSceneVfxTrim;
            float vol = JcjAudioVolume.EffectiveVfx * trim * volumeScale;
            JcjSoundPlayback.PlayVfxLoop(clip, vol, pitch);
        }

        public static void Stop() => SetWalking(false);

        /// <summary>설정창 볼륨 변경 시 재생 중 루프 볼륨 갱신.</summary>
        public static void RefreshVolumeIfWalking()
        {
            if (!_walking) return;
            SetWalking(true, _volumeScale, _pitch, _sceneTrim);
        }
    }
}
