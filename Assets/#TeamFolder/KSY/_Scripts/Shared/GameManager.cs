using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSY.Shared
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance = null;
        public static GameManager Instance => instance;

        private Dictionary<string, KSY_Unit> players = null;
        //private Dictionary<string, ItemBase> items = null;

        public void Initialize()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            players = new Dictionary<string, KSY_Unit>();
            //items = new Dictionary<string, ItemBase>();
        }

        public void AddPlayer(string playerID, KSY_Unit unit)
        {
            players[playerID] = unit;
        }

        public void RemovePlayer(string playerID)
        {
            players.Remove(playerID);
        }

        public KSY_Unit GetPlayer(string playerID)
        {
            players.TryGetValue(playerID, out KSY_Unit player);
            return player;
        }

        public void ForEachPlayer(Action<string, KSY_Unit> callback)
        {
            foreach (KeyValuePair<string, KSY_Unit> element in players)
                callback?.Invoke(element.Key, element.Value);
        }
    }
}

