using KSY.Utility;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

namespace KSY.Shared
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO eventChannel;

        private static GameManager instance = null;
        public static GameManager Instance => instance;

        private Dictionary<string, Unit> players = null;

        public void Initialize()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            players = new Dictionary<string, Unit>();
        }

        private void OnApplicationQuit()
        {

        }

        public void AddPlayer(string playerID, Unit unit)
        {
            players[playerID] = unit;
        }

        public void RemovePlayer(string playerID)
        {
            players.Remove(playerID);
        }

        public Unit GetPlayer(string playerID)
        {
            players.TryGetValue(playerID, out Unit player);
            return player;
        }

        public void ForEachPlayer(Action<string, Unit> callback)
        {
            foreach (KeyValuePair<string, Unit> element in players)
                callback?.Invoke(element.Key, element.Value);
        }
    }
}

