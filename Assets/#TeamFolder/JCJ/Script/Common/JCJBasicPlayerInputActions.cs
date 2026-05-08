using UnityEngine.InputSystem;

// 기본 플레이어용 Input Action 생성과 접근 유틸.

namespace _TeamFolder.JCJ.Script
{
    public static class JCJBasicPlayerInputActions
    {
        public const string MapName = "JCJBasicPlayer";
        public const string ActionMove = "Move";
        public const string ActionJump = "Jump";

        public const string CompositeMoveUp = "Up";
        public const string CompositeMoveDown = "Down";
        public const string CompositeMoveLeft = "Left";
        public const string CompositeMoveRight = "Right";

        public const string DefaultMoveUp = "<Keyboard>/w";
        public const string DefaultMoveDown = "<Keyboard>/s";
        public const string DefaultMoveLeft = "<Keyboard>/a";
        public const string DefaultMoveRight = "<Keyboard>/d";
        public const string DefaultJump = "<Keyboard>/space";

        public static InputActionMap CreateMap()
        {
            var map = new InputActionMap(MapName);

            var move = map.AddAction(ActionMove, InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With(CompositeMoveUp, DefaultMoveUp)
                .With(CompositeMoveDown, DefaultMoveDown)
                .With(CompositeMoveLeft, DefaultMoveLeft)
                .With(CompositeMoveRight, DefaultMoveRight);

            var jump = map.AddAction(ActionJump, InputActionType.Button);
            jump.AddBinding(DefaultJump);

            return map;
        }

        public static InputAction Find(InputActionMap map, string actionName)
        {
            return map?.FindAction(actionName, false);
        }

        public static void ApplyBindings(InputActionMap map, JCJBasicPlayerBindingsData data)
        {
            if (map == null || data == null)
            {
                return;
            }

            var move = Find(map, ActionMove);
            var jump = Find(map, ActionJump);

            if (move != null)
            {
                ReplaceCompositePart(move, CompositeMoveUp, data.moveUp);
                ReplaceCompositePart(move, CompositeMoveDown, data.moveDown);
                ReplaceCompositePart(move, CompositeMoveLeft, data.moveLeft);
                ReplaceCompositePart(move, CompositeMoveRight, data.moveRight);
            }

            if (jump != null)
            {
                ReplaceFirstBinding(jump, data.jump);
            }
        }

        public static string GetDefaultPath(JCJBasicPlayerBindingKey key)
        {
            return key switch
            {
                JCJBasicPlayerBindingKey.MoveUp => DefaultMoveUp,
                JCJBasicPlayerBindingKey.MoveDown => DefaultMoveDown,
                JCJBasicPlayerBindingKey.MoveLeft => DefaultMoveLeft,
                JCJBasicPlayerBindingKey.MoveRight => DefaultMoveRight,
                JCJBasicPlayerBindingKey.Jump => DefaultJump,
                _ => DefaultJump
            };
        }

        private static void ReplaceCompositePart(InputAction action, string partName, string path)
        {
            if (action == null || string.IsNullOrWhiteSpace(partName) || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (!binding.isPartOfComposite)
                {
                    continue;
                }

                if (!string.Equals(binding.name, partName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                action.ApplyBindingOverride(i, path);
                return;
            }
        }

        private static void ReplaceFirstBinding(InputAction action, string path)
        {
            if (action == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite)
                {
                    continue;
                }

                action.ApplyBindingOverride(i, path);
                return;
            }
        }
    }
}
