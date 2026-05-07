using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 키 리바인딩 UI와 설정 서비스를 연결하는 바인더.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 저장된 키 설정을 Maze/Tile 플레이어의 InputActionMap에 연결해 런타임 키 변경을 반영한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeyRebindBinder : MonoBehaviour
    {
        private ISettingsService _settings;
        private readonly List<InputActionMap> _trackedMaps = new();

        private void Start()
        {
            _settings = SettingsService.EnsureInstance();
            _settings.OnChanged += HandleChanged;
            CollectMaps();
            HandleChanged(_settings.Data);
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.OnChanged -= HandleChanged;
        }

        public void Register(InputActionMap map)
        {
            // 새로 스폰된 플레이어가 만든 InputActionMap을 설정 시스템에 등록한다.
            // 런타임 스폰 구조라 씬 시작 시점에 없던 플레이어도 현재 키 설정을 즉시 받을 수 있다.
            if (map == null) return;
            if (!_trackedMaps.Contains(map)) _trackedMaps.Add(map);
            if (_settings != null) ApplyToMap(map, _settings.Data);
        }

        private void CollectMaps()
        {
            // 현재 씬에 존재하는 Maze/Tile 플레이어의 입력 맵을 모두 수집한다.
            // 서버 연동 후에도 로컬 플레이어 맵만 실제 Enable되므로, 여기서 원격 맵에 키 설정이 적용되어도 입력은 발생하지 않는다.
            _trackedMaps.Clear();
            var mazeControllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in mazeControllers)
            {
                if (pc == null) continue;
                var map = pc.GetInputMap();
                if (map != null && !_trackedMaps.Contains(map)) _trackedMaps.Add(map);
            }

            var tileControllers = Object.FindObjectsByType<_TeamFolder.JCJ.TileGame.PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in tileControllers)
            {
                if (pc == null) continue;
                var map = pc.GetInputMap();
                if (map != null && !_trackedMaps.Contains(map)) _trackedMaps.Add(map);
            }
        }

        private void HandleChanged(SettingsData data)
        {
            if (data == null) return;
            CollectMaps();
            foreach (var m in _trackedMaps)
            {
                ApplyToMap(m, data);
            }
        }

        private static void ApplyToMap(InputActionMap map, SettingsData data)
        {
            // SettingsData에 저장된 키 설정을 실제 InputActionMap override로 반영한다.
            // 이 함수는 설정 UI에서 키를 바꾸거나 기본값으로 되돌릴 때마다 호출된다.
            if (map == null || data == null) return;

            var move   = JCJInputActions.Find(map, JCJInputActions.ActionMove);
            var jump   = JCJInputActions.Find(map, JCJInputActions.ActionJump);
            var sprint = JCJInputActions.Find(map, JCJInputActions.ActionSprint);
            var menu   = JCJInputActions.Find(map, JCJInputActions.ActionMenu);

            if (move != null)
            {
                JCJInputActions.ReplaceCompositePart(move, JCJInputActions.CompositeMoveUp,    data.keyMoveUp);
                JCJInputActions.ReplaceCompositePart(move, JCJInputActions.CompositeMoveDown,  data.keyMoveDown);
                JCJInputActions.ReplaceCompositePart(move, JCJInputActions.CompositeMoveLeft,  data.keyMoveLeft);
                JCJInputActions.ReplaceCompositePart(move, JCJInputActions.CompositeMoveRight, data.keyMoveRight);
            }

            if (jump   != null) JCJInputActions.ReplaceFirstNonGamepadBinding(jump,   data.keyJump);
            if (sprint != null) JCJInputActions.ReplaceFirstNonGamepadBinding(sprint, data.keySprint);
            if (menu   != null) JCJInputActions.ReplaceFirstNonGamepadBinding(menu,   data.keyMenu);
        }
    }
}
