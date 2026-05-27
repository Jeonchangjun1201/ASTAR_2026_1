using _TeamFolder.PYH._02.Scripts.Enum;
using BackEnd;
using KSY.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace KSY.Shared
{
    public class GameManager : MonoBehaviour
    {
        [field : SerializeField] public EventChannelSO EventChannel { get; private set; }

        [SerializeField] private List<MiniGameDataSO> miniGameData = null;
        [SerializeField] private Dictionary<string, MiniGameDataSO> miniGameDataDic = null;

        public bool Initialized { get; private set; }
        public string MyPlayerName => Backend.UserNickName;
        public MiniGameEnum currentMiniGame;

        private static GameManager instance = null;
        public static GameManager Instance => instance;

        private Dictionary<string, PlayerDataDTO> playerDatas = null;
        private Dictionary<string, Player> players = null;

        public void Initialize()
        {
            if (Initialized) return;
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            playerDatas = new Dictionary<string, PlayerDataDTO>();
            players = new Dictionary<string, Player>();
            Initialized = true;

            miniGameDataDic = new Dictionary<string, MiniGameDataSO>();
            if(miniGameData != null)
            {
                foreach (MiniGameDataSO data in miniGameData)
                {
                    miniGameDataDic.Add(data.SceneName, data);
                }
            }
        }

        private void OnApplicationQuit()
        {
            EventChannel?.InvokeEvent(new GameQuitEvent());
        }

        public void AddPlayer(string playerName, Player player)
        {
            players[playerName] = player;
        }

        public void RemovePlayer(string playerName)
        {
            if (!players.ContainsKey(playerName)) return;
            players.Remove(playerName);
        }

        public void AddPlayerData(string playerName, PlayerDataDTO playerData)
        {
            playerDatas[playerName] = playerData;
        }

        public void RemovePlayerData(string playerName)
        {
            if (!playerDatas.ContainsKey(playerName)) return;
            playerDatas.Remove(playerName);
        }

        public Player GetPlayer(string playerName)
        {
            players.TryGetValue(playerName, out Player player);
            return player;
        }

        public PlayerDataDTO GetPlayerData(string playerName)
        {
            playerDatas.TryGetValue(playerName, out PlayerDataDTO playerData);
            return playerData;
        }

        public void ForEachPlayer(Action<string, PlayerDataDTO> callback)
        {
            foreach (KeyValuePair<string, PlayerDataDTO> element in playerDatas)
                callback?.Invoke(element.Key, element.Value);
        }

        private System.Random sysRandom = new System.Random();

        public MiniGameDataSO SelectRandomMiniGame()
        {
            //if (miniGameData == null || miniGameData.Count == 0) return null;
            //int rand = sysRandom.Next(0, miniGameData.Count);
            //string name = miniGameData[rand].SceneName;
            //MiniGameDataSO data = miniGameDataDic[name];
            string name = miniGameData[0].SceneName;
            MiniGameDataSO data = miniGameDataDic[name];
            return data;
        }

        public MiniGameDataSO GetMiniGameData(string name)
        {
            return miniGameDataDic[name];
        }
    }
}

