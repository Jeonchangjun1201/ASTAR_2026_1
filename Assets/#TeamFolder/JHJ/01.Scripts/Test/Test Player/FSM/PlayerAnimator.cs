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
            _animator.SetBool(_stateMachine.isRunning, false);
            _animator.SetBool(_stateMachine.isFalling, false);

            switch (stateName)
            {
                case "Run":
                    _animator.SetBool(_stateMachine.isRunning, true);
                    break;

                case "Jump":
                    _animator.SetTrigger(_stateMachine.isJump);
                    break;

                case "Fall":
                    _animator.SetBool(_stateMachine.isFalling, true);
                    break;
            }
        }
    }

}
