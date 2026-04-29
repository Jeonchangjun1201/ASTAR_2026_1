using System;
using UnityEngine;
using UnityEngine.InputSystem;
using _TeamFolder.JCJ.Script;

namespace _TeamFolder.JCJ.Script.Arena
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class ArenaPlayerController : MonoBehaviour
    {
        [SerializeField] private float _baseMoveSpeed = 6f;
        [SerializeField] private float _sprintMultiplier = 1.3f;
        [SerializeField] private float _baseJumpForce = 6f;
        [SerializeField] private float _rotationSpeed = 14f;
        [SerializeField] private float _groundCheckDistance = 0.3f;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private float _baseMaxHealth = 100f;
        [SerializeField] private float _baseMaxStamina = 100f;
        [SerializeField] private float _baseStaminaRegen = 22f;
        [SerializeField] private float _sprintDrainPerSecond = 30f;
        [SerializeField] private float _minStaminaToSprint = 10f;
        [SerializeField] private float _sprintReenableDelay = 1f;
        [SerializeField] private float _basePushDamage = 20f;
        [SerializeField] private float _basePushKnockback = 9f;
        [SerializeField] private float _attackCooldownSeconds = 0.8f;
        [SerializeField] private float _attackRecoverySeconds = 0.2f;
        [SerializeField] private float _minChargeSeconds = 0.2f;
        [SerializeField] private float _maxChargeSeconds = 1.0f;
        [SerializeField] private float _chargeDamageBonusMultiplier = 1.35f;
        [SerializeField] private float _chargeKnockbackBonusMultiplier = 1.5f;
        [SerializeField] private float _attackRadius = 0.85f;
        [SerializeField] private float _attackDistance = 1.4f;
        [SerializeField] private float _attackStaminaCost = 20f;
        [SerializeField] private float _dashStaminaCost = 25f;
        [SerializeField] private float _airDashForce = 8f;
        [SerializeField] private float _carrySearchRadius = 1.5f;
        [SerializeField] private float _carryDropDistance = 1f;
        [SerializeField] private float _fallKillY = -10f;
        [SerializeField] private bool _enableMouseLook = true;
        [SerializeField] private float _mouseSensitivity = 0.18f;
        [SerializeField] private bool _lockCursor = true;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private Transform _holdAnchor;
        private IPlayerVisual _visual;
        private InputActionMap _inputMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _lookAction;
        private InputAction _attackAction;
        private InputAction _interactAction;
        private InputAction _throwAction;
        private InputAction _dashAction;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _attackPressedAt;
        private bool _attackHeld;
        private bool _sprintHeld;
        private bool _isGrounded;
        private bool _doubleJumpUsed;
        private bool _airDashUsed;
        private bool _isSprinting;
        private float _attackReadyAt;
        private float _recoverUntil;
        private float _landingBurstUntil;
        private float _sprintAvailableAt;
        private float _currentHealth;
        private float _currentStamina;
        private bool _lastStandUsed;
        private ArenaCarryItem _currentCarryItem;
        private Vector3 _botMoveDirection;
        private float _botNextDecisionAt;
        private float _botNextJumpAt;
        private float _botAttackReleaseAt;

        public event Action<ArenaPlayerController> OnEliminated;

        public string PlayerId { get; private set; }
        public string DisplayName { get; private set; }
        public int TeamId { get; private set; }
        public bool IsLocalControlled { get; private set; }
        public bool IsAlive { get; private set; } = true;
        public bool IsReady { get; private set; }
        public bool IsSpectating { get; private set; }
        public ArenaResolvedStats Stats { get; private set; } = new ArenaResolvedStats();
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _baseMaxHealth * Stats.MaxHealthMultiplier;
        public float CurrentStamina => _currentStamina;
        public float MaxStamina => _baseMaxStamina;
        public bool IsCarrying => _currentCarryItem != null;
        public bool IsSprinting => _isSprinting;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            if (_groundMask.value == 0)
            {
                _groundMask = Physics.DefaultRaycastLayers;
            }

            _holdAnchor = new GameObject("HoldAnchor").transform;
            _holdAnchor.SetParent(transform, false);
            _holdAnchor.localPosition = new Vector3(0f, 0f, 0.9f);

            _visual = GetComponent<IPlayerVisual>();
            if (_visual == null)
            {
                _visual = gameObject.AddComponent<PartyCharacterVisual>();
            }

            var settings = SettingsService.EnsureInstance().Data;
            if (settings != null)
            {
                _mouseSensitivity = settings.cameraSensitivity;
            }

            BuildInput();
            ResetRuntimeState();
        }

        private void OnEnable()
        {
            ApplyInputState();
            ApplyCursorState();
        }

        private void OnDisable()
        {
            _inputMap?.Disable();
            ReleaseCursorState();
        }

        private void OnDestroy()
        {
            _inputMap?.Disable();
            _inputMap?.Dispose();
        }

        public void Configure(ArenaPlayerSessionState session, bool isLocalControlled)
        {
            PlayerId = session.PlayerId;
            DisplayName = session.DisplayName;
            TeamId = session.TeamId;
            IsLocalControlled = isLocalControlled;
            Stats = session.ResolvedStats.Clone();
            IsReady = session.IsReady;
            IsAlive = session.IsAlive;
            ApplyInputState();
            ResetRuntimeState();
        }

        public void SetPreparationReady(bool isReady)
        {
            IsReady = isReady;
        }

        public void SetSpectating(bool spectating)
        {
            IsSpectating = spectating;
            if (spectating)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                _attackHeld = false;
                _sprintHeld = false;
                _isSprinting = false;
                _botMoveDirection = Vector3.zero;
            }
        }

        public void ApplyColor(Color tint)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var material = renderers[i].material;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", tint);
                }

                material.color = tint;
            }
        }

        public void HealToFull()
        {
            _currentHealth = MaxHealth;
            _currentStamina = MaxStamina;
            _lastStandUsed = false;
            _doubleJumpUsed = false;
            _airDashUsed = false;
            IsAlive = true;
            IsSpectating = false;
        }

        public void SetStats(ArenaResolvedStats stats)
        {
            float previousMaxHealth = MaxHealth;
            float previousRatio = previousMaxHealth > 0f ? _currentHealth / previousMaxHealth : 1f;
            Stats = stats.Clone();
            _currentHealth = Mathf.Clamp01(previousRatio) * MaxHealth;
        }

        public void ReceiveHit(float rawDamage, Vector3 knockback, bool charged)
        {
            if (!IsAlive)
            {
                return;
            }

            float appliedDamage = rawDamage * Stats.DamageTakenMultiplier;
            _currentHealth -= appliedDamage;
            float knockbackMultiplier = charged ? Stats.ChargedKnockbackTakenMultiplier : Stats.KnockbackTakenMultiplier;
            _rigidbody.AddForce(knockback * knockbackMultiplier, ForceMode.VelocityChange);
            _recoverUntil = Time.time + (_attackRecoverySeconds * Stats.RecoveryTimeMultiplier);

            if (_currentHealth <= 0f)
            {
                if (Stats.HasLastStand && !_lastStandUsed)
                {
                    _lastStandUsed = true;
                    _currentHealth = MaxHealth * 0.15f;
                    return;
                }

                Eliminate();
            }
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            if (transform.position.y <= _fallKillY)
            {
                Eliminate();
                return;
            }

            if (IsLocalControlled && !IsSpectating)
            {
                _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
                _lookInput = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
                DispatchMouseLook();
            }
            else if (IsLocalControlled)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
            }

            UpdateGrounded();
            UpdateVisualState();
            UpdateBotBrain();
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }

            if (!ArenaGameManager.Instance || ArenaGameManager.Instance.CurrentPhase != ArenaPhase.Playing)
            {
                _isSprinting = false;
                ApplyPassiveFriction();
                return;
            }

            if (IsSpectating)
            {
                _isSprinting = false;
                ApplyPassiveFriction();
                return;
            }

            UpdateStamina();
            HandleMovement();
        }

        private void BuildInput()
        {
            _inputMap = ArenaInputActions.CreateMap();
            _moveAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionMove);
            _jumpAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionJump);
            _sprintAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionSprint);
            _lookAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionLook);
            _attackAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionAttack);
            _interactAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionInteract);
            _throwAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionThrow);
            _dashAction = ArenaInputActions.Find(_inputMap, ArenaInputActions.ActionDash);

            if (_jumpAction != null)
            {
                _jumpAction.performed += _ => TryJump();
            }

            if (_sprintAction != null)
            {
                _sprintAction.started += _ => _sprintHeld = true;
                _sprintAction.canceled += _ => _sprintHeld = false;
            }

            if (_attackAction != null)
            {
                _attackAction.started += _ => BeginAttackCharge();
                _attackAction.canceled += _ => ReleaseAttackCharge();
            }

            if (_interactAction != null)
            {
                _interactAction.performed += _ => TryInteract();
            }

            if (_throwAction != null)
            {
                _throwAction.performed += _ => TryThrow();
            }

            if (_dashAction != null)
            {
                _dashAction.performed += _ => TryAirDash();
            }
        }

        private void ApplyInputState()
        {
            if (_inputMap == null)
            {
                return;
            }

            if (IsLocalControlled && !IsSpectating)
            {
                _inputMap.Enable();
            }
            else
            {
                _inputMap.Disable();
            }
        }

        private void ResetRuntimeState()
        {
            _currentHealth = MaxHealth;
            _currentStamina = MaxStamina;
            _doubleJumpUsed = false;
            _airDashUsed = false;
            _lastStandUsed = false;
            _attackReadyAt = 0f;
            _recoverUntil = 0f;
            _landingBurstUntil = 0f;
            _sprintAvailableAt = 0f;
            _isSprinting = false;
            _sprintHeld = false;
            _botMoveDirection = Vector3.zero;
            _botNextDecisionAt = 0f;
            _botNextJumpAt = 0f;
            _botAttackReleaseAt = 0f;
        }

        private void UpdateGrounded()
        {
            float bottomY = _collider.bounds.min.y;
            Vector3 origin = new Vector3(transform.position.x, bottomY + 0.05f, transform.position.z);
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics.Raycast(origin, Vector3.down, _groundCheckDistance + 0.05f, _groundMask, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                _doubleJumpUsed = false;
                _airDashUsed = false;
                if (!wasGrounded && Stats.LandingBurstMultiplier > 0f)
                {
                    _landingBurstUntil = Time.time + 1f;
                }
            }
        }

        private void UpdateVisualState()
        {
            if (_visual == null)
            {
                return;
            }

            if (IsCarrying)
            {
                float speed = new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z).magnitude;
                _visual.SetCarryState(true, speed > 0.25f);
                return;
            }

            if (!_isGrounded)
            {
                if (_rigidbody.linearVelocity.y > 0.1f)
                {
                    _visual.OnJump();
                }
                else
                {
                    _visual.OnFall();
                }
                return;
            }

            float horizontalSpeed = new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z).magnitude;
            if (horizontalSpeed > 0.25f)
            {
                _visual.SetCarryState(false, false);
                if (_isSprinting)
                {
                    _visual.OnSprint(Mathf.Clamp01(horizontalSpeed / ResolveMoveSpeed()));
                }
                else
                {
                    _visual.OnWalk(Mathf.Clamp01(horizontalSpeed / ResolveMoveSpeed()));
                }
            }
            else
            {
                _visual.SetCarryState(false, false);
                _visual.OnIdle();
            }
        }

        private void UpdateStamina()
        {
            if (ArenaGameManager.Instance == null || ArenaGameManager.Instance.CurrentPhase != ArenaPhase.Playing)
            {
                return;
            }

            if (IsLocalControlled)
            {
                bool hasMoveInput = _moveInput.sqrMagnitude > 0.01f;
                bool canContinueSprint = _isSprinting && _currentStamina > 0f;
                bool canStartSprint = Time.time >= _sprintAvailableAt && _currentStamina >= _minStaminaToSprint;
                _isSprinting = _sprintHeld && hasMoveInput && (canContinueSprint || canStartSprint);
            }
            else
            {
                _isSprinting = false;
            }

            if (_isSprinting)
            {
                _currentStamina -= _sprintDrainPerSecond * Time.fixedDeltaTime;
                if (_currentStamina <= 0f)
                {
                    _currentStamina = 0f;
                    _isSprinting = false;
                    _sprintAvailableAt = Time.time + _sprintReenableDelay;
                }

                return;
            }

            float regen = _baseStaminaRegen * Stats.StaminaRegenMultiplier * Time.fixedDeltaTime;
            _currentStamina = Mathf.Min(MaxStamina, _currentStamina + regen);
        }

        private void HandleMovement()
        {
            Vector3 direction = ResolveDesiredMovementDirection();
            float moveSpeed = ResolveMoveSpeed();
            Vector3 targetVelocity = direction * moveSpeed;
            Vector3 currentVelocity = _rigidbody.linearVelocity;

            if (_isGrounded)
            {
                currentVelocity.x = targetVelocity.x;
                currentVelocity.z = targetVelocity.z;
            }
            else
            {
                float airControl = 6f * Stats.AirControlMultiplier * Time.fixedDeltaTime;
                currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetVelocity.x, airControl);
                currentVelocity.z = Mathf.Lerp(currentVelocity.z, targetVelocity.z, airControl);
            }

            _rigidbody.linearVelocity = currentVelocity;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * _rotationSpeed);
            }
        }

        private Vector3 GetCameraRelativeDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude <= 0.001f)
            {
                return Vector3.zero;
            }

            var mazeRig = MazeCameraRig.Instance;
            if (mazeRig != null)
            {
                Vector3 yawForward = mazeRig.GetYawForward();
                Vector3 yawRight = mazeRig.GetYawRight();
                return (yawForward * moveInput.y + yawRight * moveInput.x).normalized;
            }

            Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform == null)
            {
                return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return (forward * moveInput.y + right * moveInput.x).normalized;
        }

        private Vector3 ResolveDesiredMovementDirection()
        {
            if (IsLocalControlled)
            {
                return GetCameraRelativeDirection(_moveInput);
            }

            return _botMoveDirection;
        }

        private float ResolveMoveSpeed()
        {
            float moveSpeed = _baseMoveSpeed * Stats.MoveSpeedMultiplier;
            if (_isSprinting)
            {
                moveSpeed *= _sprintMultiplier;
            }

            if (Time.time < _landingBurstUntil)
            {
                moveSpeed *= 1f + Stats.LandingBurstMultiplier;
            }

            if (_currentCarryItem != null)
            {
                float penalty = _currentCarryItem.BaseCarryMovePenaltyPercent * _currentCarryItem.ResolveCarryMovePenaltyMultiplier(Stats.Strength);
                penalty *= Stats.CarryMovePenaltyMultiplier;
                moveSpeed *= Mathf.Clamp01(1f - penalty);
            }

            return moveSpeed;
        }

        private void TryJump()
        {
            if (!CanControl())
            {
                return;
            }

            if (_isGrounded)
            {
                JumpWithForce(_baseJumpForce * Stats.JumpForceMultiplier);
                return;
            }

            if (Stats.HasDoubleJump && !_doubleJumpUsed)
            {
                _doubleJumpUsed = true;
                JumpWithForce(_baseJumpForce * Stats.JumpForceMultiplier);
            }
        }

        private void JumpWithForce(float force)
        {
            var velocity = _rigidbody.linearVelocity;
            velocity.y = 0f;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.AddForce(Vector3.up * force, ForceMode.VelocityChange);
            _visual?.OnJump();
            ArenaGameManager.Instance?.NotifyCombatAction(this, ArenaCombatActionType.Jump, string.Empty, 0, Vector3.up);
        }

        private void TryAirDash()
        {
            if (!CanControl() || !Stats.HasAirDash || _isGrounded || _airDashUsed)
            {
                return;
            }

            if (!TryConsumeStamina(_dashStaminaCost))
            {
                return;
            }

            _airDashUsed = true;
            Vector3 direction = GetCameraRelativeDirection(_moveInput);
            if (direction == Vector3.zero)
            {
                direction = transform.forward;
            }

            var velocity = _rigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.AddForce(direction * _airDashForce, ForceMode.VelocityChange);
            ArenaGameManager.Instance?.NotifyCombatAction(this, ArenaCombatActionType.AirDash, string.Empty, 0, direction);
        }

        private void BeginAttackCharge()
        {
            if (!CanControl() || _currentCarryItem != null)
            {
                return;
            }

            _attackHeld = true;
            _attackPressedAt = Time.time;
        }

        private void ReleaseAttackCharge()
        {
            if (!_attackHeld || !CanControl() || _currentCarryItem != null)
            {
                return;
            }

            _attackHeld = false;
            if (Time.time < _attackReadyAt || Time.time < _recoverUntil)
            {
                return;
            }

            if (!TryConsumeStamina(_attackStaminaCost))
            {
                return;
            }

            float effectiveChargeWindow = _maxChargeSeconds * Stats.ChargeTimeMultiplier;
            effectiveChargeWindow = Mathf.Max(_minChargeSeconds, effectiveChargeWindow);
            float heldTime = Time.time - _attackPressedAt;
            float charge01 = Mathf.Clamp01(heldTime / effectiveChargeWindow);
            bool charged = heldTime >= _minChargeSeconds;

            float damage = _basePushDamage;
            float knockback = _basePushKnockback * Stats.KnockbackDealtMultiplier;
            if (charged)
            {
                damage *= _chargeDamageBonusMultiplier;
                knockback *= _chargeKnockbackBonusMultiplier * Stats.ChargedPushPowerMultiplier;
            }

            var target = FindAttackTarget();
            Vector3 direction = transform.forward;
            if (target != null)
            {
                direction = (target.transform.position - transform.position).normalized;
                target.ReceiveHit(damage, direction * knockback, charged);
            }

            _visual?.OnPush();
            _attackReadyAt = Time.time + (_attackCooldownSeconds * Stats.AttackCooldownMultiplier);
            _recoverUntil = Time.time + (_attackRecoverySeconds * Stats.RecoveryTimeMultiplier);
            int chargeMs = Mathf.RoundToInt(heldTime * 1000f);
            ArenaGameManager.Instance?.NotifyCombatAction(this, charged ? ArenaCombatActionType.ChargedPush : ArenaCombatActionType.Push, target != null ? target.PlayerId : string.Empty, chargeMs, direction * Mathf.Max(charge01, 1f));
        }

        private ArenaPlayerController FindAttackTarget()
        {
            Vector3 center = transform.position + transform.forward * _attackDistance;
            Collider[] hits = Physics.OverlapSphere(center, _attackRadius, ~0, QueryTriggerInteraction.Ignore);
            ArenaPlayerController best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i].GetComponentInParent<ArenaPlayerController>();
                if (candidate == null || candidate == this || !candidate.IsAlive)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void TryInteract()
        {
            if (!CanControl())
            {
                return;
            }

            if (_currentCarryItem != null)
            {
                DropCurrentCarry();
                return;
            }

            ArenaCarryItem targetItem = FindCarryItem();
            if (targetItem == null)
            {
                return;
            }

            if (!targetItem.CanPickup(Stats.Strength))
            {
                return;
            }

            _currentCarryItem = targetItem;
            _currentCarryItem.AttachToOwner(this, _holdAnchor);
            _recoverUntil = Time.time + (targetItem.BasePickupTime * targetItem.ResolvePickupDurationMultiplier(Stats.Strength));
            _visual?.OnPickup();
            _visual?.SetCarryState(true, false);
            ArenaGameManager.Instance?.NotifyCombatAction(this, ArenaCombatActionType.PickUp, targetItem.ItemId, 0, transform.forward);
        }

        private void TryThrow()
        {
            if (!CanControl() || _currentCarryItem == null)
            {
                return;
            }

            float strengthMultiplier = _currentCarryItem.ResolveThrowPowerMultiplier(Stats.Strength) * Stats.ThrowPowerMultiplier;
            float throwPower = _currentCarryItem.BaseThrowPower * strengthMultiplier;
            Vector3 direction = transform.forward + Vector3.up * 0.2f;
            direction.Normalize();
            Vector3 releasePosition = _holdAnchor.position + transform.forward * 0.25f;
            Quaternion releaseRotation = transform.rotation;
            _currentCarryItem.Release(releasePosition, releaseRotation, direction * throwPower);
            string itemId = _currentCarryItem.ItemId;
            _currentCarryItem = null;
            _visual?.OnThrow();
            ArenaGameManager.Instance?.NotifyCombatAction(this, ArenaCombatActionType.Throw, itemId, 0, direction);
        }

        private void DropCurrentCarry()
        {
            if (_currentCarryItem == null)
            {
                return;
            }

            Vector3 dropPosition = transform.position + transform.forward * _carryDropDistance;
            Quaternion dropRotation = transform.rotation;
            string itemId = _currentCarryItem.ItemId;
            _currentCarryItem.Drop(dropPosition, dropRotation);
            _currentCarryItem = null;
            _visual?.SetCarryState(false, false);
            ArenaGameManager.Instance?.NotifyCombatAction(this, ArenaCombatActionType.Drop, itemId, 0, transform.forward);
        }

        private ArenaCarryItem FindCarryItem()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward, _carrySearchRadius, ~0, QueryTriggerInteraction.Collide);
            ArenaCarryItem best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var item = hits[i].GetComponentInParent<ArenaCarryItem>();
                if (item == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, item.transform.position);
                if (distance < bestDistance)
                {
                    best = item;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private bool TryConsumeStamina(float baseCost)
        {
            float cost = baseCost * Stats.StaminaUseMultiplier;
            if (_currentStamina < cost)
            {
                return false;
            }

            _currentStamina -= cost;
            return true;
        }

        private bool CanControl()
        {
            return ArenaGameManager.Instance != null
                && ArenaGameManager.Instance.CurrentPhase == ArenaPhase.Playing
                && IsAlive
                && !IsSpectating
                && Time.time >= _recoverUntil;
        }

        private void DispatchMouseLook()
        {
            if (!_enableMouseLook)
            {
                return;
            }

            var mazeRig = MazeCameraRig.Instance;
            if (mazeRig == null)
            {
                return;
            }

            mazeRig.AddLook(_lookInput * _mouseSensitivity);
        }

        private void ApplyCursorState()
        {
            if (!IsLocalControlled || !_lockCursor)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ReleaseCursorState()
        {
            if (!_lockCursor)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UpdateBotBrain()
        {
            if (IsLocalControlled || !IsAlive || IsSpectating)
            {
                return;
            }

            var manager = ArenaGameManager.Instance;
            if (manager == null || manager.CurrentPhase != ArenaPhase.Playing)
            {
                _botMoveDirection = Vector3.zero;
                _attackHeld = false;
                return;
            }

            if (_attackHeld && Time.time >= _botAttackReleaseAt)
            {
                ReleaseAttackCharge();
            }

            if (Time.time < _botNextDecisionAt)
            {
                return;
            }

            _botNextDecisionAt = Time.time + UnityEngine.Random.Range(0.08f, 0.16f);
            var target = FindBestBotTarget();
            Vector3 centerOffset = Vector3.ProjectOnPlane(manager.GetArenaCenter() - transform.position, Vector3.up);
            bool nearEdge = new Vector2(transform.position.x, transform.position.z).magnitude >= ArenaDesignValues.ArenaPlayableRadius;

            if (nearEdge && centerOffset.sqrMagnitude > 0.01f)
            {
                _botMoveDirection = centerOffset.normalized;
                if (_isGrounded && Time.time >= _botNextJumpAt)
                {
                    _botNextJumpAt = Time.time + UnityEngine.Random.Range(1.1f, 1.8f);
                    TryJump();
                }
                return;
            }

            if (_currentCarryItem != null)
            {
                if (target != null)
                {
                    Vector3 throwDirection = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
                    _botMoveDirection = throwDirection.normalized;
                    if (throwDirection.sqrMagnitude <= 48f)
                    {
                        TryThrow();
                    }
                }
                else
                {
                    _botMoveDirection = centerOffset.sqrMagnitude > 0.01f ? centerOffset.normalized : transform.forward;
                }

                return;
            }

            if (TryBotPickupOpportunity(target))
            {
                return;
            }

            if (target == null)
            {
                _botMoveDirection = centerOffset.sqrMagnitude > 0.01f ? centerOffset.normalized : Vector3.zero;
                return;
            }

            Vector3 toTarget = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up);
            float distance = toTarget.magnitude;
            _botMoveDirection = distance > 0.01f ? toTarget.normalized : Vector3.zero;

            if (_isGrounded && target.transform.position.y - transform.position.y > 0.9f && Time.time >= _botNextJumpAt)
            {
                _botNextJumpAt = Time.time + UnityEngine.Random.Range(0.9f, 1.6f);
                TryJump();
            }

            if (distance <= _attackDistance + 0.35f)
            {
                if (!_attackHeld && Time.time >= _attackReadyAt && Time.time >= _recoverUntil)
                {
                    BeginAttackCharge();
                    _botAttackReleaseAt = Time.time + UnityEngine.Random.Range(0.22f, 0.45f);
                }
            }
            else if (_attackHeld)
            {
                ReleaseAttackCharge();
            }
        }

        private bool TryBotPickupOpportunity(ArenaPlayerController target)
        {
            ArenaCarryItem item = FindCarryItem();
            if (item == null || !item.CanPickup(Stats.Strength))
            {
                return false;
            }

            if (target != null && Vector3.Distance(transform.position, target.transform.position) < 2f)
            {
                return false;
            }

            TryInteract();
            _botMoveDirection = Vector3.zero;
            return true;
        }

        private ArenaPlayerController FindBestBotTarget()
        {
            ArenaPlayerController best = null;
            float bestScore = float.MaxValue;
            var candidates = FindObjectsByType<ArenaPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || candidate == this || !candidate.IsAlive)
                {
                    continue;
                }

                if (candidate.TeamId == TeamId)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                float edgeBias = Mathf.Abs(new Vector2(candidate.transform.position.x, candidate.transform.position.z).magnitude - ArenaDesignValues.ArenaPlayableRadius);
                float score = distance - (1.5f / Mathf.Max(0.25f, edgeBias));
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private void ApplyPassiveFriction()
        {
            var velocity = _rigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            _rigidbody.linearVelocity = velocity;
        }

        private void Eliminate()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            IsSpectating = true;
            DropCurrentCarry();
            OnEliminated?.Invoke(this);
        }
    }
}
