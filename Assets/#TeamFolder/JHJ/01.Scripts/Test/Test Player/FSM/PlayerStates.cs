/*using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer.FSM
{
    // 1. 대기 상태
    public class PlayerIdleState : PlayerBaseState
    {
        public PlayerIdleState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState()
        {
            _ctx.Animator.SetBool(_ctx.AnimParamIsRunning, false);
            _ctx.Animator.SetBool(_ctx.AnimParamIsFalling, false);
        }

        public override void UpdateState()
        {
            if (_ctx.CurrentMovementInput.sqrMagnitude > 0.01f)
            {
                _ctx.ChangeState(_ctx.RunState);
            }
            else if (!_ctx.IsGrounded())
            {
                _ctx.ChangeState(_ctx.FallState);
            }
        }

        public override void FixedUpdateState() { *//* 대기 중엔 특별한 물리 처리 없음 *//* }
        public override void ExitState() { }
    }

    // 2. 달리기 상태
    public class PlayerRunState : PlayerBaseState
    {
        public PlayerRunState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState()
        {
            _ctx.Animator.SetBool(_ctx.AnimParamIsRunning, true);
        }

        public override void UpdateState()
        {
            if (_ctx.CurrentMovementInput.sqrMagnitude <= 0.01f)
            {
                _ctx.ChangeState(_ctx.IdleState);
            }
            else if (!_ctx.IsGrounded())
            {
                _ctx.ChangeState(_ctx.FallState);
            }
        }

        public override void FixedUpdateState()
        {
            _ctx.ApplyMovement(); // 런 상태일 때만 이동 물리 적용
        }

        public override void ExitState()
        {
            _ctx.Animator.SetBool(_ctx.AnimParamIsRunning, false);
        }
    }

    // 3. 점프 상태
    public class PlayerJumpState : PlayerBaseState
    {
        public PlayerJumpState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState()
        {
            _ctx.Animator.SetTrigger(_ctx.AnimParamJump);

            // 점프 물리력 적용 (velocity를 직접 제어하거나 AddForce 사용)
            _ctx.Rigidbody.linearVelocity = new Vector3(_ctx.Rigidbody.linearVelocity.x, _ctx.JumpForce, _ctx.Rigidbody.linearVelocity.z);
        }

        public override void UpdateState()
        {
            // y축 속도가 0 이하로 떨어지면(정점을 찍고 내려오면) Fall 상태로 전환
            if (_ctx.Rigidbody.linearVelocity.y <= 0f)
            {
                _ctx.ChangeState(_ctx.FallState);
            }
        }

        public override void FixedUpdateState()
        {
            _ctx.ApplyMovement(); // 공중에서도 조작하고 싶다면 남겨두고, 아니면 제거
        }

        public override void ExitState() { }
    }

    // 4. 추락(Fall) 상태
    public class PlayerFallState : PlayerBaseState
    {
        public PlayerFallState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState()
        {
            _ctx.Animator.SetBool(_ctx.AnimParamIsFalling, true);
        }

        public override void UpdateState()
        {
            if (_ctx.IsGrounded())
            {
                // 땅에 닿았을 때 입력이 있으면 달리기, 없으면 대기 상태로
                if (_ctx.CurrentMovementInput.sqrMagnitude > 0.01f)
                    _ctx.ChangeState(_ctx.RunState);
                else
                    _ctx.ChangeState(_ctx.IdleState);
            }
        }

        public override void FixedUpdateState()
        {
            _ctx.ApplyMovement(); // 공중 이동 허용
        }

        public override void ExitState()
        {
            _ctx.Animator.SetBool(_ctx.AnimParamIsFalling, false);
        }
    }
}*/