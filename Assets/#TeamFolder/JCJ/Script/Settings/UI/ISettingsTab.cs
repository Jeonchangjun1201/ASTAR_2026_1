using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 설정 패널의 탭들이 제목, UI 생성, 데이터 새로고침 방식을 맞추기 위한 인터페이스.
    /// </summary>
    public interface ISettingsTab
    {
        string Title { get; }
        GameObject Build(RectTransform contentArea, ISettingsService settings);
        void Refresh(SettingsData data);
    }
}
