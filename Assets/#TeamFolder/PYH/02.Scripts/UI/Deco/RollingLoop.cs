using UnityEngine;

namespace _TeamFolder.PYH._02.Scripts.UI.Deco
{
    public class RollingLoop : MonoBehaviour
    {
        [SerializeField] private RectTransform[] backgrounds;
    
        [SerializeField] private Vector2 endPoint;
        [SerializeField] private float speed = 1f;
    
        [SerializeField] private bool moveX, moveY;
    
        private void FixedUpdate()
        {
            Vector2 move = Vector2.zero;
    
            if (moveX)
                move.x = Mathf.Sign(endPoint.x) * speed * Time.fixedDeltaTime;
    
            if (moveY)
                move.y = Mathf.Sign(endPoint.y) * speed * Time.fixedDeltaTime;
    
            foreach (RectTransform t in backgrounds)
                t.anchoredPosition += move;
    
            LoopCheck();
        }
    
        private void LoopCheck()
        {
            if (moveX)
                CheckX();
    
            if (moveY)
                CheckY();
        }
    
        private void CheckX()
        {
            bool moveRight = endPoint.x > 0f;
    
            for (int i = 0; i < backgrounds.Length; i++)
            {
                RectTransform bg = backgrounds[i];
    
                if (moveRight)
                {
                    if (GetLeft(bg) >= endPoint.x)
                    {
                        float minLeft = GetMinLeft();
                        SetRight(bg, minLeft);
                    }
                }
                else
                {
                    if (GetRight(bg) <= endPoint.x)
                    {
                        float maxRight = GetMaxRight();
                        SetLeft(bg, maxRight);
                    }
                }
            }
        }
        private void CheckY()
        {
            bool moveUp = endPoint.y > 0f;
    
            for (int i = 0; i < backgrounds.Length; i++)
            {
                RectTransform bg = backgrounds[i];
    
                if (moveUp)
                {
                    if (GetBottom(bg) >= endPoint.y)
                    {
                        float minBottom = GetMinBottom();
                        SetTop(bg, minBottom);
                    }
                }
                else
                {
                    if (GetTop(bg) <= endPoint.y)
                    {
                        float maxTop = GetMaxTop();
                        SetBottom(bg, maxTop);
                    }
                }
            }
        }
    
        private float GetLeft(RectTransform rect)
        {
            return rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
        }
        private float GetRight(RectTransform rect)
        {
            return rect.anchoredPosition.x + rect.rect.width * (1f - rect.pivot.x);
        }
        private float GetBottom(RectTransform rect)
        {
            return rect.anchoredPosition.y - rect.rect.height * rect.pivot.y;
        }
        private float GetTop(RectTransform rect)
        {
            return rect.anchoredPosition.y + rect.rect.height * (1f - rect.pivot.y);
        }
    
        private void SetLeft(RectTransform rect, float left)
        {
            rect.anchoredPosition = new Vector2(
                left + rect.rect.width * rect.pivot.x,
                rect.anchoredPosition.y
            );
        }
        private void SetRight(RectTransform rect, float right)
        {
            rect.anchoredPosition = new Vector2(
                right - rect.rect.width * (1f - rect.pivot.x),
                rect.anchoredPosition.y
            );
        }
        private void SetBottom(RectTransform rect, float bottom)
        {
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                bottom + rect.rect.height * rect.pivot.y
            );
        }
        private void SetTop(RectTransform rect, float top)
        {
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                top - rect.rect.height * (1f - rect.pivot.y)
            );
        }
    
        private float GetMinLeft()
        {
            float value = GetLeft(backgrounds[0]);
    
            for (int i = 1; i < backgrounds.Length; i++)
                value = Mathf.Min(value, GetLeft(backgrounds[i]));
    
            return value;
        }
        private float GetMaxRight()
        {
            float value = GetRight(backgrounds[0]);
    
            for (int i = 1; i < backgrounds.Length; i++)
                value = Mathf.Max(value, GetRight(backgrounds[i]));
    
            return value;
        }
        private float GetMinBottom()
        {
            float value = GetBottom(backgrounds[0]);
    
            for (int i = 1; i < backgrounds.Length; i++)
                value = Mathf.Min(value, GetBottom(backgrounds[i]));
    
            return value;
        }
        private float GetMaxTop()
        {
            float value = GetTop(backgrounds[0]);
    
            for (int i = 1; i < backgrounds.Length; i++)
                value = Mathf.Max(value, GetTop(backgrounds[i]));
    
            return value;
        }
    }
}
