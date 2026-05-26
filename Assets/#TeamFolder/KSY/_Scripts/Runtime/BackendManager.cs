using System.Net;
using System.Net.Sockets;
using BackEnd;
using BackEnd.Tcp;
using KSY.Clients;
using KSY.Servers;
using KSY.Shared.UI;
using KSY.Utility;
using LitJson;
using TMPro;
using UnityEngine;

namespace KSY.Shared
{
    public class BackendManager : MonoBehaviour
    {
        public static BackendManager Instance { get; private set; }
        public ServerBootstrap serverBootstrap { get; private set; }
        public ClientBootstrap clientBootstrap { get; private set; }

        private const string TABLE_NAME_RoomCodes = "RoomCodes";
        private const string COLUMN_NAME_RoomCode = "RoomCode";
        private const string COLUMN_NAME_HostIP = "HostIP";
        private string _myRoomCode;

        // [핵심] 뒤끝 SDK에 방 존재 여부 함수가 없으므로, 로컬에서 상태를 직접 추적합니다.
        private bool _isInMatchRoom = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
                Destroy(gameObject);
        }

        private void Update()
        {
            Backend.Match.Poll();
            SendQueue.Poll();
        }

        private void OnApplicationQuit()
        {
            SendQueue.StopSendQueue();
        }

        private void Initialize()
        {
            serverBootstrap = GetComponentInChildren<ServerBootstrap>();
            clientBootstrap = GetComponentInChildren<ClientBootstrap>();

            var bro = Backend.Initialize();
            if (bro.IsSuccess())
            {
                CustomLog.Log("초기화 성공 : " + bro);
                SendQueue.StartSendQueue();

                Backend.Match.OnMatchMakingRoomLeave = OnMatchMakingRoomLeaveInternal;
            }
            else
                CustomLog.LogError("초기화 실패 : " + bro);
        }

        private bool IsMatchSuccess(ErrorInfo errInfo) => errInfo.Category == ErrorCode.Success;
        private bool IsMatchSuccess(ErrorCode errCode) => errCode == ErrorCode.Success;

        [ContextMenu("Clear Guest Data")]
        private void DEBUG_ClearGuestData()
        {
            Backend.BMember.DeleteGuestInfo();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            CustomLog.Log("Success clear guest data", Color.green);
        }

        #region Login
        [ContextMenu("Login")]
        public void Login()
        {
            BackendReturnObject bro = Backend.BMember.IsAccessTokenAlive();
            if (bro.IsSuccess())
            {
                SelectHostOrVisitor();
            }
            else
            {
                UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
                loadingView.Show("게스트 로그인 중입니다");
                Backend.BMember.LoginWithTheBackendToken(OnLoginWithTheBackendToken);
            }
        }

