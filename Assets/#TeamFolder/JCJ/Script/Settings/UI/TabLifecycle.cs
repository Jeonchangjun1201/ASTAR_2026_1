using System;
using UnityEngine;

// 탭 열림/닫힘 시점 공통 동작을 관리하는 보조 컴포넌트.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 코드로 만든 탭 콘텐츠가 파괴될 때 정리 콜백을 실행하는 작은 생명주기 도우미.
    /// </summary>
    public class TabLifecycle : MonoBehaviour
    {
        public Action OnDestroyed;

        private void OnDestroy()
        {
            try { OnDestroyed?.Invoke(); }
            catch (Exception e) { Debug.LogWarning($"[TabLifecycle] dispose error: {e.Message}"); }
        }
    }
}
