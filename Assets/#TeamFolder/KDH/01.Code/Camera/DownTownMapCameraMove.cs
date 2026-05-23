using System.Collections;
using UnityEngine;

public class DownTownMapCameraMove : MonoBehaviour
{
     [SerializeField] private float moveDuration = 3f;
        [SerializeField] private float stayDuration = 2f;

        private Vector3[] _positions = new Vector3[]
        {
            new Vector3(-11.84f,  5.03f, 15.89f),  // 1번 시작
            new Vector3(-17.58f,  5.69f,  2.1f),   // 2번
            new Vector3(  7.47f,  5.51f, -4.97f),  // 3번
            new Vector3( -3.897f, 3.823f,  2.03f), // 4번 최종
        };

        private Vector3[] _rotations = new Vector3[]
        {
            new Vector3(16.875f, 138.119f, 0f), // 1번 시작
            new Vector3(22.018f,  90f,     0f), // 2번
            new Vector3(22.921f, -46.594f, 0f), // 3번
            new Vector3(52.839f,  90f,     0f), // 4번 최종
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
