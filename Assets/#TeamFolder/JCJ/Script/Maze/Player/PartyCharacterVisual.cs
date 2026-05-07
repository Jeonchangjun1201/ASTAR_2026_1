using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// 플레이어에 파티 캐릭터 비주얼과 애니메이션을 붙이는 처리.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// IPlayerVisual 구현 — 리깅 캐릭터 프리팹을 두고 Animator(char_AC) 트리거로 구동.
    /// 프리팹 없으면 Resources에서 character_default 로드(Resources/Prefabs 포함 에셋 전제).
    /// </summary>
    public class PartyCharacterVisual : MonoBehaviour, IPlayerVisual
    {
        [Header("캐릭터")]
        [Tooltip("Animator+char_AC 있는 프리팹.\n비우면 Resources에서 character_default 자동 로드.")]
        [SerializeField] private GameObject _characterPrefab;
        [SerializeField] private string _resourceFallback = "Prefabs/character_default";
        [SerializeField] private float _scale = 1f;
        [SerializeField] private Vector3 _localOffset = new(0f, -1f, 0f);
        [SerializeField] private string _battleCharacterPrefabPath = "Assets/#TeamFolder/JCJ/Shooter Pack/party_character.fbx";
        [SerializeField] private float _battleScale = 1f;
        [SerializeField] private Vector3 _battleLocalOffset = new(0f, -1f, 0f);
        [SerializeField] private Vector3 _battleLocalEuler = Vector3.zero;
        [SerializeField] private bool _hideBaseCapsule = true;

        [Header("애니메이터 트리거")]
        [SerializeField] private string _idleTrigger  = "idle";
        [SerializeField] private string _runTrigger   = "run";
        [SerializeField] private string _jumpTrigger  = "jump";
        [SerializeField] private string _fallTrigger  = "fall";
        [SerializeField] private string _getupTrigger = "getup";
        [SerializeField] private string _winTrigger   = "feel";

        [Header("Animator Speed")]
        [SerializeField] private float _walkAnimSpeed   = 1f;
        [SerializeField] private float _sprintAnimSpeed = 1.5f;
        [SerializeField] private string _arenaAnimationLibraryPath = "Arena/ArenaAnimationLibrary";
        [SerializeField] private string _battleIdleClipPath = "Assets/#TeamFolder/JCJ/Shooter Pack/rifle aiming idle.fbx";
        [SerializeField] private string _battleWalkClipPath = "Assets/#TeamFolder/JCJ/Shooter Pack/walking.fbx";
        [SerializeField] private string _battleSprintClipPath = "Assets/#TeamFolder/JCJ/Shooter Pack/rifle run.fbx";
        [SerializeField] private string _battleStopClipPath = "Assets/#TeamFolder/JCJ/Shooter Pack/stop walking.fbx";

        private GameObject _instance;
        private Animator _animator;
        private ArenaAnimationLibrary _animationLibrary;
        private PlayableGraph _playableGraph;
        private AnimationLayerMixerPlayable _layerMixer;
        private AnimatorControllerPlayable _controllerPlayable;
        private AnimationClipPlayable _actionClipPlayable;
        private AnimationClip _activeActionClip;
        private bool _actionClipLoop;
        private float _actionClipEndAt;
        private AnimationClip _battleIdleClip;
        private AnimationClip _battleWalkClip;
        private AnimationClip _battleSprintClip;
        private AnimationClip _battleStopClip;
        private AnimationClip _queuedLoopClip;
        private string _currentState = "";
        private Quaternion _targetLocalRotation = Quaternion.identity;
        private static readonly string[] _allTriggers =
            { "idle", "run", "jump", "fall", "getup", "feel" };

        private MaterialPropertyBlock _mpb;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (_hideBaseCapsule) TryHideBaseMesh();
            SpawnCharacter();
        }

        private void Update()
        {
            if (!_playableGraph.IsValid() || _actionClipLoop || _activeActionClip == null) return;
            if (Time.time < _actionClipEndAt) return;
            if (_queuedLoopClip != null)
            {
                var queuedClip = _queuedLoopClip;
                _queuedLoopClip = null;
                TryPlayLibraryClip(queuedClip, true);
                return;
            }
            StopActionClip();
        }

        private void LateUpdate()
        {
            if (_instance == null) return;
            _instance.transform.localRotation = _targetLocalRotation;
        }

        private void Start()
        {
            // 스폰 매니저가 IsLocalControlled를 세팅한 뒤에 색을 적용해야 원격 플레이어까지 바뀌지 않는다.
            RefreshCustomization();
        }

        private void OnEnable()
        {
            var svc = CustomizeService.EnsureInstance();
            if (svc != null) svc.OnChanged += HandleCustomizeChanged;
        }

        private void OnDisable()
        {
            var svc = CustomizeService.Instance;
            if (svc != null) svc.OnChanged -= HandleCustomizeChanged;
        }

        private void OnDestroy()
        {
            if (_playableGraph.IsValid()) _playableGraph.Destroy();
            if (_instance != null) Destroy(_instance);
        }

        private void HandleCustomizeChanged(CustomizeData data)
        {
            ApplyCustomization(data);
        }

        private void RefreshCustomization()
        {
            var svc = CustomizeService.EnsureInstance();
            if (svc == null) return;
            ApplyCustomization(svc.Data);
        }

        public void ApplyCustomization(CustomizeData data)
        {
            if (_instance == null || data == null) return;
            if (!IsCustomizationTarget())
            {
                // 원격/AI 플레이어는 각자 기본 팔레트나 스폰 색상을 유지해야 하므로 내 커스텀 색을 지운다.
                ClearBodyColorOverride();
                return;
            }
            ApplyBodyColor(data.bodyColor);
        }

        public void SetVisualHidden(bool hidden)
        {
            if (_instance != null) _instance.SetActive(!hidden);
        }

        public void RefreshVisualModel()
        {
            if (_playableGraph.IsValid()) _playableGraph.Destroy();
            if (_instance != null) Destroy(_instance);
            _instance = null;
            _animator = null;
            _activeActionClip = null;
            _queuedLoopClip = null;
            _currentState = "";
            SpawnCharacter();
            RefreshCustomization();
        }

        private void ApplyBodyColor(Color color)
        {
            // sharedMaterial을 직접 바꾸지 않고 PropertyBlock만 써서 같은 프리팹을 쓰는 다른 플레이어 색에는 영향 주지 않는다.
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            var renderers = _instance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(_baseColorId, color);
                _mpb.SetColor(_colorId, color);
                r.SetPropertyBlock(_mpb);
            }
        }

        private void ClearBodyColorOverride()
        {
            // 내 색 오버라이드가 잘못 적용된 경우를 대비해 렌더러별 PropertyBlock을 비워 원래 머티리얼 색으로 되돌린다.
            if (_instance == null) return;
            var renderers = _instance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }

        private bool IsCustomizationTarget()
        {
            // 커스터마이즈 저장값은 로컬 사용자 설정이므로 현재 조작권을 가진 플레이어에게만 적용한다.
            var mazePlayer = GetComponentInParent<PlayerController>();
            if (mazePlayer != null) return mazePlayer.IsLocalControlled;

            var tilePlayer = GetComponentInParent<_TeamFolder.JCJ.TileGame.PlayerController>();
            if (tilePlayer != null) return tilePlayer.IsLocalControlled;

            return true;
        }

        // ───────── Build ─────────
        private void SpawnCharacter()
        {
            var prefab = ResolvePrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[PartyCharacterVisual] No character prefab assigned and Resources fallback not found.");
                return;
            }

            _instance = Instantiate(prefab, transform);
            _instance.transform.localPosition = HasBattleWeapon() ? _battleLocalOffset : _localOffset;
            _targetLocalRotation = HasBattleWeapon() ? Quaternion.Euler(_battleLocalEuler) : Quaternion.identity;
            _instance.transform.localRotation = _targetLocalRotation;
            _instance.transform.localScale = Vector3.one * (HasBattleWeapon() ? _battleScale : _scale);

            _animator = _instance.GetComponentInChildren<Animator>();
            if (_animator == null && HasBattleWeapon())
            {
                _animator = _instance.GetComponent<Animator>();
                if (_animator == null) _animator = _instance.AddComponent<Animator>();
                TryAssignBattleAvatar();
            }
            if (_animator == null)
                Debug.LogWarning("[PartyCharacterVisual] Spawned character has no Animator component.");
            else
            {
                InitializeActionClipGraph();
                LoadBattleLocomotionClips();
                TriggerState(_idleTrigger);
            }
        }

        private GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            if (HasBattleWeapon() && !string.IsNullOrEmpty(_battleCharacterPrefabPath))
            {
                var battlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(_battleCharacterPrefabPath);
                if (battlePrefab != null) return battlePrefab;
            }
