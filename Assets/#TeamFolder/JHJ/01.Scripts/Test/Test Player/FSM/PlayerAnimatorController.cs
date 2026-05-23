using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer.FSM
{
    public class PlayerAnimatorController : MonoBehaviour
    {
        private PlayerStateMachine _stateMachine;
        private Animator _animator;

        private string _currentStateName = "";

        private void Awake()
        {
            _stateMachine = GetComponentInParent<PlayerStateMachine>();
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            RefreshAnimatorParams();
        }

        private void RefreshAnimatorParams()
        {
            string targetState = GetCurrentStateName();
            if (targetState == _currentStateName) return;

            _currentStateName = targetState;
            ApplyAnimatorState(targetState);
        }

        private string GetCurrentStateName()
        {
            if (_stateMachine.CurrentState == _stateMachine.IdleState) return "Idle";
            if (_stateMachine.CurrentState == _stateMachine.RunState) return "Run";
            if (_stateMachine.CurrentState == _stateMachine.JumpState) return "Jump";
            if (_stateMachine.CurrentState == _stateMachine.FallState) return "Fall";
            return "";
        }

        private void ApplyAnimatorState(string stateName)
        {
            // 🌟 [수정] Any State 트랜지션이 꼬이지 않도록 모든 트리거 초기화 (필수)
            _animator.ResetTrigger(_stateMachine.idle);
            _animator.ResetTrigger(_stateMachine.isRunning);
            _animator.ResetTrigger(_stateMachine.isJump);
            _animator.ResetTrigger(_stateMachine.isFalling);

            // 🌟 [수정] 사진의 파라미터(동그라미) 타입에 맞춰 모두 SetTrigger로 변경
            switch (stateName)
            {
                case "Idle":
                    _animator.SetTrigger(_stateMachine.idle);
                    break;
                case "Run":
                    _animator.SetTrigger(_stateMachine.isRunning);
                    break;
                case "Jump":
                    _animator.SetTrigger(_stateMachine.isJump);
                    break;
                case "Fall":
                    _animator.SetTrigger(_stateMachine.isFalling);
                    break;
            }
        }
    }
}