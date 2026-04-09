using System.Diagnostics;
using UnityEngine;

namespace KSY.Utility
{
    public static class CustomLog
    {
        // UNITY_EDITOR 또는 DEVELOPMENT_BUILD일 때만 코드가 컴파일에 포함됩니다.
        // 일반 릴리스 빌드(최종 배포)에서는 이 메서드를 호출하는 모든 코드가 사라집니다.
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log($"[DEBUG] {message}");
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, Color color)
        {
            // Color를 hex 문자열로 변환 (예: #FF0000)
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            UnityEngine.Debug.Log($"[DEBUG] <color=#{hexColor}>{message}</color>");
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning($"[WARNING] {message}");
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(object message)
        {
            // 에러 로그는 릴리스에서도 보고 싶다면 [Conditional]을 빼면 됩니다.
            UnityEngine.Debug.LogError($"[ERROR] {message}");
        }
    }
}
