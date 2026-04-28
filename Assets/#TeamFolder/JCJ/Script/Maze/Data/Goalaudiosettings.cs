using UnityEngine;
 
namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 골 지점에서 들리는 힌트 사운드의 클립, 반복 간격, 3D 거리 감쇠 값을 담는다.
    /// </summary>
    [CreateAssetMenu(fileName = "GoalAudioSettings", menuName = "Maze/GoalAudioSettings")]
    public class GoalAudioSettings : ScriptableObject
    {
        [Header("Sound Clip")]
        [Tooltip("도착지점에서 주기적으로 재생할 클립")]
        public AudioClip hintClip;
 
        [Header("Interval")]
        [Tooltip("소리 재생 간격 (초)")]
        [Range(0.5f, 10f)] public float interval = 3f;
 
        [Header("Audio Distance")]
        [Tooltip("소리가 들리기 시작하는 최소 거리")]
        [Range(1f, 10f)] public float minDistance = 3f;
 
        [Tooltip("소리가 완전히 들리지 않는 최대 거리 — cellSize * 미로크기 절반 정도로 설정")]
        [Range(10f, 300f)] public float maxDistance = 80f;
    }
}