using System.Collections;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace MiniGame.HumanGolf
{
    public class MinimalGround : MonoBehaviour
    {
        private int _pointTick = 0;
        private float _currentTick = 0;

        private bool _isInit;
        
        public void SetStartTick(int tick)
        {
            if (_isInit) return;

            _isInit = true;
            
            _pointTick = tick;
        }

        public void StartResize()
        {
            StartCoroutine(Resize());
        }
        public void StopResize()
        {
            StopAllCoroutines();
        }
        
        public IEnumerator Resize()
        {
            _currentTick = _pointTick * 2;

            while (_currentTick > 0)
            {
                _currentTick -= 0.01f;
                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(0, 0, 0), 0.001f);
                yield return new WaitForSeconds(0.01f);
            }
            
            transform.localScale = Vector3.zero;
        }
    }
}
