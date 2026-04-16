using UnityEngine;

namespace JHJ.Scripts.JingeomDariGame.GiMick
{
    [RequireComponent(typeof(Rigidbody))]
    public class FallingBridge : MonoBehaviour
    {
        [SerializeField] private float _fallDelay = 0.2f;   
        [SerializeField] private float _requiredStayTime = 0.5f; 

        private float _currentStayTime = 0f; 
        private Rigidbody _rb;
        private bool _isFalling = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && !_isFalling)
            {
                _currentStayTime += Time.deltaTime;

                if (_currentStayTime >= _requiredStayTime)
                {
                    Debug.Log("떨어진다");
                    _isFalling = true;
                    Invoke(nameof(StartFalling), _fallDelay);
                }
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && !_isFalling)
            {
                _currentStayTime = 0f;
                Debug.Log("플레이어 토낌");
            }
        }

        private void StartFalling()
        {
            Debug.Log("다리 떨어짐");
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.AddForce(Vector3.down, ForceMode.VelocityChange);
            Destroy(gameObject, 5f);
        }
    }
}