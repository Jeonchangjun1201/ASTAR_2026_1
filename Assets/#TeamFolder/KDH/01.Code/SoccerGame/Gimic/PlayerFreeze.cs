using System.Collections;
using _TeamFolder.JCJ.Script;
using UnityEngine;

namespace KDH.Gimic
{
    public class PlayerFreeze : MonoBehaviour
    {
        private MonoBehaviour _playerController;
        private Rigidbody _rigid;
        private bool _isfreezen = false;

        private void Awake()
        {
            _rigid = GetComponent<Rigidbody>();
            _playerController = GetComponent<PlayerController>();
        }

        public void StartFreeze(float duration)
        {
            StartCoroutine(FreezeCoroutine(duration));
        }

        private IEnumerator FreezeCoroutine(float duration)
        {
            _isfreezen = true;

            if (_playerController != null)
            {
                _playerController.enabled = false;
            }
            _rigid.linearVelocity = Vector3.zero;
            _rigid.angularVelocity = Vector3.zero;
            _rigid.constraints = RigidbodyConstraints.FreezeAll;

            Debug.Log($"{gameObject.name}모두ㅜㅜㅜㅜㅜㅜㅜ 얼음ㅁㅁㅁㅁㅁㅁ");

            yield return new WaitForSeconds(duration);

            _rigid.constraints = RigidbodyConstraints.None;
            if (_playerController != null)
                _playerController.enabled = true;

            _isfreezen = false;
            
        }
    }
}