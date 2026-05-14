using System;
using UnityEngine;
using UnityEngine.InputSystem;

// PlayerController의 입력 액션 생성과 입력 상태 반영을 담당하는 모듈.

namespace _TeamFolder.JCJ.Script
{
    public static class PlayerControllerInputModule
    {
        public static void BuildInputActions(
            Func<bool> isLocalControlled,
            Action onJumpBuffered,
            Action<bool> onSprintHeldChanged,
            out InputActionMap inputMap,
            out InputAction moveAction,
            out InputAction jumpAction,
            out InputAction sprintAction,
            out InputAction lookAction)
        {
            inputMap = JCJInputActions.CreateMap();
            moveAction = JCJInputActions.Find(inputMap, JCJInputActions.ActionMove);
            jumpAction = JCJInputActions.Find(inputMap, JCJInputActions.ActionJump);
            sprintAction = JCJInputActions.Find(inputMap, JCJInputActions.ActionSprint);
            lookAction = JCJInputActions.Find(inputMap, JCJInputActions.ActionLook);

            if (jumpAction != null)
            {
                jumpAction.performed += _ =>
                {
                    if (!isLocalControlled()) return;
                    onJumpBuffered?.Invoke();
                };
            }

            if (sprintAction != null)
            {
                sprintAction.started += _ =>
                {
                    if (!isLocalControlled()) return;
                    onSprintHeldChanged?.Invoke(true);
                };
                sprintAction.canceled += _ => onSprintHeldChanged?.Invoke(false);
            }
        }

        public static void ApplyLocalControlState(bool isLocalControlled, InputActionMap inputMap)
        {
            if (inputMap == null) return;
            if (isLocalControlled) inputMap.Enable();
            else inputMap.Disable();
        }

        public static void DispatchMouseLook(bool enableMouseLook, Vector2 lookInput, float mouseSensitivity)
        {
            if (!enableMouseLook) return;
            var rig = MazeCameraRig.Instance;
            if (rig == null) return;
            rig.AddLook(lookInput * mouseSensitivity);
        }

        public static void SetMovementEnabled(bool enabled, InputAction moveAction, InputAction jumpAction, InputAction sprintAction)
        {
            if (moveAction == null || sprintAction == null) return;
            if (enabled)
            {
                moveAction.Enable();
                if (jumpAction != null) jumpAction.Enable();
                sprintAction.Enable();
            }
            else
            {
                moveAction.Disable();
                if (jumpAction != null) jumpAction.Disable();
                sprintAction.Disable();
            }
        }

        public static void ClearInputState(ref Vector2 moveInput, ref Vector2 lookInput, ref bool sprintHeld, ref bool isSprinting, ref float jumpBufferedUntil)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            sprintHeld = false;
            isSprinting = false;
            jumpBufferedUntil = 0f;
        }
    }
}
