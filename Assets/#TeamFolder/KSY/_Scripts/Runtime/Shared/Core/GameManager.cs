using BackEnd;
using KSY.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSY.Shared
{
    public class GameManager : MonoBehaviour
    {
        [field : SerializeField] public EventChannelSO EventChannel { get; private set; }

        [SerializeField] private List<MiniGameDataSO> miniGameData = null;
        [SerializeField] private Dictionary<string, MiniGameDataSO> miniGameDataDic = null;

        public bool Initialized { get; private set; }

        private static GameManager instance = null;
        public static GameManager Instance => instance;

        public string MyPlayerName => Backend.UserNickName;

        private Dictionary<string, PlayerDataDTO> players = null;

        public void Initialize()
        {
            if (Initialized) return;
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            players = new Dictionary<string, PlayerDataDTO>();
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

        private System.Random sysRandom = new System.Random(); // 클래스 상단에 멤버 변수로 선언

        public MiniGameDataSO SelectRandomMiniGame()
        {
            if (miniGameData == null || miniGameData.Count == 0) return null;
            int rand = sysRandom.Next(0, miniGameData.Count);
            string name = miniGameData[rand].SceneName;
            MiniGameDataSO data = miniGameDataDic[name];

            return data;
        }

        public MiniGameDataSO GetMiniGameData(string name)
        {
            return miniGameDataDic[name];
        }
    }
}

