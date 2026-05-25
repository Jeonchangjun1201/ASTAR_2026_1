using UnityEngine;

namespace KSY.Shared.UI
{
    public class UILoadingView : MonoBehaviour, IView
    {
        [SerializeField] private UILoadingText loadDot;
        [SerializeField] private UILoadingText loadInfo;

        public string Name => gameObject.name;

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
            gameObject.SetActive(true);
            loadDot?.Load();
            loadInfo?.Load(info);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
