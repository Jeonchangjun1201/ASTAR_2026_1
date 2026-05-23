using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 지면/공중 전환과 낙하·착지 비주얼 트리거 판정.
    /// Update 루프에서 로컬/원격이 공유하는 상태 머신이다.
    /// </summary>
    public struct PlayerAirborneState
    {
        public bool IsGrounded;
        public bool WasGrounded;
        public bool IsFalling;
        public float AirborneSince;
        public float SpawnTime;

        public void OnSpawned()
        {
            SpawnTime = Time.time;
            IsGrounded = true;
            WasGrounded = true;
            IsFalling = false;
            AirborneSince = 0f;
        }
    }

    public readonly struct PlayerAirborneVisualDelta
    {
        public readonly bool Landed;
        public readonly bool StartedFall;

        public PlayerAirborneVisualDelta(bool landed, bool startedFall)
        {
            Landed = landed;
            StartedFall = startedFall;
        }

        public static PlayerAirborneVisualDelta None => new(false, false);
    }

    public static class PlayerControllerAirborneModule
    {
        public static PlayerAirborneVisualDelta Tick(
            ref PlayerAirborneState state,
            bool isGrounded,
            float velocityY,
            float fallVelocityThreshold,
            float spawnGrace,
            float fallAirborneDelay)
        {
            bool inGrace = (Time.time - state.SpawnTime) < spawnGrace;
            bool landed = false;
            bool startedFall = false;

            if (isGrounded)
            {
                state.AirborneSince = 0f;
                if (!state.WasGrounded && state.IsFalling && !inGrace)
                    landed = true;
                state.IsFalling = false;
            }
            else
            {
                if (state.AirborneSince <= 0f)
                    state.AirborneSince = Time.time;

                float airborneTime = Time.time - state.AirborneSince;
                if (!state.IsFalling
                    && !inGrace
                    && airborneTime > fallAirborneDelay
                    && velocityY < fallVelocityThreshold)
                {
                    state.IsFalling = true;
                    startedFall = true;
                }
            }

            state.WasGrounded = state.IsGrounded;
            state.IsGrounded = isGrounded;
            return new PlayerAirborneVisualDelta(landed, startedFall);
        }
    }
}
