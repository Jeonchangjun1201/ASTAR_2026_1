using System;

// 설정 저장/조회와 변경 알림 서비스 계약 인터페이스.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 설정 저장, 불러오기, 변경 알림을 외부에서 일관되게 사용할 수 있게 하는 계약.
    /// </summary>
    public interface ISettingsService
    {
        SettingsData Data { get; }
        event Action<SettingsData> OnChanged;
        void Apply(SettingsData updated, bool persist = true);
        void Save();
        void Load();
        void ResetToDefaults();
        void Mutate(Action<SettingsData> mutator, bool persist = true);
    }
}
