using KSY.Shared.UI;
using KSY.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace KSY.Shared
{
    public class ViewManager : MonoBehaviour
    {
        public static ViewManager Instance { get; private set; }

        private Dictionary<string, IView> _views = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
                Destroy(gameObject);
        }

        private void Initialize()
        {
            _views.Clear();

            IView[] views = gameObject.GetComponentsInChildren<IView>(includeInactive : true);
            foreach (var view in views)
            {
                if (!_views.ContainsKey(view.Name))
                {
                    _views.Add(view.Name, view);
                }
            }

            CustomLog.Log($"ÃÑ {_views.Count}°³ÀÇ UI µî·Ï", Color.green);
        }

        public T GetUI<T>(string uiName) where T : class, IView  
        {
            if (_views.TryGetValue(uiName, out IView ui))
                return ui as T;
            return null;
        }
    }
}