        private void OnLoginWithTheBackendToken(BackendReturnObject bro)
        {
            if (bro.IsSuccess())
            {
                UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
                loadingView.Hide();
                CustomLog.Log("로컬에 유효한 게스트 계정 정보가 존재합니다.", Color.green);
                SelectHostOrVisitor();
            }
            else
            {
                Backend.BMember.GuestLogin(OnLogin);
                CustomLog.Log("로컬 게스트 정보 없음");
                UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);
                inputView.Initialize(TMP_InputField.ContentType.Name, 10);
                inputView.Show("사용하실 이름을 입력해주세요!");
                inputView.RegisterInsertEvent(() =>
                {
                    string userName = inputView.GetInput();
                    CreateNickname(userName);
                });
            }
        }

        public void CreateNickname(string newNickname)
        {
            CustomLog.Log($"Try CreateNickname : {newNickname}");
            if (IsValidNickname(newNickname))
            {
                CreateNicknameError(newNickname);
                return;
            }
            Backend.BMember.UpdateNickname(newNickname, OnCreateNickname);
        }

        public bool IsValidNickname(string nickname)
        {
            if (!string.IsNullOrEmpty(nickname))
            {
                nickname = System.Text.RegularExpressions.Regex.Replace(nickname, @"[\x00-\x1F\x7F]|\u200b", "");
                nickname = nickname.Trim();
            }
            if (string.IsNullOrEmpty(nickname) || nickname.Length > 10)
                return true;

            return false;
        }

        private void OnCreateNickname(BackendReturnObject bro)
        {
            if (bro.IsSuccess())
            {
                CustomLog.Log($"닉네임 생성 성공! 현재 닉네임: {Backend.UserNickName}", Color.green);
                UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);
                inputView.Hide();
                SelectHostOrVisitor();
            }
            else
            {
                CustomLog.LogError("닉네임 변경 실패");
            }
        }

        private void CreateNicknameError(string name)
        {
            UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);

            if (string.IsNullOrEmpty(name))
            {
                inputView.Show("빈 닉네임이거나 입력 데이터가 비어있습니다.", Color.red);
                CustomLog.Log("빈 닉네임이거나 입력 데이터가 비어있습니다.", Color.red);
            }
            else if (name.Length > 10)
            {
                inputView.Show("닉네임이 너무 깁니다. (최대 10자 미만)", Color.red);
                CustomLog.Log("닉네임이 너무 깁니다. (최대 10자 미만)", Color.red);
            }
            else
            {
                inputView.Show("이미 다른 유저가 사용중이거나 사용할 수 없는 닉네임입니다.", Color.red);
                CustomLog.Log("이미 다른 유저가 사용중이거나 사용할 수 없는 닉네임입니다.", Color.red);
            }
        }

        private void OnLogin(BackendReturnObject bro)
        {
            UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
            if (bro.IsSuccess())
            {
                CustomLog.Log("게스트 로그인 성공", Color.green);
                loadingView.Hide();
            }
            else
            {
                CustomLog.LogError("게스트 로그인 실패 : " + bro);
                loadingView.Hide();
            }
        }
        #endregion

        #region Create / Join / Delete Room Control
        public void SelectHostOrVisitor()
        {
            CustomLog.Log("SelectHostOrVisitor", Color.red);
            var hub = GameObject.Find("Play").GetComponent<PlayModeUiControlHub>();

            if (hub.IsOpen)
                return;

            hub.InteractPopup();
        }

        private System.Action _onLeaveRoomCallback;

        private void OnMatchMakingRoomLeaveInternal(MatchMakingInteractionEventArgs args)
        {
            _isInMatchRoom = false;
            if (IsMatchSuccess(args.ErrInfo))
            {
                CustomLog.Log("기존 대기방 퇴장(파괴) 완료.", Color.yellow);
                _onLeaveRoomCallback?.Invoke();
                _onLeaveRoomCallback = null;
            }
            else
            {
                CustomLog.LogError("기존 대기방 세션 정리에 실패했습니다: " + args.ErrInfo);
                ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name).Hide();
            }
        }

        private void LeaveAndClearExistingRoom(System.Action nextAction)
        {
            if (!string.IsNullOrEmpty(_myRoomCode))
                DeleteRoomCode();

            if (_isInMatchRoom)
            {
                CustomLog.LogWarning("기존 대기방 세션이 발견되었습니다. 방을 파괴합니다.");
                _onLeaveRoomCallback = nextAction;
                Backend.Match.LeaveMatchRoom();
            }
            else
                nextAction?.Invoke();
        }

        [ContextMenu("CreateRoom")]
        public void CreateRoom()
        {
            UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
            loadingView.Show("대기방 상태를 조회하고 있습니다...");

            LeaveAndClearExistingRoom(() =>
            {
                loadingView.Show("방을 생성중입니다");

                Backend.Match.OnJoinMatchMakingServer = OnJoinMatchMakingServer;
                Backend.Match.OnMatchMakingRoomCreate = OnMatchMakingRoomCreate;

                Backend.Match.JoinMatchMakingServer(out var errorInfo);
            });
        }

        public void OnMatchMakingRoomCreate(MatchMakingInteractionEventArgs args)
        {
            UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
            loadingView.Hide();

            if (!IsMatchSuccess(args.ErrInfo))
            {
                CustomLog.LogError("대기방 생성 실패: " + args.ErrInfo);
                _isInMatchRoom = false;
                return;
            }

            CustomLog.Log("대기방 생성 성공!", Color.green);
            _isInMatchRoom = true; 
            SaveRoomCodeWithCheck(0);
        }

        private void SaveRoomCodeWithCheck(int attemptCount)
        {
            const int maxAttempts = 5;

            if (attemptCount >= maxAttempts)
            {
                CustomLog.LogError($"방코드 생성 실패: {maxAttempts}회 재시도했으나 모두 중복되거나 실패했습니다.");
                return;
            }

            string targetRoomCode = Random.Range(1000, 10000).ToString();
            CustomLog.Log($"방코드 검증 시도 ({attemptCount + 1}회차): {targetRoomCode}");

            Where where = new Where();
            where.Equal(COLUMN_NAME_RoomCode, targetRoomCode);

            SendQueue.Enqueue(Backend.GameData.Get, TABLE_NAME_RoomCodes, where, (bro) =>
            {
                if (!bro.IsSuccess())
                {
                    CustomLog.LogError("방코드 중복 조회 중 서버 에러 발생: " + bro);
                    return;
                }

                JsonData rows = bro.FlattenRows();

                if (rows.Count > 0)
                {
                    CustomLog.LogWarning($"방코드 중복 발견 ({targetRoomCode}). 새로운 코드를 생성합니다.");
                    SaveRoomCodeWithCheck(attemptCount + 1);
                    return;
                }

                InsertUniqueRoomCode(targetRoomCode);
            });
        }

        private void InsertUniqueRoomCode(string uniqueRoomCode)
        {
            string localIP = GetLocalIPAddress();

            Param param = new Param();
            param.Add(COLUMN_NAME_RoomCode, uniqueRoomCode);
            param.Add(COLUMN_NAME_HostIP, localIP);

            SendQueue.Enqueue(Backend.GameData.Insert, TABLE_NAME_RoomCodes, param, (bro) =>
            {
                if (bro.IsSuccess())
                {
                    _myRoomCode = uniqueRoomCode;
                    CustomLog.Log($"최종 방코드 [{_myRoomCode}] (IP: {localIP}) 등록 성공! 플레이어를 기다립니다.", Color.green);
                }
                else
                {
                    CustomLog.LogError("방코드 최종 저장 실패: " + bro);
                }
            });
        }

        public void OnJoinMatchMakingServer(JoinChannelEventArgs args)
        {
            if (!IsMatchSuccess(args.ErrInfo))
            {
                CustomLog.LogError("매칭 서버 접속 실패: " + args.ErrInfo);
                ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name).Hide();
                return;
            }
            CustomLog.Log("매칭 서버 접속 성공", Color.green);
            Backend.Match.CreateMatchRoom();
        }

        [ContextMenu("Join Room")]
        public void JoinRoomByCode()
        {
            UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);
            inputView.Initialize(TMP_InputField.ContentType.IntegerNumber, 4);
            inputView.Show("방 코드를 입력해주세요");

            inputView.RegisterInsertEvent(() =>
            {
                string code = inputView.GetInput();
                JoinRoomByCode(code);
            });
        }

        public void JoinRoomByCode(string roomCode)
        {
            UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
            loadingView.Show("방 정보를 검증하고 기존 세션을 정리 중입니다...");

            LeaveAndClearExistingRoom(() =>
            {
                Where where = new Where();
                where.Equal(COLUMN_NAME_RoomCode, roomCode);

                SendQueue.Enqueue(Backend.GameData.Get, TABLE_NAME_RoomCodes, where, (bro) =>
                {
                    if (!bro.IsSuccess())
                    {
                        CustomLog.LogError("방코드 조회 실패: " + bro);
                        loadingView.Hide();
                        return;
                    }

                    JsonData rows = bro.FlattenRows();
                    if (rows.Count == 0)
                    {
                        CustomLog.LogError("존재하지 않는 방코드입니다.");
                        loadingView.Hide();
                        return;
                    }

                    string hostIP = "0.0.0.0";
                    if (rows[0].ContainsKey(COLUMN_NAME_HostIP) && rows[0][COLUMN_NAME_HostIP] != null)
                    {
                        hostIP = rows[0][COLUMN_NAME_HostIP].ToString();
                        CustomLog.Log($"방코드 조회 성공! 방장 IP 식별 완료: {hostIP}", Color.green);
                    }
                    else
                    {
                        CustomLog.LogWarning("방 코드는 유효하나 HostIP 데이터가 누락되었습니다. 기본 주소(0.0.0.0)를 사용합니다.");
                    }

                    CustomLog.Log("방코드 확인 완료. 매칭 서버 접속 준비 중...");
                    UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);
                    inputView.Hide();

                    Backend.Match.OnJoinMatchMakingServer = (joinArgs) =>
                    {
                        if (!IsMatchSuccess(joinArgs.ErrInfo))
                        {
                            CustomLog.LogError("매칭 서버 접속 실패: " + joinArgs.ErrInfo);
                            loadingView.Hide();
                            return;
                        }
                        CustomLog.Log("매칭 서버 접속 완료. 방 입장을 시도합니다.");

                        int port = 9678;
                        CustomLog.Log($"ClientBootstrap을 구동합니다. 대상 호스트 IP: {hostIP}:{port}", Color.cyan);
                        clientBootstrap.StartClient(hostIP, port);
                    };

                    Backend.Match.OnMatchMakingRoomInviteResponse = (inviteArgs) =>
                    {
                        loadingView.Hide();
                        if (IsMatchSuccess(inviteArgs.ErrInfo))
                        {
                            CustomLog.Log("방 입장 성공!", Color.green);
                            _isInMatchRoom = true; // 입장 성공 시 상태 변경
                        }
                        else
                        {
                            CustomLog.LogError("방 입장 실패: " + inviteArgs.ErrInfo);
                            _isInMatchRoom = false;
                        }
                    };

                    Backend.Match.JoinMatchMakingServer(out var matchErrorInfo);
                });
            });
        }

        public void DeleteRoomCode()
        {
            if (string.IsNullOrEmpty(_myRoomCode)) return;

            Where where = new Where();
            where.Equal(COLUMN_NAME_RoomCode, _myRoomCode);
            _myRoomCode = null;

            SendQueue.Enqueue(Backend.GameData.Delete, TABLE_NAME_RoomCodes, where, (bro) =>
            {
                if (bro.IsSuccess())
                    CustomLog.Log("GameData 테이블에서 방코드 데이터 삭제 완료");
            });
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "0.0.0.0";
            }
            catch (System.Exception ex)
            {
                CustomLog.LogWarning("로컬 IP 주소를 조회하는 중 오류 발생: " + ex.Message);
                return "0.0.0.0";
            }
        }

        private void GetHostIPByRoomCode(int roomCode, System.Action<string> onGetComplete)
        {
            Where where = new Where();
            where.Equal(COLUMN_NAME_RoomCode, roomCode.ToString());

            SendQueue.Enqueue(Backend.GameData.Get, TABLE_NAME_RoomCodes, where, (bro) =>
            {
                if (!bro.IsSuccess())
                {
                    CustomLog.LogError($"방코드 [{roomCode}] IP 조회 실패: {bro}");
                    onGetComplete?.Invoke(null);
                    return;
                }

                JsonData rows = bro.FlattenRows();

                if (rows.Count == 0)
                {
                    CustomLog.LogWarning($"방코드 [{roomCode}]에 해당하는 방이 존재하지 않습니다.");
                    onGetComplete?.Invoke(null);
                    return;
                }

                if (rows[0].ContainsKey(COLUMN_NAME_HostIP) && rows[0][COLUMN_NAME_HostIP] != null)
                {
                    string hostIP = rows[0][COLUMN_NAME_HostIP].ToString();
                    CustomLog.Log($"방코드 [{roomCode}]의 Host IP 조회 성공: {hostIP}", Color.green);

                    onGetComplete?.Invoke(hostIP);
                }
                else
                {
                    CustomLog.LogWarning($"방코드는 존재하나 'hostIP' 컬럼 데이터가 비어있습니다.");
                    onGetComplete?.Invoke(null);
                }
            });
        }
        #endregion
    }
}