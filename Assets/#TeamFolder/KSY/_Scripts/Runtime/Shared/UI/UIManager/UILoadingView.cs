using UnityEngine;

namespace KSY.Shared.UI
{
    public class UILoadingView : MonoBehaviour
    {
        [SerializeField] private UILoadingText loadDot;
        [SerializeField] private UILoadingText loadInfo;

        private void OnEnable()
        {
            loadDot?.Initialize();
            loadInfo.Initialize();
        }

        private void OnDisable()
        {
            loadDot?.Unload();
            loadInfo?.Unload();
        }

        public void Show(string info)
        {
            loadDot?.Load();
            loadInfo?.Load(info);
        }
    }
}
