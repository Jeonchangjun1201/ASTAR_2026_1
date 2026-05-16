using UnityEngine;

// UI에서 재사용하는 색상 상수 모음.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로·타일 HUD에서 공유하는 색의 단일 정의. 여기만 바꾸면 두 미니게임 테마가 함께 바뀐다.
    /// </summary>
    public static class JCJUiColors
    {
        public static readonly Color HudPanel        = new(0.045f, 0.055f, 0.085f, 0.88f);
        public static readonly Color HudAccentLine   = new(0.45f, 0.72f, 1.00f, 0.92f);
        public static readonly Color HudAccent       = new(0.82f, 0.88f, 0.98f, 1.00f);
        public static readonly Color HudAccentBright = new(1.00f, 1.00f, 1.00f, 1.00f);
        public static readonly Color HudPrimaryText  = new(0.96f, 0.97f, 0.99f, 1.00f);
        public static readonly Color HudMutedText    = new(0.58f, 0.64f, 0.74f, 1.00f);
        public static readonly Color HudDanger       = new(0.92f, 0.30f, 0.30f, 1.00f);
        public static readonly Color HudDangerSoft   = new(1.00f, 0.48f, 0.50f, 1.00f);
        public static readonly Color HudShadow       = new(0.00f, 0.00f, 0.00f, 0.42f);
        public static readonly Color HudTextOutline  = new(0.02f, 0.03f, 0.065f, 0.86f);

        public static readonly Color PodiumPanel     = new(0.035f, 0.040f, 0.055f, 0.92f);
        public static readonly Color PodiumFirst     = new(0.97f, 0.98f, 1.00f, 1.00f);
        public static readonly Color PodiumSecond    = new(0.76f, 0.79f, 0.85f, 1.00f);
        public static readonly Color PodiumThird     = new(0.52f, 0.54f, 0.58f, 1.00f);
        public static readonly Color PodiumBody      = new(0.95f, 0.96f, 0.98f, 1.00f);
        public static readonly Color PodiumMuted     = new(0.62f, 0.64f, 0.68f, 1.00f);
    }
}
