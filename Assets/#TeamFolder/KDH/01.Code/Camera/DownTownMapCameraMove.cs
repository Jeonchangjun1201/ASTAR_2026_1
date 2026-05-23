using System;
using System.Collections;
using UnityEngine;

public class DownTownMapCameraMove : MonoBehaviour
{
    [SerializeField] private float moveDuration = 3f;
        [SerializeField] private float stayDuration = 2f;

        public static event Action OnSequenceFinished;

        // 1번 맵 카메라
        private Vector3[] _map1Positions = new Vector3[]
        {
            new Vector3(-11.84f,  5.03f, 15.89f),
            new Vector3(-17.58f,  5.69f,  2.1f),
            new Vector3(  7.47f,  5.51f, -4.97f),
            new Vector3( -3.897f, 3.823f,  2.03f),
        };

        private Vector3[] _map1Rotations = new Vector3[]
        {
            new Vector3(16.875f, 138.119f, 0f),
            new Vector3(22.018f,  90f,     0f),
            new Vector3(22.921f, -46.594f, 0f),
            new Vector3(52.839f,  90f,     0f),
        };

        // 2번 맵 카메라
        private Vector3[] _map2Positions = new Vector3[]
        {
            new Vector3(-11.1f,   2.76f,  17.48f),
            new Vector3(-13.43f,  5.25f,   3.29f),
            new Vector3( 15.6f,   5.642f, -8.82f),
            new Vector3( -6.13f,  4.55f,   1.69f),
        };

        private Vector3[] _map2Rotations = new Vector3[]
        {
            new Vector3(16.875f, 138.119f, 0f),
            new Vector3(20.549f, 103.879f, 3.017f),
            new Vector3(19.957f, -60.87f,  0f),
            new Vector3(33.706f,  90f,     0f),
        };

        private Vector3[] _positions;
        private Vector3[] _rotations;

        private void Start()
        {
            StartSequence(0);
        }

        public void StartSequence(int mapIndex = 0)
        {
            StopAllCoroutines();

            _positions = mapIndex == 0 ? _map1Positions : _map2Positions;
            _rotations = mapIndex == 0 ? _map1Rotations : _map2Rotations;

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
            OnSequenceFinished?.Invoke();
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
