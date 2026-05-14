using System.Diagnostics;
using UnityEngine;

namespace KSY.Utility
{
    public static class CustomLog
    {
        // UNITY_EDITOR �Ǵ� DEVELOPMENT_BUILD�� ���� �ڵ尡 �����Ͽ� ���Ե˴ϴ�.
        // �Ϲ� ������ ����(���� ����)������ �� �޼��带 ȣ���ϴ� ��� �ڵ尡 ������ϴ�.
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log($"[DEBUG] {message}");
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, Color color)
        {
            // Color�� hex ���ڿ��� ��ȯ (��: #FF0000)
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
            // ���� �α״� ������������ ���� �ʹٸ� [Conditional]�� ���� �˴ϴ�.
            UnityEngine.Debug.LogError($"[ERROR] {message}");
        }
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition,object message)
        {
            UnityEngine.Debug.Assert(condition, message);
        }
    }
}
