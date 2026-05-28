using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.Enum;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class RandomizerMiniGameUiControlHub : MonoBehaviour
    {
        [SerializeField] private MiniGameInfoSO[] miniGameData;

        [SerializeField] private Image miniGameIcon;
        [SerializeField] private TMP_Text miniGameLabel;

        [SerializeField] private int rollCount = 20;
        [SerializeField] private float startInterval = 0.03f;
        [SerializeField] private float endInterval = 0.35f;

        private PlayerBoxUi[] boxes;
        [SerializeField] private Transform point;
        [SerializeField] private PlayerBoxUi prefab;

        private Coroutine _randomizerRoutine;
        private bool _isInit;

        private void Awake()
        {
            AStarEventBus.Subscribe<RandomizerMiniGameInitEvent>(Initialize);
            AStarEventBus.Subscribe<RandomizerMiniGameEvent>(RandomizerMiniGame);
        }

        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<RandomizerMiniGameInitEvent>(Initialize);
            AStarEventBus.Unsubscribe<RandomizerMiniGameEvent>(RandomizerMiniGame);
        }

        private void Initialize(RandomizerMiniGameInitEvent @event)
        {
            if (_isInit) return;
            
            _isInit = true;
            boxes = new PlayerBoxUi[4];
            
            for (int i = 0; i < boxes.Length; i++)
            {
                PlayerBoxUi player = Instantiate(prefab, point);
                player.Initialize(@event.Infos[i].Index, @event.Infos[i].NickName);
                boxes[i] = player;
            }
        }

        private void RandomizerMiniGame(RandomizerMiniGameEvent @event)
        {
            if (!_isInit) return;
            if (_randomizerRoutine != null)
                StopCoroutine(_randomizerRoutine);

            _randomizerRoutine = StartCoroutine(RandomizerRoutine(@event.TargetMiniGameEnum));
        }

        private IEnumerator RandomizerRoutine(MiniGameEnum resultMiniGame)
        {
            for (int i = 0; i < rollCount; i++)
            {
                MiniGameEnum randMiniGame = GetRandomMiniGame();
                SetMiniGameUi(randMiniGame);

                float t = i / (float)(rollCount - 1);
                float interval = Mathf.Lerp(startInterval, endInterval, t * t);

                yield return new WaitForSeconds(interval);
            }

            SetMiniGameUi(resultMiniGame);
            GameManager
            var args = new RandomizerMiniGameEndEvent(resultMiniGame);
            AStarEventBus.Publish<RandomizerMiniGameEndEvent>();
            _randomizerRoutine = null;
        }
        private MiniGameEnum GetRandomMiniGame()
        {
            return (MiniGameEnum)Random.Range(0, (int)MiniGameEnum.MAX);
        }
        private void SetMiniGameUi(MiniGameEnum miniGameEnum)
        {
            int index = (int)miniGameEnum;

            miniGameLabel.text = miniGameData[index].MiniGameName;
            miniGameIcon.sprite = miniGameData[index].MiniGameIcon;
        }
    }
}
