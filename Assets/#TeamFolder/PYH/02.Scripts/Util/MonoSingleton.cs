using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.Util
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // ReSharper disable once StaticMemberInGenericType
        private static bool _shuttingDown = false;
        // ReSharper disable once StaticMemberInGenericType
        private static readonly object Lock = new object();
        private static T _instance;
    
        public static T Instance
        {
            get
            {
                if (_shuttingDown)
                {
                    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                                          "' already destroyed. Returning null.");
                    return null;
                }
            
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindAnyObjectByType(typeof(T));
                    
                        if (_instance == null)
                        {
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString() + "(Singleton)";
                        
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(this.gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        
            Debug.Log("MonoSingleton Awake");
        }
        private void OnDestroy()
        {
            _shuttingDown = true;
        }
        private void OnApplicationQuit()
        { 
            _shuttingDown = true;
        }
    }
}