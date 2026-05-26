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

        private Dictionary<string, PlayerDataDTO> players = null;

        public void Initialize()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            players = new Dictionary<string, PlayerDataDTO>();
        }

        private void OnApplicationQuit()
        {
            EventChannel?.InvokeEvent(new GameQuitEvent());
        }

        public void AddPlayer(string playerName, PlayerDataDTO playerData)
        {
            players[playerName] = playerData;
        }

        public void RemovePlayer(string playerName)
        {
            players.Remove(playerName);
        }

        public PlayerDataDTO GetPlayer(string playerName)
        {
            players.TryGetValue(playerName, out PlayerDataDTO playerData);
            return playerData;
        }

        public void ForEachPlayer(Action<string, PlayerDataDTO> callback)
        {
            foreach (KeyValuePair<string, PlayerDataDTO> element in players)
                callback?.Invoke(element.Key, element.Value);
        }
    }
}

