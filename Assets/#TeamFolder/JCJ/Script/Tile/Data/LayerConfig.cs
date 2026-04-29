using System;
using UnityEngine;

namespace _TeamFolder.JCJ.TileGame
{
    /// <summary>
    /// 층별 설정. TileBoard 인스펙터 배열에서 편집.
    /// 아래층일수록 fallDelayMultiplier 작게, 기믹 비율 높게 설정.
    /// </summary>
    [Serializable]
    public class LayerConfig
    {
        [Header("Layer Info")]
        public string layerName = "Layer";
        [Tooltip("이 층 타일의 Y 위치")]
        public float yPosition  = 0f;

        [Header("Grid Size")]
        public int gridWidth = 12;
        public int gridDepth = 12;

        [Header("Timing Multiplier")]
        [Tooltip("기본 stepDelay 에 곱함. < 1.0 = 더 빨리 사라짐")]
        [Range(0.2f, 2.0f)] public float fallDelayMultiplier = 1.0f;

        [Header("Gimmick Limits")]
        [Tooltip("이 층 최대 기믹 타일 수. -1 = 제한 없음")]
        public int maxGimmickCount = 20;

        [Header("Gimmick Distribution (전체 타일 대비 비율)")]
        [Range(0f, 0.3f)] public float bombRatio       = 0.05f;
        [Range(0f, 0.3f)] public float webRatio        = 0.05f;
        [Range(0f, 0.3f)] public float iceRatio        = 0.05f;
        [Range(0f, 0.3f)] public float balloonRatio    = 0.05f;
        [Range(0f, 0.3f)] public float trampolineRatio = 0.03f;
        [Range(0f, 0.3f)] public float confusionRatio  = 0.02f;
    }
}
