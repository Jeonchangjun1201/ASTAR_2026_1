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
            CustomLog.Log("Create : S2C_EnterRoomBroadcastPacketHandler", Color.orange);
            this._gameManager = gameManager;
            this._gameClient = gameClient; 
        }

        ValueTask IPacketHandler<S2C_GameStartBroadCastPacket>.HandlePacket(Session session, S2C_GameStartBroadCastPacket packet)
        {
            CustomLog.Log("S2C_EnterRoomBroadcastPacketHandler : HandlePacket", Color.orange);

            string miniGameSceneName = packet.StartMiniGame;

            List<PlayerDataDTO> players = packet.PlayerList;
            foreach(var element in players)
                _gameManager.AddPlayer(element.Nickname, element);

            SceneManager.sceneLoaded += (scene, mode)=> 
            {
                var miniGameRouletteUI = GameObject.Find("MiniGameRoulette").GetComponent<UIMiniGameRoulette>();

                for(int i = 0; i < 4; i++)
                {
                    miniGameRouletteUI.playerBoxUis[i].Initialize(i, players[i].Nickname);
                }

                miniGameRouletteUI.OnRouletteSpinStopping += (data) =>
                {
                    SceneManager.LoadScene(data.SceneName);
                    SceneManager.sceneLoaded += (scene, mode) =>
                    {
                        C2S_PlayerResponsePacket packet = new C2S_PlayerResponsePacket()
                        {
                            PlayerName = GameManager.Instance.MyPlayerName
                        };
                        _gameClient.Send(packet);
                    };
                };
                miniGameRouletteUI.RouletteUI(GameManager.Instance.GetMiniGameData(miniGameSceneName));
            };
            SceneManager.LoadScene("KSY_MiniGameSelect");
            return new ValueTask();
        }
    }
}