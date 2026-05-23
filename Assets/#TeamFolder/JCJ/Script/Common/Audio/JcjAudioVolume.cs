using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 설정창 MASTER / BGM / VFX 슬라이더에서 읽은 유효 볼륨.
    /// 실제 재생 시마다 조회해 저장 직후 즉시 반영된다.
    /// </summary>
    public static class JcjAudioVolume
    {
        public const float DefaultMaster = 1f;
        public const float DefaultBgm = 0.85f;
        public const float DefaultVfx = 0.9f;

        public static float Master => Read(data => data.masterVolume, DefaultMaster);
        public static float Bgm => Read(data => data.bgmVolume, DefaultBgm);
        public static float Vfx => Read(data => data.vfxVolume, DefaultVfx);

        /// <summary>배경음 최종 배율 = MASTER × BGM</summary>
        public static float EffectiveBgm => Mathf.Clamp01(Master * Bgm);

        /// <summary>효과음(발소리·점프·UI 등) 최종 배율 = MASTER × VFX</summary>
        public static float EffectiveVfx => Mathf.Clamp01(Master * Vfx);

        private static float Read(System.Func<SettingsData, float> selector, float fallback)
        {
            var data = SettingsService.Instance?.Data;
            return data == null ? fallback : Mathf.Clamp01(selector(data));
        }
    }
}
