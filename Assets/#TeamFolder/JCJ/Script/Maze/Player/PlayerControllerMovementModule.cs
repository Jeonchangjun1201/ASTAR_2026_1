using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public static class PlayerControllerMovementModule
    {
        public static float ResolveExternalSpeedMul(float externalSpeedUntil, float externalSpeedMul)
        {
            if (Time.time >= externalSpeedUntil) return 1f;
            return Mathf.Clamp01(externalSpeedMul);
        }

        public static void UpdateStamina(
            ref float stamina,
            float maxStamina,
            float sprintDrainPerSec,
            float staminaRegenPerSec,
            float minStaminaToSprint,
            float sprintReenableDelay,
            bool sprintHeld,
            Vector2 moveInput,
            ref bool isSprinting,
            ref float sprintAvailableTime,
            ref bool staminaWasAbove)
        {
            bool hasInput = moveInput.sqrMagnitude > 0.01f;
            bool cooldownDone = Time.time >= sprintAvailableTime;
            bool canContinue = isSprinting && stamina > 0f;
            bool canStartFresh = cooldownDone && stamina > 0.01f && stamina >= minStaminaToSprint;
            bool canSprint = canContinue || canStartFresh;
            isSprinting = sprintHeld && hasInput && canSprint;

            if (isSprinting)
            {
                stamina -= sprintDrainPerSec * Time.fixedDeltaTime;
                if (stamina <= 0f)
                {
                    stamina = 0f;
                    isSprinting = false;
                    sprintAvailableTime = Time.time + Mathf.Max(0f, sprintReenableDelay);
                    if (staminaWasAbove) MazeAudio.Play(MazeSfx.StaminaOut, 0.9f);
                }
            }
            else
            {
                stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSec * Time.fixedDeltaTime);
            }

            staminaWasAbove = stamina > 0.01f;
        }

        public static void ApplyMovement(
            Rigidbody rb,
            Transform actor,
            Vector2 moveInput,
            Transform cameraTransform,
            float moveSpeed,
            float sprintMultiplier,
            bool isSprinting,
            float externalSpeedMul,
            float rotationSpeed)
        {
            Vector3 direction = GetCameraRelativeDirection(moveInput, cameraTransform);
            float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f) * externalSpeedMul;

            Vector3 velocity = direction * speed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            if (direction.sqrMagnitude > 0.01f)
            {
                actor.rotation = Quaternion.Slerp(
                    actor.rotation,
                    Quaternion.LookRotation(direction),
                    Time.fixedDeltaTime * rotationSpeed);
            }
        }

        public static void ApplyFriction(Rigidbody rb)
        {
            var velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }

        public static void ApplyJump(Rigidbody rb, float jumpForce)
        {
            var velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        public static Vector3 GetCameraRelativeDirection(Vector2 moveInput, Transform cameraTransform)
        {
            if (cameraTransform != null)
            {
                Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
                Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up);
                if (cameraForward.sqrMagnitude > 0.0001f && cameraRight.sqrMagnitude > 0.0001f)
                    return (cameraForward.normalized * moveInput.y + cameraRight.normalized * moveInput.x).normalized;
            }

            var rig = MazeCameraRig.Instance;
            if (rig != null)
            {
                Vector3 forward = rig.GetYawForward();
                Vector3 right = rig.GetYawRight();
                return (forward * moveInput.y + right * moveInput.x).normalized;
            }

            return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        public static bool CheckGround(Collider collider, Transform actor, float groundCheckDist, LayerMask groundLayer)
        {
            float bottomY;
            if (collider != null) bottomY = collider.bounds.min.y;
            else bottomY = actor.position.y - 0.5f;

            Vector3 origin = new(
                actor.position.x,
                bottomY + 0.05f,
                actor.position.z);

            if (Physics.Raycast(origin, Vector3.down, groundCheckDist + 0.05f, groundLayer, QueryTriggerInteraction.Ignore))
                return true;

            float smallRadius = 0.08f * Mathf.Abs(actor.lossyScale.x);
            return Physics.SphereCast(origin, smallRadius, Vector3.down, out _, groundCheckDist, groundLayer, QueryTriggerInteraction.Ignore);
        }
    }
}
