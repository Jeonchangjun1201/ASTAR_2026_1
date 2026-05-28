using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using Cysharp.Threading.Tasks;
using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Utility;
using PHY.Scripts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KSY.Clients.Handlers
{
    [PacketHandler(typeof(S2C_GameStartBroadCastPacket))]
    public class S2C_GameStartBroadCastPacketHandler : IPacketHandler<S2C_GameStartBroadCastPacket>
    {
        private readonly GameManager _gameManager;
        private readonly GameClient _gameClient;

        public S2C_GameStartBroadCastPacketHandler(GameManager gameManager, GameClient gameClient)
        {
            this._gameManager = gameManager;
            this._gameClient = gameClient;
        }

        ValueTask IPacketHandler<S2C_GameStartBroadCastPacket>.HandlePacket(Session session, S2C_GameStartBroadCastPacket packet)
        {
            return HandlePacketInternal(session, packet);
        }

        private async ValueTask HandlePacketInternal(Session session, S2C_GameStartBroadCastPacket packet)
        {
            CustomLog.Log("S2C_GameStartBroadCastPacketHandler : HandlePacket", Color.orange);

            string miniGameSceneName = packet.StartMiniGame;
            List<PlayerDataDTO> playersList = packet.PlayerList;

            //유니티의 메인스레드로 전환한다.
            await UniTask.SwitchToMainThread();

            //플레이어 데이터들을 등록한다.
            foreach (var element in playersList)
                _gameManager.AddPlayerData(element.Nickname, element);

            //KSY_MiniGameSelect가 전부 Load될 때까지 기다린다.
            await SceneManager.LoadSceneAsync("KSY_MiniGameSelect").ToUniTask();

            MiniGameEnum selectGame = MiniGameEnum.ColorMemory;
            PlayerInfo[] players = new PlayerInfo[4];

            _gameManager.ForEachPlayer((name, data) =>
            {
                players[data.Id] = new PlayerInfo(data.Id, name);
            });

            var eventArgs1 = new RandomizerMiniGameInitEvent(players);
            var eventArgs2 = new RandomizerMiniGameEvent(selectGame);

            Action<RandomizerMiniGameEvent> handler1 = (args) =>
            {
                _gameManager.currentMiniGame = args.TargetMiniGameEnum;
            };
            Action<RandomizerMiniGameEndEvent> handler2 = (args) =>
            {
                string sceneName = _gameManager.GetMiniGameData(args.SelectedMiniGameEnum).SceneName;
                SceneManager.LoadSceneAsync(sceneName);
            };

            AStarEventBus.Publish<RandomizerMiniGameInitEvent>(eventArgs1);
            AStarEventBus.Subscribe<RandomizerMiniGameEvent>(handler1);
            AStarEventBus.Subscribe<RandomizerMiniGameEndEvent>(handler2);
            AStarEventBus.Publish<RandomizerMiniGameEvent>(eventArgs2);

            C2S_PlayerResponsePacket responsePacket = new C2S_PlayerResponsePacket()
            {
                PlayerName = _gameManager.MyPlayerName,
                Position = new Vector3(0, 0, 0)
            };

            _gameClient.Send(responsePacket);
        }
    }
}