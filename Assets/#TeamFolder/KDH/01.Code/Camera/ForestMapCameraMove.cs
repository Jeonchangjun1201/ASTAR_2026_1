using System.Collections;
using UnityEngine;

namespace _TeamFolder.KDH._01.Code.Camera
{
    public class ForestMapCameraMove : MonoBehaviour
    {
        [SerializeField] private float moveDuration = 3f;
        [SerializeField] private float stayDuration = 2f;

        private Vector3[] _positions = new Vector3[]
        {
            new Vector3(-6.25f,  9.59f, -20.41f),  // 1번
            new Vector3(8.05000019f,9.59000015f,-20.4099998f),
            new Vector3(16.41f,  7.51f,   4.93f),  // 3번
            new Vector3(-10.87f, 5.97f,   7.26f),  // 4번
            new Vector3(-0.15f,5.6f,-3.160f)
        };

        private Vector3[] _rotations = new Vector3[]
        {
            new Vector3(23.08f,    0f,    0f),  // 1번
            new Vector3(3.22490525f,341.172791f,0f),
            new Vector3(23f, -110f,    0f),  // 3번
            new Vector3(21.255f, 119.886f, 0f), // 4번
            new Vector3(60.0f,0f,0f)
        };

        private void Start()
        {
            transform.position = _positions[0];
            transform.eulerAngles = _rotations[0];
            StartCoroutine(MoveSequence());
        }

        private IEnumerator MoveSequence()
        {
            yield return new WaitForSeconds(stayDuration);

            for (int i = 1; i < _positions.Length; i++)
            {
                yield return StartCoroutine(MoveTo(_positions[i], _rotations[i]));
                yield return new WaitForSeconds(stayDuration);
            }

            Debug.Log("카메라 이동 완료!");
        }

        private IEnumerator MoveTo(Vector3 targetPos, Vector3 targetRot)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion endRot = Quaternion.Euler(targetRot);

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float smooth = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                transform.position = Vector3.Lerp(startPos, targetPos, smooth);
                transform.rotation = Quaternion.Slerp(startRot, endRot, smooth);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = endRot;
        }
    }
}
