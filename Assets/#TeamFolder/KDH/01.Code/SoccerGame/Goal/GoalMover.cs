using System;
using UnityEngine;

namespace KDH
{
    public class GoalMover : MonoBehaviour
    {
        [SerializeField] private float moveDistance = 3f;
        [SerializeField] private float moveSpeeed = 2f;
        
        private Vector3 _startpos;

        private void Start()
        {
            _startpos = transform.position;
        }

        private void Update()
        {
            float offset = Mathf.Sin(Time.time * moveSpeeed) * moveDistance;
            transform.position = _startpos + transform.right * offset;
        }
    }
}