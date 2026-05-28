using _TeamFolder.PYH._02.Scripts.Enum;
using UnityEditor;
using UnityEngine;

namespace KSY.Shared
{
    [CreateAssetMenu(fileName = "GameData", menuName = "KSY/SO/GameData")]
    public class MiniGameDataSO : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif

        [field: SerializeField] public string SceneName { get; private set; }
        [field: SerializeField] public string gameName { get; private set; }
        [field: SerializeField] public MiniGameEnum miniGameEnum { get; private set; }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sceneAsset != null)
            {
                SceneName = sceneAsset.name;
            }
            else
            {
                SceneName = string.Empty;
            }
        }
#endif
    }
}
