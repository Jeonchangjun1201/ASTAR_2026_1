namespace JHJ.Scripts.Test.TestPlayer.FSM
{
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

    public class PlayerIdleState : PlayerBaseState
    {
        public PlayerIdleState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState() { } // 애니메이션 처리는 Controller에 위임

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

    public class PlayerRunState : PlayerBaseState
    {
        public PlayerRunState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState() { }

        public override void UpdateState()
        {
            if (_ctx.CurrentMovementInput.sqrMagnitude <= 0.01f)
                _ctx.ChangeState(_ctx.IdleState);
            else if (!_ctx.IsGrounded())
                _ctx.ChangeState(_ctx.FallState);
        }

        public override void FixedUpdateState() { }

        public override void ExitState() { }
    }

    public class PlayerJumpState : PlayerBaseState
    {
        public PlayerJumpState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState() { }

        public override void UpdateState()
        {
            if (_ctx.Rigidbody.linearVelocity.y <= 0f)
                _ctx.ChangeState(_ctx.FallState);
        }

        public override void FixedUpdateState() { }
        public override void ExitState() { }
    }

    public class PlayerFallState : PlayerBaseState
    {
        public PlayerFallState(PlayerStateMachine ctx) : base(ctx) { }

        public override void EnterState() { }

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

        public override void FixedUpdateState() { }

        public override void ExitState() { }
    }
}