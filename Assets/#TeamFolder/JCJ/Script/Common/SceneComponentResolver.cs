using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public static class SceneComponentResolver
    {
        public static T GetOrAdd<T>(Component owner) where T : Component
        {
            if (owner == null) return null;
            var existing = owner.GetComponent<T>();
            if (existing != null) return existing;
            var childExisting = owner.GetComponentInChildren<T>(true);
            if (childExisting != null) return childExisting;
            return owner.gameObject.AddComponent<T>();
        }

        public static T FindOrCreate<T>(Transform parent = null, string name = null) where T : Component
        {
            var existing = Object.FindFirstObjectByType<T>();
            if (existing != null) return existing;

            var go = new GameObject(string.IsNullOrEmpty(name) ? typeof(T).Name : name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        public static T GetOrAddOnMainCamera<T>(string fallbackCameraName) where T : Component
        {
            var existing = Object.FindFirstObjectByType<T>();
            if (existing != null) return existing;

            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject(string.IsNullOrEmpty(fallbackCameraName) ? "Main Camera" : fallbackCameraName);
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }

            return cam.gameObject.GetComponent<T>() ?? cam.gameObject.AddComponent<T>();
        }
    }
}
