using System.Collections.Generic;
using System.Linq;
using _TeamFolder.PYH._02.Scripts.Data;
using _TeamFolder.PYH._02.Scripts.UI.Event;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class ResultUiControlHub : MonoBehaviour
    {
        [SerializeField] private ResultBox resultBoxPrefab;
        [SerializeField] private Transform point;

        [SerializeField] private ResultBox[] boxes;
        [SerializeField] private GridLayoutGroup grid;

        [SerializeField] private float reorderDuration = 0.35f;
        [SerializeField] private Ease reorderEase = Ease.OutCubic;

        private readonly Dictionary<string, ResultBox> _boxMap = new();
        private Sequence _reorderSeq;

        private void Awake()
        {
            AStarEventBus.Subscribe<ResultUiInitEvent>(Initialize);
            AStarEventBus.Subscribe<ResultUiReorderEvent>(Reorder);
        }
        private void OnDestroy()
        {
            AStarEventBus.Unsubscribe<ResultUiInitEvent>(Initialize);
            AStarEventBus.Unsubscribe<ResultUiReorderEvent>(Reorder);

            _reorderSeq?.Kill();
        }

        private void Initialize(ResultUiInitEvent @event)
        {
            _reorderSeq?.Kill();
            _boxMap.Clear();

            boxes = new ResultBox[4];

            for (int i = 0; i < 4; i++)
            {
                PlayerInfo info = @event.PlayerResultInfos[i];

                ResultBox obj = Instantiate(resultBoxPrefab, point);
                obj.Initialize(info.Index, info.NickName, info.Point, info.Ranking);

                boxes[i] = obj;
                _boxMap.Add(info.NickName, obj);
            }
        }
        private void Reorder(ResultUiReorderEvent @event)
        {
            _reorderSeq?.Kill();

            PlayerInfo[] curInfos = @event.CurInfos
                .OrderBy(info => info.Ranking)
                .ToArray();

            Dictionary<ResultBox, Vector2> startPositions = new();

            foreach (ResultBox box in boxes)
            {
                startPositions[box] = box.Rect.anchoredPosition;
            }

            grid.enabled = true;

            ApplyOrder(curInfos, false);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grid.transform);

            Dictionary<ResultBox, Vector2> targetPositions = new();

            foreach (ResultBox box in boxes)
            {
                targetPositions[box] = box.Rect.anchoredPosition;
                box.Rect.anchoredPosition = startPositions[box];
            }

            grid.enabled = false;

            _reorderSeq = DOTween.Sequence();

            foreach (ResultBox box in boxes)
            {
                _reorderSeq.Join(
                    box.Rect
                        .DOAnchorPos(targetPositions[box], reorderDuration)
                        .SetEase(reorderEase)
                );
            }

            _reorderSeq.OnComplete(() =>
            {
                ApplyOrder(curInfos, true);
                grid.enabled = true;

                boxes = curInfos
                    .Select(info => _boxMap[info.NickName])
                    .ToArray();
            });
        }
        private void ApplyOrder(PlayerInfo[] infos, bool updateText)
        {
            for (int i = 0; i < infos.Length; i++)
            {
                PlayerInfo info = infos[i];

                if (!_boxMap.TryGetValue(info.NickName, out ResultBox box))
                    continue;

                box.transform.SetSiblingIndex(i);

                if (updateText)
                {
                    box.Initialize(info.Index, info.NickName, info.Point, info.Ranking);
                }
            }
        }
    }
}