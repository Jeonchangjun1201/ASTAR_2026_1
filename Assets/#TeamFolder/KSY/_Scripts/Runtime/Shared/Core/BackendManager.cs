using BackEnd;
using BackEnd.Tcp;
using KSY.Shared.UI;
using KSY.Utility;
using LitJson;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace KSY.Shared
{
    public class BackendManager : MonoBehaviour
    {
        public static BackendManager Instance { get; private set; }

        private const string TABLE_NAME = "RoomCodes";
        private string _myRoomCode;

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
        }

        private void Initialize()
        {
            var bro = Backend.Initialize();
            if (bro.IsSuccess())
                CustomLog.Log("초기화 성공 : " + bro);
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

        [ContextMenu("Login")]
        public void Login()
        {
            UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
            loadingView.Show("게스트 로그인 중입니다");
            Backend.BMember.LoginWithTheBackendToken(OnLoginWithTheBackendToken);
        }

        private void OnLoginWithTheBackendToken(BackendReturnObject bro)
        {
            if (bro.IsSuccess())
            {
                CustomLog.Log("로컬에 유효한 게스트 계정 정보가 존재합니다.", Color.green);
                SceneManager.LoadScene("KSY_HostOrVisitor");
            }
            else
            {
                Backend.BMember.GuestLogin(OnLogin);    
                CustomLog.Log("로컬 게스트 정보 없음");
                UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);
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
            {
                return true;
            }

            return false;
        }

        private void OnCreateNickname(BackendReturnObject bro)
        {
            if (bro.IsSuccess())
            {
                CustomLog.Log($"닉네임 생성 성공! 현재 닉네임: {Backend.UserNickName}", Color.green);
                UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);
                inputView.Hide();
                SceneManager.LoadScene("KSY_HostOrVisitor");
            }
            else
            {
                CustomLog.LogError("닉네임 변경 실패");
            }
        }

        private void CreateNicknameError(string name)
        {
            UIInputView inputView = ViewManager.Instance.GetUI<UIInputView>(typeof(UIInputView).Name);

            if(string.IsNullOrEmpty(name))
            {
                inputView.Show("빈 닉네임이거나 입력 데이터가 비어있습니다.", Color.red);
                CustomLog.Log("빈 닉네임이거나 입력 데이터가 비어있습니다.", Color.red);
            }
            else if(name.Length > 10)
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

        [ContextMenu("CreateRoom")]
        public void CreateRoom()
        {
            UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
            loadingView.Show("방을 생성중입니다");

            Backend.Match.OnJoinMatchMakingServer = OnJoinMatchMakingServer;
            Backend.Match.OnMatchMakingRoomCreate = OnMatchMakingRoomCreate;
            Backend.Match.JoinMatchMakingServer(out var errorInfo);
        }

        public void OnMatchMakingRoomCreate(MatchMakingInteractionEventArgs args)
        {
            if (!IsMatchSuccess(args.ErrInfo))
            {
                CustomLog.LogError("대기방 생성 실패: " + args.ErrInfo);

                UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
                loadingView.Hide();
                return;
            }
            _myRoomCode = Random.Range(1000, 10000).ToString();
            CustomLog.Log($"대기방 생성 성공! 방코드 : {_myRoomCode}", Color.green);
            SaveRoomCode(_myRoomCode);
        }

        public void OnJoinMatchMakingServer(JoinChannelEventArgs args)
        {
            if (!IsMatchSuccess(args.ErrInfo))
            {
                CustomLog.LogError("매칭 서버 접속 실패: " + args.ErrInfo);
                
                UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
                loadingView.Hide();

                return;

            }
            CustomLog.Log("매칭 서버 접속 성공", Color.green);
            Backend.Match.CreateMatchRoom();
        }

        private void SaveRoomCode(string roomCode)
        {
            Param param = new Param();
            param.Add("roomCode", roomCode);

            SendQueue.Enqueue(Backend.GameData.Insert, TABLE_NAME, param, (bro) =>
            {
                if (bro.IsSuccess())
                    CustomLog.Log($"방코드 {roomCode} 저장 성공", Color.green);
                else
                    CustomLog.LogError("방코드 저장 실패: " + bro);
            });
        }

        public void JoinRoomByCode(string roomCode)
        {
            Where where = new Where();
            where.Equal("roomCode", roomCode);

            SendQueue.Enqueue(Backend.GameData.Get, TABLE_NAME, where, (bro) =>
            {
                if (!bro.IsSuccess())
                {
                    CustomLog.LogError("방코드 조회 실패: " + bro);
                    return;
                }

                JsonData rows = bro.FlattenRows();
                if (rows.Count == 0)
                {
                    CustomLog.LogError("존재하지 않는 방코드입니다.");
                    return;
                }

                CustomLog.Log("방코드 확인 완료. 매칭 서버 접속 중...");

                Backend.Match.JoinMatchMakingServer(out var matchErrorInfo);

                Backend.Match.OnJoinMatchMakingServer = (args) =>
                {
                    if (!IsMatchSuccess(args.ErrInfo))
                    {
                        CustomLog.LogError("매칭 서버 접속 실패: " + args.ErrInfo);
                        return;
                    }
                    CustomLog.Log("매칭 서버 접속 완료. 방장의 초대를 기다리는 중...");
                };

                Backend.Match.OnMatchMakingRoomSomeoneInvited = (args) =>
                {
                    if (!IsMatchSuccess(args.ErrInfo)) return;

                    CustomLog.Log("초대 수신! 자동 수락 중...");
                    Backend.Match.AcceptInvitation(args.RoomId, args.RoomToken);
                };

                Backend.Match.OnMatchMakingRoomInviteResponse = (args) =>
                {
                    if (IsMatchSuccess(args.ErrInfo))
                        CustomLog.Log("방 입장 성공!");
                    else
                        CustomLog.LogError("방 입장 실패: " + args.ErrInfo);
                };
            });
        }

        public void DeleteRoomCode()
        {
            if (string.IsNullOrEmpty(_myRoomCode)) return;

            Where where = new Where();
            where.Equal("roomCode", _myRoomCode);

            SendQueue.Enqueue(Backend.GameData.Delete, TABLE_NAME, where, (bro) =>
            {
                if (bro.IsSuccess())
                    CustomLog.Log("방코드 삭제 완료");
            });
        }
    }
}