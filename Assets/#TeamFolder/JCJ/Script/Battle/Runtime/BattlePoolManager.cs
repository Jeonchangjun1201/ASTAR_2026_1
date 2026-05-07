using System;
using System.Collections.Generic;
using UnityEngine;

// 발사체와 이펙트를 재사용하기 위한 오브젝트 풀 관리자.

namespace _TeamFolder.JCJ.Battle
{
    public interface IBattlePoolAware
    {
        void OnSpawnedFromPool();
        void OnReturnedToPool();
    }

    public sealed class BattlePooledObject : MonoBehaviour
    {
        [SerializeField] private string _poolKey;

        public string PoolKey => _poolKey;

        internal void Bind(string poolKey)
        {
            _poolKey = poolKey;
        }
    }

    public class BattlePoolManager : MonoBehaviour
    {
        private sealed class PoolEntry
        {
            public readonly Queue<GameObject> Inactive = new();
        }

        private readonly Dictionary<string, PoolEntry> _entries = new(StringComparer.Ordinal);
        private Transform _poolRoot;

        public static BattlePoolManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _poolRoot = new GameObject("RuntimePool").transform;
            _poolRoot.SetParent(transform, false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static BattlePoolManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var root = new GameObject(nameof(BattlePoolManager));
            return root.AddComponent<BattlePoolManager>();
        }

        // 풀에서 오브젝트를 꺼내거나 새로 만들어 초기 위치에 배치하는 공용 진입점이다.
        // 서버 동기화와 직접 연결되진 않지만, 발사체/이펙트 재생 수명이 여기로 집중된다.
        public static GameObject Spawn(string poolKey, Func<GameObject> factory, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var manager = EnsureInstance();
            return manager.SpawnInternal(poolKey, factory, position, rotation, parent);
        }

        // 사용이 끝난 오브젝트를 풀로 돌려보내는 공용 진입점이다.
        // 네트워크 이펙트 재생이 많아질수록 생성/파괴 비용을 줄이는 핵심 경로다.
        public static void Release(GameObject instance)
        {
            if (instance == null) return;

            var pooled = instance.GetComponent<BattlePooledObject>();
            if (pooled == null)
            {
                Destroy(instance);
                return;
            }

            var manager = EnsureInstance();
            manager.ReleaseInternal(pooled);
        }

        private GameObject SpawnInternal(string poolKey, Func<GameObject> factory, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (string.IsNullOrEmpty(poolKey)) throw new ArgumentException("poolKey");
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            if (!_entries.TryGetValue(poolKey, out var entry))
            {
                entry = new PoolEntry();
                _entries.Add(poolKey, entry);
            }

            GameObject instance = null;
            while (entry.Inactive.Count > 0 && instance == null)
                instance = entry.Inactive.Dequeue();

            if (instance == null)
            {
                instance = factory.Invoke();
                if (instance == null) throw new InvalidOperationException($"Pool factory returned null for key '{poolKey}'.");
                var pooled = instance.GetComponent<BattlePooledObject>();
                if (pooled == null) pooled = instance.AddComponent<BattlePooledObject>();
                pooled.Bind(poolKey);
            }

            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            var listeners = instance.GetComponents<IBattlePoolAware>();
            for (int i = 0; i < listeners.Length; i++)
                listeners[i].OnSpawnedFromPool();

            return instance;
        }

        private void ReleaseInternal(BattlePooledObject pooled)
        {
            if (pooled == null || string.IsNullOrEmpty(pooled.PoolKey))
            {
                if (pooled != null) Destroy(pooled.gameObject);
                return;
            }

            if (!_entries.TryGetValue(pooled.PoolKey, out var entry))
            {
                entry = new PoolEntry();
                _entries.Add(pooled.PoolKey, entry);
            }

            var instance = pooled.gameObject;
            var listeners = instance.GetComponents<IBattlePoolAware>();
            for (int i = 0; i < listeners.Length; i++)
                listeners[i].OnReturnedToPool();

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot, false);
            entry.Inactive.Enqueue(instance);
        }
    }
}
