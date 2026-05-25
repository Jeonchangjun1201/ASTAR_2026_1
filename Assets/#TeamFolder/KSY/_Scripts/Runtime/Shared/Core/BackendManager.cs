using UnityEngine;
using BackEnd;
using KSY.Utility;
namespace KSY.Shared
{
    public class BackendManager : MonoBehaviour
    {
        public static BackendManager Instance { get; private set; }

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

        private void Initialize()
        {
            var bro = Backend.Initialize();

            if (bro.IsSuccess())
                CustomLog.Log("초기화 성공 : " + bro);
            else
                CustomLog.LogError("초기화 실패 : " + bro);
        }

        [ContextMenu("Login")]
        public void Login()
        {
            BackendReturnObject bro = Backend.BMember.GuestLogin("게스트 로그인으로 로그인함");
            if (bro.IsSuccess())
            {
                CustomLog.Log("게스트 로그인에 성공했습니다");
            }
        }
    }
}
