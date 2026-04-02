using UnityEngine;

namespace JHJ.Scripts.JingeomDariGame.GiMick
{
    [RequireComponent(typeof(Rigidbody))]
    public class FallingBridge : MonoBehaviour
    {
        [SerializeField] private float fallDelay = 0.2f;   
        [SerializeField] private float requiredStayTime = 0.5f; 

        private float currentStayTime = 0f; 
        private Rigidbody rb;
        private bool isFalling = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && !isFalling)
            {
                currentStayTime += Time.deltaTime;

                if (currentStayTime >= requiredStayTime)
                {
                    Debug.Log("떨어진다");
                    isFalling = true;
                    Invoke(nameof(StartFalling), fallDelay);
                }
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && !isFalling)
            {
                currentStayTime = 0f;
                Debug.Log("플레이어 토낌");
            }
        }

        private void StartFalling()
        {
            Debug.Log("다리 떨어짐");
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(Vector3.down, ForceMode.VelocityChange);
            Destroy(gameObject, 5f);
        }
    }
}