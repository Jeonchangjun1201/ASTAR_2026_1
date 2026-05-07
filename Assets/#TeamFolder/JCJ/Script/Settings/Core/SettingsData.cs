using System;
using UnityEngine;

// 카메라, 미니맵, 키 설정 값을 담는 데이터 모델.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미니맵을 화면 네 모서리 중 어디에 붙일지 나타낸다.
    /// </summary>
    public enum MinimapAnchorPreset
    {
        TopLeft     = 0,
        TopRight    = 1,
        BottomLeft  = 2,
        BottomRight = 3,
    }

    /// <summary>
    /// 저장 가능한 사용자 설정 데이터. PlayerPrefs에 JSON으로 저장되며 모든 값은 ClampAndFix에서 보정된다.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        [Range(0.05f, 1f)] public float cameraSensitivity = 0.18f;
        public bool lockPitch = true;

        public MinimapAnchorPreset minimapAnchor = MinimapAnchorPreset.BottomRight;
        [Range(120f, 360f)] public float minimapSize = 220f;
        public Color minimapPlayerColor = new Color(0.55f, 0.95f, 0.70f, 1f);

        public string keyMoveUp    = JCJInputActions.DefaultMoveUp;
        public string keyMoveDown  = JCJInputActions.DefaultMoveDown;
        public string keyMoveLeft  = JCJInputActions.DefaultMoveLeft;
        public string keyMoveRight = JCJInputActions.DefaultMoveRight;
        public string keyJump      = JCJInputActions.DefaultJump;
        public string keySprint    = JCJInputActions.DefaultSprint;
        public string keyMenu      = JCJInputActions.DefaultMenu;

        public SettingsData Clone()
        {
            return (SettingsData)MemberwiseClone();
        }

        public void ClampAndFix()
        {
            // 저장 파일이 손상되었거나 예전 값이 들어와도 플레이 가능한 범위로 되돌린다.
            if (version <= 0) version = CurrentVersion;
            cameraSensitivity = Mathf.Clamp(cameraSensitivity, 0.05f, 1f);
            minimapSize = Mathf.Clamp(minimapSize, 120f, 360f);
            if (minimapPlayerColor.a < 0.05f) minimapPlayerColor.a = 1f;

            keyMoveUp    = Fallback(keyMoveUp,    JCJInputActions.DefaultMoveUp);
            keyMoveDown  = Fallback(keyMoveDown,  JCJInputActions.DefaultMoveDown);
            keyMoveLeft  = Fallback(keyMoveLeft,  JCJInputActions.DefaultMoveLeft);
            keyMoveRight = Fallback(keyMoveRight, JCJInputActions.DefaultMoveRight);
            keyJump      = Fallback(keyJump,      JCJInputActions.DefaultJump);
            keySprint    = Fallback(keySprint,    JCJInputActions.DefaultSprint);
            keyMenu      = Fallback(keyMenu,      JCJInputActions.DefaultMenu);
        }

        private static string Fallback(string s, string fallback) =>
            string.IsNullOrWhiteSpace(s) ? fallback : s;

        public Vector2 GetMinimapAnchor()
        {
            // RectTransform의 anchorMin/anchorMax에 바로 넣기 쉬운 0~1 좌표로 변환한다.
            switch (minimapAnchor)
            {
                case MinimapAnchorPreset.TopLeft:     return new Vector2(0f, 1f);
                case MinimapAnchorPreset.TopRight:    return new Vector2(1f, 1f);
                case MinimapAnchorPreset.BottomLeft:  return new Vector2(0f, 0f);
                case MinimapAnchorPreset.BottomRight:
                default:                              return new Vector2(1f, 0f);
            }
        }

        public Vector2 GetMinimapAnchoredPos(float margin = 24f)
        {
            // 선택한 모서리 안쪽으로 일정 여백을 두도록 부호를 맞춘다.
            switch (minimapAnchor)
            {
                case MinimapAnchorPreset.TopLeft:     return new Vector2( margin, -margin);
                case MinimapAnchorPreset.TopRight:    return new Vector2(-margin, -margin);
                case MinimapAnchorPreset.BottomLeft:  return new Vector2( margin,  margin);
                case MinimapAnchorPreset.BottomRight:
                default:                              return new Vector2(-margin,  margin);
            }
        }
    }
}
