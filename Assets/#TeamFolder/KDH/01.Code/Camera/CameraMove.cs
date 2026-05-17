using System;
using System.Collections;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
        [SerializeField] private float moveDuration = 3f;
        [SerializeField] private float stayDuration = 2f;

        // 시작 위치
        private Vector3 _startPosition = new Vector3(-41.62f, 28.56f, 38.56f);
        private Vector3 _startRotation = new Vector3(30.6f, 119.874f, 0f);

        // 4개 위치 순서대로
        private Vector3[] _positions = new Vector3[]
        {
            new Vector3(-42.71f, 27.4f,  -25.98f),  // 1번
            new Vector3( 47.91f, 27.4f,  -25.98f),  // 2번
            new Vector3( 44.67f, 26.55f,  39.7f ),  // 3번
            new Vector3(  0f,    16.4f,   -9.4f ),  // 4번 (목적지)
        };

        private Vector3[] _rotations = new Vector3[]
        {
            new Vector3(28.315f,   42.744f,   0f    ),  // 1번
            new Vector3(25.106f,  -53.611f,  -0.493f),  // 2번
            new Vector3(23.278f, -144.267f,  -0.792f),  // 3번
            new Vector3(54.377f,    0f,       0f    ),  // 4번 (목적지)
        };

        private void Start()
        {
            transform.position = _startPosition;
            transform.eulerAngles = _startRotation;
            StartCoroutine(MoveSequence());
        }

        private IEnumerator MoveSequence()
        {
            // 시작 위치에서 잠깐 머물기
            yield return new WaitForSeconds(stayDuration);

            // 4개 위치 순서대로 이동
            for (int i = 0; i < _positions.Length; i++)
            {
                yield return StartCoroutine(MoveTo(_positions[i], _rotations[i]));
                yield return new WaitForSeconds(stayDuration);
            }

            // 마지막 위치에서 멈춤
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
