using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace KDH
{
    public class RopeRotator : MonoBehaviour
    {
        [Header("설정")]
        [SerializeField] private float rotateSpeed = 180f;
        [SerializeField] private float speedIncrement = 10f;
        [SerializeField] private float maxSpeed = 720f;

        public float CurrentSpeed => rotateSpeed;

        private float _totalRotation = 0f;

        private void Update()
        {
            float rotationThisFrame = rotateSpeed * Time.deltaTime;

            transform.Rotate(Vector3.forward, rotationThisFrame);

            _totalRotation += rotationThisFrame;
            if (_totalRotation >= 360f)
            {
                _totalRotation = 0f;
                SoundManager.Instance.PlaySound("LopeJump");
            }

            rotateSpeed += speedIncrement * Time.deltaTime;
            rotateSpeed = Mathf.Min(rotateSpeed, maxSpeed);
        }
    }
}