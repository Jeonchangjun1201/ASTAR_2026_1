 using System;
using UnityEngine;

// 외형 설정 저장/조회 서비스 계약 인터페이스.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 커스터마이즈 데이터의 저장, 변경 알림, 캐릭터 적용 기능을 제공하는 계약.
    /// </summary>
    public interface ICustomizeService
    {
        CustomizeData Data { get; }
        event Action<CustomizeData> OnChanged;
        void Apply(CustomizeData updated, bool persist = true);
        void Mutate(Action<CustomizeData> mutator, bool persist = true);
        void Save();
        void Load();
        void ResetToDefaults();
        void ApplyTo(GameObject characterRoot);
    }
}
