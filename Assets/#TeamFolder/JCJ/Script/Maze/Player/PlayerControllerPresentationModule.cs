using UnityEngine;

// PlayerController의 비주얼과 발소리 표현을 담당하는 모듈.

namespace _TeamFolder.JCJ.Script
{
    public static class PlayerControllerPresentationModule
    {
        public static IPlayerVisual AttachPreferredVisual(PlayerController owner, bool usePartyCharacter)
        {
            if (usePartyCharacter)
                return owner.gameObject.AddComponent<PartyCharacterVisual>();

            return owner.gameObject.AddComponent<PlayerVisualController>();
        }

        public static void ApplyLowFrictionMaterial(Collider collider, ref PhysicsMaterial lowFrictionPlayerMaterial)
        {
            if (collider == null) return;

            lowFrictionPlayerMaterial ??= new PhysicsMaterial("MazePlayerLowFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            collider.sharedMaterial = lowFrictionPlayerMaterial;
        }

        public static void HideBasePrimitiveMesh(PlayerController owner)
        {
            var meshRenderer = owner.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.enabled = false;
        }

        public static void AddDefaultTrail(PlayerController owner)
        {
            var trail = owner.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.6f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.02f;
            trail.minVertexDistance = 0.1f;
            trail.emitting = true;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var material = new Material(shader);
            var color = new Color(0.4f, 0.9f, 1f, 0.6f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            trail.material = material;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
        }

        /// <summary>지상에서 이동 입력이 있으면 발소리 루프, 아니면 정지.</summary>
        public static void UpdateFootstepLoop(bool isGrounded, Vector2 moveInput, bool isSprinting)
        {
            bool walking = isGrounded && moveInput.sqrMagnitude >= 0.04f;
            if (!walking)
            {
                JcjFootstepAudio.SetWalking(false);
                return;
            }

            JcjFootstepAudio.SetWalking(
                true,
                volumeScale: isSprinting ? 0.9f : 0.7f,
                pitch: isSprinting ? 1.08f : 1f);
        }

        public static void UpdateVisualState(IPlayerVisual visual, bool isGrounded, Vector2 moveInput, bool isSprinting, Rigidbody rb = null)
        {
            if (visual == null) return;
            if (!isGrounded) return;

            const float inputDeadzoneSq = 0.0064f;
            const float planarVelDeadzoneSq = 0.01f;
            bool inputIdle = moveInput.sqrMagnitude < inputDeadzoneSq;
            bool velIdle = true;
            if (rb != null)
            {
                Vector3 v = rb.linearVelocity;
                v.y = 0f;
                velIdle = v.sqrMagnitude < planarVelDeadzoneSq;
            }

            var gsm = GameStateManager.Instance;
            bool playing = gsm == null || gsm.CurrentState == GameState.Playing;
            if (!playing || (inputIdle && velIdle))
            {
                visual.OnIdle();
                return;
            }

            float speedNorm = Mathf.Clamp01(moveInput.magnitude);
            if (isSprinting) visual.OnSprint(speedNorm);
            else visual.OnWalk(speedNorm);
        }
    }
}
