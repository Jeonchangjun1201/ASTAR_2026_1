using UnityEngine;
using BackEnd;
using KSY.Utility;
using UnityEngine.SceneManagement;
using Unity.Loading;
using KSY.Shared.UI;
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
            Backend.BMember.GuestLogin(OnLogin);
        }

        private void OnLogin(BackendReturnObject bro)
        {
            UILoadingView loadingView = ViewManager.Instance.GetUI<UILoadingView>(typeof(UILoadingView).Name);
            if (bro.IsSuccess())
            {
                loadingView.Hide();
                SceneManager.LoadScene("KSY_HostOrVisitor");
            }
            else
            {
                CustomLog.LogError("게스트 로그인 실패 : " + bro);
                loadingView.Hide();
            }
        }
    }
}
