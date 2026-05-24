using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _TeamFolder.PYH._02.Scripts.UI.Scene
{
    public class CharacterUiMover : MonoBehaviour
    {
        [SerializeField] private RectTransform[] characters;
        [SerializeField] private float[] gaps;
        [SerializeField] private float[] durations;

        private Tween[] _tweens;

        private void Awake()
        {
            Animation();
        }

        private void Update()
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                Animation();
            }
            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                OnDestroy();
            }
        }
        
        private void Animation()
        {
            int count = Mathf.Min(characters.Length, gaps.Length, durations.Length);
            _tweens = new Tween[count];

            for (int i = 0; i < count; i++)
            {
                RectTransform character = characters[i];
                if (character == null) continue;

                float originY = character.anchoredPosition.y;
                float targetY = originY + gaps[i];

                _tweens[i] = character
                    .DOAnchorPosY(targetY, durations[i])
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void OnDestroy()
        {
            if (_tweens == null) return;

            foreach (Tween tween in _tweens)
            {
                tween?.Kill();
            }
        }
    }
}