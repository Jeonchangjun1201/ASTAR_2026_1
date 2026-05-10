using UnityEngine;

namespace JHJ.Scripts.Test.TestPlayer.FSM
{
    // 부모
    public abstract class PlayerBaseState
    {
        protected PlayerStateMachine _ctx;

        public PlayerBaseState(PlayerStateMachine currentContext)
        {
            _ctx = currentContext;
        }

        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void ExitState();
    }

    //  idle
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
                _ctx.ChangeState(_ctx.RunState);
            else if (!_ctx.IsGrounded())
                _ctx.ChangeState(_ctx.FallState);
        }

        public override void FixedUpdateState() { }
        public override void ExitState() { }
    }

    // run 
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
                _ctx.ChangeState(_ctx.IdleState);
            else if (!_ctx.IsGrounded())
                _ctx.ChangeState(_ctx.FallState);
        }

        public override void FixedUpdateState()
        {
            _ctx.ApplyMovement();
        }

        public override void ExitState()
        {
            _ctx.Animator.SetBool(_ctx.AnimParamIsRunning, false);
        }
    }

    // jump
    public class PlayerJumpState : PlayerBaseState
    {
        public PlayerJumpState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState()
        {
            _ctx.Animator.SetTrigger(_ctx.AnimParamJump);
            _ctx.Rigidbody.linearVelocity = new Vector3(_ctx.Rigidbody.linearVelocity.x, _ctx.JumpForce, _ctx.Rigidbody.linearVelocity.z);
        }

        public override void UpdateState()
        {
            if (_ctx.Rigidbody.linearVelocity.y <= 0f)
                _ctx.ChangeState(_ctx.FallState);
        }

        public override void FixedUpdateState()
        {
            _ctx.ApplyMovement(); // 공중 이동 제어
        }

        public override void ExitState() { }
    }

    //  fall 
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
                if (_ctx.CurrentMovementInput.sqrMagnitude > 0.01f)
                    _ctx.ChangeState(_ctx.RunState);
                else
                    _ctx.ChangeState(_ctx.IdleState);
            }
        }

        public override void FixedUpdateState()
        {
            _ctx.ApplyMovement();
        }

        public override void ExitState()
        {
            _ctx.Animator.SetBool(_ctx.AnimParamIsFalling, false);
        }
    }
}