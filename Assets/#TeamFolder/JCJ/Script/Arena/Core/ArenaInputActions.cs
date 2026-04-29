using UnityEngine.InputSystem;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Arena
{
    public static class ArenaInputActions
    {
        public const string MapName = "ArenaPlayer";
        public const string ActionMove = JCJInputActions.ActionMove;
        public const string ActionJump = JCJInputActions.ActionJump;
        public const string ActionSprint = JCJInputActions.ActionSprint;
        public const string ActionLook = JCJInputActions.ActionLook;
        public const string ActionAttack = "Attack";
        public const string ActionInteract = "Interact";
        public const string ActionThrow = "Throw";
        public const string ActionDash = "Dash";

        public static InputActionMap CreateMap()
        {
            var map = JCJInputActions.CreateMap();

            var attack = map.AddAction(ActionAttack, InputActionType.Button);
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/rightTrigger");

            var interact = map.AddAction(ActionInteract, InputActionType.Button);
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            var throwAction = map.AddAction(ActionThrow, InputActionType.Button);
            throwAction.AddBinding("<Keyboard>/q");
            throwAction.AddBinding("<Gamepad>/buttonEast");

            var dash = map.AddAction(ActionDash, InputActionType.Button);
            dash.AddBinding("<Keyboard>/leftCtrl");
            dash.AddBinding("<Gamepad>/rightShoulder");

            return map;
        }

        public static InputAction Find(InputActionMap map, string actionName)
        {
            return map?.FindAction(actionName, false);
        }
    }
}
