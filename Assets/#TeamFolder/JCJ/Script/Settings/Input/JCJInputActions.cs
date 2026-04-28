using UnityEngine.InputSystem;

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// Maze와 Tile에서 함께 쓰는 런타임 입력 액션 이름, 기본 키, 리바인딩 보조 함수를 모아둔다.
    /// </summary>
    public static class JCJInputActions
    {
        public const string MapName       = "JCJPlayer";
        public const string ActionMove    = "Move";
        public const string ActionJump    = "Jump";
        public const string ActionSprint  = "Sprint";
        public const string ActionLook    = "Look";
        public const string ActionMenu    = "ToggleMenu";

        public const string CompositeMoveUp    = "Up";
        public const string CompositeMoveDown  = "Down";
        public const string CompositeMoveLeft  = "Left";
        public const string CompositeMoveRight = "Right";

        public const string DefaultMoveUp     = "<Keyboard>/w";
        public const string DefaultMoveDown   = "<Keyboard>/s";
        public const string DefaultMoveLeft   = "<Keyboard>/a";
        public const string DefaultMoveRight  = "<Keyboard>/d";
        public const string DefaultJump       = "<Keyboard>/space";
        public const string DefaultSprint     = "<Keyboard>/leftShift";
        public const string DefaultMenu       = "<Keyboard>/escape";

        public static InputActionMap CreateMap()
        {
            // Maze와 Tile 플레이어가 공통으로 사용하는 입력 맵을 런타임 생성한다.
            // 서버 연동 시에도 각 PlayerController가 이 맵을 만들지만, 소유권이 없는 플레이어는 Enable하지 않는다.
            var map = new InputActionMap(MapName);

            // 2DVector는 WASD/방향키/게임패드 스틱을 같은 Move 값으로 합친다.
            // keyMoveUp은 UI에서 "앞으로 이동"으로 표시되며, 실제 월드 방향 변환은 각 PlayerController가 담당한다.
            var move = map.AddAction(ActionMove, InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With(CompositeMoveUp,    DefaultMoveUp)
                .With(CompositeMoveDown,  DefaultMoveDown)
                .With(CompositeMoveLeft,  DefaultMoveLeft)
                .With(CompositeMoveRight, DefaultMoveRight);
            move.AddCompositeBinding("2DVector")
                .With(CompositeMoveUp,    "<Keyboard>/upArrow")
                .With(CompositeMoveDown,  "<Keyboard>/downArrow")
                .With(CompositeMoveLeft,  "<Keyboard>/leftArrow")
                .With(CompositeMoveRight, "<Keyboard>/rightArrow");
            move.AddCompositeBinding("2DVector")
                .With(CompositeMoveUp,    "<Gamepad>/leftStick/up")
                .With(CompositeMoveDown,  "<Gamepad>/leftStick/down")
                .With(CompositeMoveLeft,  "<Gamepad>/leftStick/left")
                .With(CompositeMoveRight, "<Gamepad>/leftStick/right");

            var jump = map.AddAction(ActionJump, InputActionType.Button);
            jump.AddBinding(DefaultJump);
            jump.AddBinding("<Gamepad>/buttonSouth");

            var sprint = map.AddAction(ActionSprint, InputActionType.Button);
            sprint.AddBinding(DefaultSprint);
            sprint.AddBinding("<Gamepad>/leftShoulder");

            var look = map.AddAction(ActionLook, InputActionType.Value);
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick");

            var menu = map.AddAction(ActionMenu, InputActionType.Button);
            menu.AddBinding(DefaultMenu);
            menu.AddBinding("<Gamepad>/start");

            return map;
        }

        public static InputAction Find(InputActionMap map, string actionName)
        {
            return map?.FindAction(actionName, throwIfNotFound: false);
        }

        public static void ReplaceFirstNonGamepadBinding(InputAction action, string newPath)
        {
            // 키 리바인딩은 키보드/마우스 바인딩만 바꾼다.
            // 게임패드 기본 바인딩은 유지해서 리바인딩 후에도 패드 테스트가 가능하게 한다.
            if (action == null || string.IsNullOrEmpty(newPath)) return;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (string.IsNullOrEmpty(b.path)) continue;
                if (b.path.StartsWith("<Gamepad>")) continue;
                action.ApplyBindingOverride(i, newPath);
                return;
            }
        }

        public static void ReplaceCompositePart(InputAction moveAction, string partName, string newPath)
        {
            // Move 2DVector의 Up/Down/Left/Right 중 한 부분만 교체한다.
            // 이 함수가 잘못 바뀌면 W/S/A/D가 뒤집히므로 서버 담당자도 입력 문제 추적 시 먼저 확인하면 된다.
            if (moveAction == null || string.IsNullOrEmpty(partName) || string.IsNullOrEmpty(newPath)) return;
            for (int i = 0; i < moveAction.bindings.Count; i++)
            {
                var b = moveAction.bindings[i];
                if (!b.isPartOfComposite) continue;
                if (!string.Equals(b.name, partName, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(b.path)) continue;
                if (b.path.StartsWith("<Gamepad>")) continue;
                moveAction.ApplyBindingOverride(i, newPath);
                return;
            }
        }

        public static string GetCurrentNonGamepadPath(InputAction action)
        {
            if (action == null) return string.Empty;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (string.IsNullOrEmpty(b.effectivePath)) continue;
                if (b.effectivePath.StartsWith("<Gamepad>")) continue;
                return b.effectivePath;
            }
            return string.Empty;
        }

        public static string GetCurrentCompositePartPath(InputAction moveAction, string partName)
        {
            if (moveAction == null || string.IsNullOrEmpty(partName)) return string.Empty;
            for (int i = 0; i < moveAction.bindings.Count; i++)
            {
                var b = moveAction.bindings[i];
                if (!b.isPartOfComposite) continue;
                if (!string.Equals(b.name, partName, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrEmpty(b.effectivePath)) continue;
                if (b.effectivePath.StartsWith("<Gamepad>")) continue;
                return b.effectivePath;
            }
            return string.Empty;
        }
    }
}
