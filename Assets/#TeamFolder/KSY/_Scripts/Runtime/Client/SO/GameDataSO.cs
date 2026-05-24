using UnityEngine;

namespace KSY.Clients
{
    [CreateAssetMenu(fileName = "GameData", menuName = "SO/KSY/GameData")]
    public class GameDataSO : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset sceneAsset;
#endif

        [HideInInspector]
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
