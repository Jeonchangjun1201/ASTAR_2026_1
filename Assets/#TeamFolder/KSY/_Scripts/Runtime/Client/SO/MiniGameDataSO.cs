using UnityEngine;
using UnityEditor;

namespace KSY.Clients
{
    [CreateAssetMenu(fileName = "GameData", menuName = "KSY/SO/GameData")]
    public class MiniGameDataSO : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif

        [SerializeField] private string sceneName;

        public string SceneName => sceneName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sceneAsset != null)
            {
                sceneName = sceneAsset.name;
            }
            else
            {
                sceneName = string.Empty;
            }
        }
#endif
    }
}
