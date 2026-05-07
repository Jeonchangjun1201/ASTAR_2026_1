using UnityEngine;

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

        public static void UpdateFootstepSfx(bool isGrounded, Vector2 moveInput, bool isSprinting, float walkFootstepInterval, float sprintFootstepInterval, ref float nextFootstepTime)
        {
            if (!isGrounded || moveInput.sqrMagnitude < 0.04f) return;
            if (Time.time < nextFootstepTime) return;

            float interval = isSprinting ? sprintFootstepInterval : walkFootstepInterval;
            nextFootstepTime = Time.time + interval;
            MazeAudio.Play(MazeSfx.Footstep, volumeScale: isSprinting ? 0.9f : 0.7f, pitch: Random.Range(0.92f, 1.08f));
        }

        public static void UpdateVisualState(IPlayerVisual visual, bool isGrounded, Vector2 moveInput, bool isSprinting)
        {
            if (visual == null) return;
            if (!isGrounded) return;

            var gsm = GameStateManager.Instance;
            bool playing = gsm == null || gsm.CurrentState == GameState.Playing;
            if (!playing || moveInput.sqrMagnitude < 0.01f)
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
