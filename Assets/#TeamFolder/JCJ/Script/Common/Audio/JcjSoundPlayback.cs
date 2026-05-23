using JHJ.Scripts.SoundManager;
using UnityEngine;
using _TeamFolder.JCJ.TileGame;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// JCJ 효과음(카운트다운·점프·발소리 등)을 전부 VFX 버스로
    /// <see cref="SoundManager"/> 싱글톤의 VFX AudioSource에 재생한다.
    /// 최종 볼륨 = MASTER × VFX(설정) × 씬 트림 × 호출자 volumeScale.
    /// </summary>
    public static class JcjSoundPlayback
    {
        /// <summary>활성 씬의 SFX 트림. MazeAudio → TileAudio 순으로 조회.</summary>
        public static float ActiveSceneVfxTrim
        {
            get
            {
                if (MazeAudio.Instance != null)
                    return MazeAudio.Instance.SceneVfxTrim;
                if (TileAudio.Instance != null)
                    return TileAudio.Instance.SceneVfxTrim;
                return 1f;
            }
        }

        /// <summary>
        /// VFX 1회 재생. SoundManager가 없으면 MonoSingleton이 자동 생성한다.
        /// </summary>
        public static void PlayVfx(
            AudioClip clip,
            float volumeScale = 1f,
            float pitch = 1f,
            float? sceneTrim = null)
        {
            if (clip == null) return;

            var sm = SoundManager.Instance;
            if (sm == null) return;

            float trim = sceneTrim ?? ActiveSceneVfxTrim;
            float vol = JcjAudioVolume.EffectiveVfx * trim * volumeScale;
            sm.PlayVfxClip(clip, vol, pitch);
        }

        public static void PlayVfxLoop(AudioClip clip, float volumeScale = 1f, float pitch = 1f, float? sceneTrim = null)
        {
            if (clip == null) return;
            var sm = SoundManager.Instance;
            if (sm == null) return;

            float trim = sceneTrim ?? ActiveSceneVfxTrim;
            float vol = JcjAudioVolume.EffectiveVfx * trim * volumeScale;
            sm.PlayVfxLoop(clip, vol, pitch);
        }

        public static void StopVfxLoop()
        {
            var sm = SoundManager.Instance;
            if (sm == null) return;
            sm.StopVfxLoop();
        }

        /// <summary>설정 MASTER를 SoundManager 레거시 출력 게인에 반영한다.</summary>
        public static void SyncSoundManagerMaster()
        {
            var sm = SoundManager.Instance;
            if (sm == null) return;
            sm.SetOutputVolume(JcjAudioVolume.Master);
        }
    }
}
