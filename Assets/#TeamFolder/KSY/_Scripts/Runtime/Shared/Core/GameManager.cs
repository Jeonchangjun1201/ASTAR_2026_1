using KSY.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSY.Shared
{
    public class GameManager : MonoBehaviour
    {
        [field : SerializeField] public EventChannelSO EventChannel { get; private set; }

        private static GameManager instance = null;
        public static GameManager Instance => instance;

        private Dictionary<string, Player> players = null;

        public void Initialize()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            players = new Dictionary<string, Player>();
        }

        private void OnApplicationQuit()
        {
            EventChannel?.InvokeEvent(new GameQuitEvent());
        }

        public void AddPlayer(string playerID, Player unit)
        {
            players[playerID] = unit;
        }

        public void RemovePlayer(string playerID)
        {
            players.Remove(playerID);
        }

        public Player GetPlayer(string playerID)
        {
            players.TryGetValue(playerID, out Player player);
            return player;
        }

        public void ForEachPlayer(Action<string, Player> callback)
        {
            foreach (KeyValuePair<string, Player> element in players)
                callback?.Invoke(element.Key, element.Value);
        }
    }
}