#endif
            if (_characterPrefab != null) return _characterPrefab;
            if (string.IsNullOrEmpty(_resourceFallback)) return null;
            return Resources.Load<GameObject>(_resourceFallback);
        }

        private void TryHideBaseMesh()
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        // ───────── IPlayerVisual ─────────
        public void OnIdle()                        => TriggerState(_idleTrigger);
        public void OnWalk(float speedNormalized)
        {
            if (TryPlayBattleLocomotion(_battleWalkClip, true))
            {
                _currentState = "battle_walk";
                return;
            }
            TriggerRun(_walkAnimSpeed);
        }

        public void OnSprint(float speedNormalized)
        {
            if (TryPlayBattleLocomotion(_battleSprintClip, true))
            {
                _currentState = "battle_sprint";
                return;
            }
            TriggerRun(_sprintAnimSpeed);
        }

        public void OnJump()
        {
            if (_animator == null) return;
            ClearAllTriggersExcept(_jumpTrigger);
            _animator.speed = 1f;
            SetTrigger(_jumpTrigger);
            _currentState = _jumpTrigger;
        }

        public void OnPickup()
        {
            if (TryPlayLibraryClip(_animationLibrary != null ? _animationLibrary.PickupClip : null, false)) return;
            OnCollect();
        }

        public void OnFall()
        {
            if (_animator == null) return;
            if (_currentState == _fallTrigger) return;
            ClearAllTriggersExcept(_fallTrigger);
            _animator.speed = 1f;
            SetTrigger(_fallTrigger);
            _currentState = _fallTrigger;
        }

        public void OnLand()
        {
            if (_animator == null) return;
            ClearAllTriggersExcept(_getupTrigger);
            _animator.speed = 1f;
            SetTrigger(_getupTrigger);
            _currentState = _getupTrigger;
        }

        public void OnCollect()
        {
            if (_animator == null) return;
            ClearAllTriggersExcept(_winTrigger);
            _animator.speed = 1f;
            SetTrigger(_winTrigger);
            _currentState = _winTrigger;
        }

        public void OnThrow()
        {
            if (TryPlayLibraryClip(_animationLibrary != null ? _animationLibrary.ThrowClip : null, false)) return;
            OnCollect();
        }

        public void SetCarryState(bool carrying, bool moving)
        {
            if (!carrying)
            {
                if (_actionClipLoop) StopActionClip();
                return;
            }

            if (_animationLibrary == null) return;
            var targetClip = moving ? _animationLibrary.CarryMoveClip : _animationLibrary.CarryIdleClip;
            TryPlayLibraryClip(targetClip, true);
        }

        private void TriggerRun(float speedMultiplier)
        {
            if (_animator == null) return;
            _animator.speed = speedMultiplier;
            if (_currentState != _runTrigger)
            {
                ClearAllTriggersExcept(_runTrigger);
                SetTrigger(_runTrigger);
                _currentState = _runTrigger;
            }
        }

        private void TriggerState(string trigger)
        {
            if (trigger == _idleTrigger && TryPlayBattleIdle()) return;
            if (_animator == null || string.IsNullOrEmpty(trigger)) return;
            if (_currentState == trigger) return;
            if (trigger != _runTrigger) _animator.speed = 1f;
            ClearAllTriggersExcept(trigger);
            SetTrigger(trigger);
            _currentState = trigger;
        }

        private bool TryPlayBattleIdle()
        {
            if (!HasBattleWeapon() || _battleIdleClip == null) return false;
            if (_currentState == "battle_idle" && _activeActionClip == _battleIdleClip && _actionClipLoop) return true;
            if ((_currentState == "battle_walk" || _currentState == "battle_sprint") && _battleStopClip != null)
            {
                _queuedLoopClip = _battleIdleClip;
                TryPlayLibraryClip(_battleStopClip, false);
                _currentState = "battle_stop";
                return true;
            }

            _queuedLoopClip = null;
            TryPlayLibraryClip(_battleIdleClip, true);
            _currentState = "battle_idle";
            return true;
        }

        private bool TryPlayBattleLocomotion(AnimationClip clip, bool loop)
        {
            if (!HasBattleWeapon() || clip == null) return false;
            _queuedLoopClip = null;
            return TryPlayLibraryClip(clip, loop);
        }

        private bool HasBattleWeapon()
        {
            return GetComponentInParent<_TeamFolder.JCJ.Battle.BattleWeaponManager>() != null;
        }

        /// <summary>
        /// 이번 상태 전환에서 쓰지 않을 대기 트리거를 지운다.
        /// 오래 남은 feel, fall 같은 트리거가 다음 AnyState 평가에서 갑자기 실행되는 일을 막는다.
        /// </summary>
        private void ClearAllTriggersExcept(string keep)
        {
            if (_animator == null) return;
            foreach (var t in _allTriggers)
            {
                if (t == keep) continue;
                ResetTrigger(t);
            }
        }

        /// <summary>게임 재시작 시 외부에서 상태 초기화.</summary>
        public void ResetState()
        {
            _currentState = "";
            StopActionClip();
            TriggerState(_idleTrigger);
        }

        private void InitializeActionClipGraph()
        {
            if (_animator == null) return;
            _animationLibrary = LoadAnimationLibrary();
            bool hasController = _animator.runtimeAnimatorController != null;
            if (_animationLibrary == null && !hasController) return;

            _playableGraph = PlayableGraph.Create("PartyCharacterVisualGraph");
            _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _layerMixer = AnimationLayerMixerPlayable.Create(_playableGraph, 2);
            if (hasController)
            {
                _controllerPlayable = AnimatorControllerPlayable.Create(_playableGraph, _animator.runtimeAnimatorController);
                _layerMixer.ConnectInput(0, _controllerPlayable, 0);
                _layerMixer.SetInputWeight(0, 1f);
            }
            else
            {
                _layerMixer.SetInputWeight(0, 0f);
            }

            var output = AnimationPlayableOutput.Create(_playableGraph, "Animation", _animator);
            output.SetSourcePlayable(_layerMixer);
            _playableGraph.Play();
        }

        private bool TryPlayLibraryClip(AnimationClip clip, bool loop)
        {
            if (clip == null || !_playableGraph.IsValid()) return false;
            if (_activeActionClip == clip && _actionClipLoop == loop) return true;

            if (_actionClipPlayable.IsValid())
            {
                _playableGraph.DestroyPlayable(_actionClipPlayable);
            }

            _actionClipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
            _actionClipPlayable.SetApplyFootIK(false);
            _actionClipPlayable.SetDuration(loop ? double.MaxValue : clip.length);
            _actionClipPlayable.SetTime(0d);
            _actionClipPlayable.SetSpeed(1d);
            _layerMixer.DisconnectInput(1);
            _layerMixer.ConnectInput(1, _actionClipPlayable, 0);
            _layerMixer.SetInputWeight(1, 1f);
            _activeActionClip = clip;
            _actionClipLoop = loop;
            _actionClipEndAt = Time.time + clip.length;
            return true;
        }

        private void StopActionClip()
        {
            if (!_playableGraph.IsValid()) return;
            if (_actionClipPlayable.IsValid())
            {
                _layerMixer.DisconnectInput(1);
                _playableGraph.DestroyPlayable(_actionClipPlayable);
            }

            _layerMixer.SetInputWeight(1, 0f);
            _activeActionClip = null;
            _actionClipLoop = false;
        }

        private void SetTrigger(string triggerName)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            if (_playableGraph.IsValid()) _controllerPlayable.SetTrigger(triggerName);
            else _animator.SetTrigger(triggerName);
        }

        private void ResetTrigger(string triggerName)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            if (_playableGraph.IsValid()) _controllerPlayable.ResetTrigger(triggerName);
            else _animator.ResetTrigger(triggerName);
        }

        private ArenaAnimationLibrary LoadAnimationLibrary()
        {
            var library = Resources.Load<ArenaAnimationLibrary>(_arenaAnimationLibraryPath);
#if UNITY_EDITOR
            if (library == null)
            {
                library = UnityEditor.AssetDatabase.LoadAssetAtPath<ArenaAnimationLibrary>("Assets/#TeamFolder/JCJ/Resources/Arena/ArenaAnimationLibrary.asset");
            }

            if (library == null)
            {
                library = ScriptableObject.CreateInstance<ArenaAnimationLibrary>();
                var serializedObject = new UnityEditor.SerializedObject(library);
                SetLibraryClip(serializedObject, "_throwClip", "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Throw.fbx", "Throw");
                SetLibraryClip(serializedObject, "_carryIdleClip", "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Box Idle.fbx", "CarryIdle");
                SetLibraryClip(serializedObject, "_carryMoveClip", "Assets/#TeamFolder/JCJ/FREE/Pack_FREE_PartyCharacters/Animations/Box Walk Arc.fbx", "CarryMove");
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
#endif
            return library;
        }

        private void LoadBattleLocomotionClips()
        {
#if UNITY_EDITOR
            _battleIdleClip = LoadAnimationClip(_battleIdleClipPath);
            _battleWalkClip = LoadAnimationClip(_battleWalkClipPath);
            _battleSprintClip = LoadAnimationClip(_battleSprintClipPath);
            _battleStopClip = LoadAnimationClip(_battleStopClipPath);
#endif
        }

        private void TryAssignBattleAvatar()
        {
#if UNITY_EDITOR
            if (_animator == null || string.IsNullOrEmpty(_battleCharacterPrefabPath)) return;
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(_battleCharacterPrefabPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                {
                    _animator.avatar = avatar;
                    return;
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static AnimationClip LoadAnimationClip(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            return null;
        }

        private static void SetLibraryClip(UnityEditor.SerializedObject serializedObject, string propertyName, string assetPath, string clipName)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null) return;
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip animationClip && animationClip.name == clipName)
                {
                    property.objectReferenceValue = animationClip;
                    return;
                }
            }
        }
#endif
    }
}
