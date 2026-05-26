using Cysharp.Threading.Tasks;
using KSY.Networks;
using KSY.Shared;
using KSY.Shared.Packets;
using KSY.Shared.UI;
using KSY.Utility;
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
            CustomLog.Log("S2C_GameStartBroadCastPacketHandler : HandlePacket 시작", Color.orange);

            string miniGameSceneName = packet.StartMiniGame;
            List<PlayerDataDTO> players = packet.PlayerList;

            await UniTask.SwitchToMainThread();

            foreach (var element in players)
                _gameManager.AddPlayer(element.Nickname, element);

            CustomLog.Log("선택 씬 로드 시작...", Color.cyan);
            await SceneManager.LoadSceneAsync("KSY_MiniGameSelect");
            CustomLog.Log("선택 씬 로드 완료!", Color.cyan);

            var rouletteObj = GameObject.Find("MiniGameRoulette");
            if (rouletteObj == null)
            {
                CustomLog.Log("ERROR: MiniGameRoulette 오브젝트를 찾을 수 없습니다!", Color.red);
                return;
            }

            var miniGameRouletteUI = rouletteObj.GetComponent<UIMiniGameRoulette>();

            for (int i = 0; i < 4; i++)
            {
                miniGameRouletteUI.playerBoxUis[i].Initialize(i, players[i].Nickname);
            }

            string nextSceneName = string.Empty;
            bool isSpinStopping = false;

            miniGameRouletteUI.OnRouletteSpinStopping += (data) =>
            {
                nextSceneName = data.SceneName;
                isSpinStopping = true;
            };

            miniGameRouletteUI.RouletteUI(_gameManager.GetMiniGameData(miniGameSceneName));

            await UniTask.WaitUntil(() => isSpinStopping);
            CustomLog.Log($"룰렛 신호 감지! 미니게임 씬으로 이동합니다: {nextSceneName}", Color.yellow);

            await SceneManager.LoadSceneAsync(nextSceneName);
            CustomLog.Log("미니게임 씬 로드 완료! 패킷을 보냅니다.", Color.cyan);

            C2S_PlayerResponsePacket responsePacket = new C2S_PlayerResponsePacket()
            {
                PlayerName = _gameManager.MyPlayerName
            };

            _gameClient.Send(responsePacket);
            CustomLog.Log("gameClient Send", Color.green);
        }
    }
}