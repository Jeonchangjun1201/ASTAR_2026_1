using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    [CreateAssetMenu(fileName = "GuideUiSO", menuName = "PYH/GuideUiSO")]
    public class GuideUiSO : ScriptableObject
    {
        [field: SerializeField] public string MiniGameName { get; private set; }
        [field: SerializeField] public string MiniGameInfo { get; private set; }
        [field: SerializeField] public Sprite MiniGameIcon { get; private set; }
    }
}