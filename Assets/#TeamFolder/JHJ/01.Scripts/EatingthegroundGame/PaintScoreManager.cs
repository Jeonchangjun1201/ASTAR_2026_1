using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public class PaintScoreManager : MonoBehaviour
    {
        [Header("계산 설정")]
        [SerializeField] private RenderTexture _paintCanvas; // 우리가 색칠할 메인 캔바스
        [SerializeField] private float _checkInterval = 1f;  // 검사 주기 (1초)

        [Header("플레이어 색상 (4개 세팅)")]
        [SerializeField] private Color[] _playerColors = new Color[4];

        [SerializeField] private float _percentage;
        private Texture2D _tempTexture;

        private void Start()
        {
            // 1. 텍스쳐 크기랑 같은 종이? 도화지? 만들기
            _tempTexture = new Texture2D(_paintCanvas.width, _paintCanvas.height, TextureFormat.RGBA32, false);

            //일정 타이머 마다 퍼센테이지를 계산
            InvokeRepeating(nameof(CalculatePaintPercentage), 1f, _checkInterval);
        }

        private void CalculatePaintPercentage()
        {
            if (_paintCanvas == null) return;

            //RenderTexture.active를 키며 작업할 캔버스를 지정
            RenderTexture.active = _paintCanvas;

            //_tempTexture.ReadPixels(new Rect(0, 0, _paintCanvas.width, _paintCanvas.height), 0, 0);로 
            //_tempTexture에 x,y좌표를 기준으로 도화지 저장(아무것도 없는 빈)
            _tempTexture.ReadPixels(new Rect(0, 0, _paintCanvas.width, _paintCanvas.height), 0, 0);

            // 지금까지 작업( _tempTexture.ReadPixels(new Rect(0, 0, _paintCanvas.width, _paintCanvas.height), 0, 0);) 을 저장
            _tempTexture.Apply();

            //RenderTexture.active를 끔
            RenderTexture.active = null;



            // 이미지의 모든 픽셀 색상 정보를 1줄짜리 긴 배열(목록)로 뽑아주는 함수.
            Color[] pixels = _tempTexture.GetPixels();

            int totalPixels = pixels.Length;
            int[] playerScores = new int[4]; // 4명 점수 저장

            //위에서 뽑은 색상 정보를 전부 검사
            for (int i = 0; i < totalPixels; i++)
            {
                Color pColor = pixels[i];

                //만약 도화지가 하얀색(아무것도 안 칠해져 있으면) 넘기기
                if (pColor.r > 0.95f && pColor.g > 0.95f && pColor.b > 0.95f) continue;

                bool isMatched = false; 

                for (int p = 0; p < _playerColors.Length; p++)
                {
                    
                    if (Mathf.Abs(pColor.r - _playerColors[p].r) < 1f &&
                        Mathf.Abs(pColor.g - _playerColors[p].g) < 1f &&
                        Mathf.Abs(pColor.b - _playerColors[p].b) < 1f)
                    {
                        playerScores[p]++;
                        isMatched = true;
                        break;
                    }
                }
            }

            // 결과 출력
            Debug.Log("=== 현재 땅따먹기 현황 ===");
            for (int p = 0; p < _playerColors.Length; p++)
            {
                _percentage = ((float)playerScores[p] / totalPixels) * 100f;
                Debug.Log($"Player {p + 1} 점수: {_percentage:F2}%");
            }
        }
        public Color GetPlayerColor(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex < _playerColors.Length)
            {
                return _playerColors[playerIndex];
            }
            return Color.white; 
        }

    }
}
