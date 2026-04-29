using System;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// PlayerPrefs에 저장되는 플레이어 외형 설정 데이터.
    /// </summary>
    [Serializable]
    public class CustomizeData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public Color bodyColor = new Color(0.55f, 0.95f, 0.70f, 1f);

        public CustomizeData Clone() => (CustomizeData)MemberwiseClone();

        public void ClampAndFix()
        {
            // 저장값이 비정상이어도 렌더링 가능한 색상으로 보정한다.
            if (version <= 0) version = CurrentVersion;
            if (bodyColor.a < 0.05f) bodyColor.a = 1f;
        }
    }
}
