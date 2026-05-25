using System.Collections;
using _TeamFolder.JCJ.Script;
using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace KDH.Gimic
{
    public class PlayerFreeze : MonoBehaviour
    {
        private MonoBehaviour _playerController;
        private Rigidbody _rigid;
        private bool _isfreezen = false;
        private RigidbodyConstraints _originalConstraints; // 원래 제약 저장

        private void Awake()
        {
            _rigid = GetComponent<Rigidbody>();
            _playerController = GetComponent<PlayerController>();
            _originalConstraints = _rigid.constraints; // 원래 제약 저장
        }

        public void StartFreeze(float duration)
        {
            if (_isfreezen) return;
            StartCoroutine(FreezeCoroutine(duration));
        }

        private IEnumerator FreezeCoroutine(float duration)
        {
            _isfreezen = true;

            if (_playerController != null)
                _playerController.enabled = false;

            _rigid.linearVelocity = Vector3.zero;
            _rigid.angularVelocity = Vector3.zero;
            _rigid.constraints = RigidbodyConstraints.FreezeAll;

            yield return new WaitForSeconds(duration);

            // None 대신 원래 제약으로 복구!
            _rigid.constraints = _originalConstraints;
            if (_playerController != null)
                _playerController.enabled = true;

            SoundManager.Instance.PlaySound("PlayerIce");
            _isfreezen = false;
        }
    }
}