using System;
using UnityEngine;
using UnityEngine.Events;
using NoLightBelow.Cards;
using NoLightBelow.Combat;
using NoLightBelow.Core;
using NoLightBelow.Player.States;

namespace NoLightBelow.Player
{
    /// <summary>
    /// Controlador Principal de Gameplay em 3ª Pessoa (PlayerController).
    /// Gerencia movimentação com CharacterController, gravidade, aceleração/desaceleração,
    /// rotação suave em direção à câmera, integração com o Unity New Input System e State Pattern.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion Settings")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float runSpeed = 7.0f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float deceleration = 18f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private float gravity = 22f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 8.5f;
        [SerializeField] private float jumpStaminaCost = 8f;

        [Header("Dodge Roll Settings")]
        [SerializeField] private float dodgeSpeed = 13.5f;
        [SerializeField] private float dodgeDuration = 0.38f;
        [SerializeField] private float dodgeStaminaCost = 22f;
        [SerializeField] private float dodgeCooldown = 0.25f;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;

        [Header("Animator & Game Feel Events")]
        [Tooltip("Disparado para atualizar parâmetro 'Speed' (float) do Animator")]
        public UnityEvent<float> OnSpeedChanged;

        [Tooltip("Disparado para atualizar parâmetro 'AttackIndex' (int) do Animator")]
        public UnityEvent<int> OnAttackTriggered;

        [Tooltip("Disparado para atualizar parâmetro 'IsBlocking' (bool) do Animator")]
        public UnityEvent<bool> OnBlockChanged;

        [Tooltip("Disparado para acionar trigger 'Hit' do Animator")]
        public UnityEvent OnHurtTriggered;

        [Tooltip("Disparado para acionar trigger 'Die' do Animator")]
        public UnityEvent OnDeathTriggered;

        [Header("Legacy & Combat Events")]
        public UnityEvent<DamageInfo> OnDamageTakenEvent;
        public UnityEvent OnDodgeExecutedEvent;
        public UnityEvent OnDeathEvent;
        public UnityEvent<IInteractable> OnInteractableFoundEvent;

        // Components
        public CharacterController Controller { get; private set; }
        public PlayerStats Stats { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public PlayerHealth Health { get; private set; }
        public Animator Animator { get; private set; }
        public Camera MainCamera { get; private set; }

        // State Machine & Concrete States
        public PlayerStateMachine StateMachine { get; private set; }
        public PlayerGroundedState GroundedState { get; private set; }
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerBlockState BlockState { get; private set; }
        public PlayerDodgeState DodgeState { get; private set; }
        public PlayerHurtState HurtState { get; private set; }
        public PlayerDeadState DeadState { get; private set; }

        // State Properties
        public bool IsBlocking { get; set; }
        public bool IsGrounded => Controller != null && Controller.isGrounded;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float RotationSpeed => rotationSpeed;
        public float DodgeSpeed => dodgeSpeed;
        public float DodgeDuration => dodgeDuration;
        public float DodgeStaminaCost => dodgeStaminaCost;
        public string CurrentStateName => StateMachine?.CurrentState?.StateName ?? "None";
        public IInteractable CurrentInteractable { get; private set; }

        private Vector3 _velocity;
        private float _lastDodgeEndTime;
        private Component _impulseSourceComponent;
        private System.Reflection.MethodInfo _generateImpulseMethod;

        // Animator Parameter Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackIndexHash = Animator.StringToHash("AttackIndex");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
        private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        private static readonly int DieTriggerHash = Animator.StringToHash("Die");

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            Stats = GetComponent<PlayerStats>();
            Combat = GetComponent<PlayerCombat>();
            Health = GetComponent<PlayerHealth>();
            if (Health == null)
            {
                Health = gameObject.AddComponent<PlayerHealth>();
            }

            Animator = GetComponentInChildren<Animator>();
            MainCamera = Camera.main;

            InitImpulseSource();

            // Initialize State Machine
            StateMachine = new PlayerStateMachine();
            GroundedState = new PlayerGroundedState(this, StateMachine);
            IdleState = new PlayerIdleState(this, StateMachine);
            MoveState = new PlayerMoveState(this, StateMachine);
            AttackState = new PlayerAttackState(this, StateMachine);
            BlockState = new PlayerBlockState(this, StateMachine);
            DodgeState = new PlayerDodgeState(this, StateMachine);
            HurtState = new PlayerHurtState(this, StateMachine);
            DeadState = new PlayerDeadState(this, StateMachine);

            StateMachine.Initialize(GroundedState);
        }

        private void Start()
        {
            if (MainCamera == null)
            {
                MainCamera = FindAnyObjectByType<Camera>();
            }

            // Hook up Damage & Death feedback
            if (Stats != null)
            {
                Stats.OnDamageTaken += HandleDamageTaken;
                Stats.OnDeath += HandleDeath;
            }

#if ENABLE_INPUT_SYSTEM
            var es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es != null)
            {
                var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standalone != null)
                {
                    Destroy(standalone);
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
#endif
        }

        private void OnDestroy()
        {
            if (Stats != null)
            {
                Stats.OnDamageTaken -= HandleDamageTaken;
                Stats.OnDeath -= HandleDeath;
            }
        }

        private void Update()
        {
            StateMachine.Update();
            CheckInteraction();
        }

        private void FixedUpdate()
        {
            StateMachine.FixedUpdate();
        }

        private void InitImpulseSource()
        {
            var comp = GetComponent("CinemachineImpulseSource");
            if (comp != null)
            {
                _impulseSourceComponent = comp;
                _generateImpulseMethod = comp.GetType().GetMethod("GenerateImpulse", new[] { typeof(float) })
                                      ?? comp.GetType().GetMethod("GenerateImpulse", Type.EmptyTypes);
            }
        }

        // ================= Input System Readers =================

        public Vector2 GetMoveInput()
        {
            Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            }

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > move.sqrMagnitude) move = stick;
            }
#else
            move.x = Input.GetAxisRaw("Horizontal");
            move.y = Input.GetAxisRaw("Vertical");
#endif

            if (move.sqrMagnitude > 1f) move.Normalize();
            return move;
        }

        public bool IsAttackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && (pad.rightTrigger.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public bool IsBlockHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed) return true;

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && pad.leftTrigger.isPressed) return true;
            return false;
#else
            return Input.GetMouseButton(1);
#endif
        }

        public bool IsJumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public bool IsSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && pad.leftStickButton.isPressed) return true;
            return false;
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        public bool IsDodgePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.leftCtrlKey.wasPressedThisFrame || kb.leftAltKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) return true;

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && pad.buttonEast.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.F);
#endif
        }

        public bool IsInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) return true;

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && pad.buttonNorth.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        // ================= Animator Events & Notifiers =================

        public void NotifySpeedChanged(float speed)
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                try { Animator.SetFloat(SpeedHash, speed); } catch { }
            }
            OnSpeedChanged?.Invoke(speed);
        }

        public void NotifyAttackTriggered(int index)
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                try
                {
                    Animator.SetInteger(AttackIndexHash, index);
                    if (index > 0)
                    {
                        Animator.SetTrigger(AttackTriggerHash);
                    }
                }
                catch { }
            }
            OnAttackTriggered?.Invoke(index);
        }

        public void NotifyBlockChanged(bool isBlocking)
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                try { Animator.SetBool(IsBlockingHash, isBlocking); } catch { }
            }
            OnBlockChanged?.Invoke(isBlocking);
        }

        public void NotifyHurtTriggered()
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                try { Animator.SetTrigger(HitTriggerHash); } catch { }
            }
            OnHurtTriggered?.Invoke();
        }

        public void NotifyDeathTriggered()
        {
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                try { Animator.SetTrigger(DieTriggerHash); } catch { }
            }
            OnDeathTriggered?.Invoke();
        }

        // ================= Locomotion Methods =================

        public void ExecuteMovement(Vector2 input, bool isSprinting)
        {
            if (MainCamera == null) MainCamera = Camera.main;
            if (MainCamera == null) return;

            Vector3 camFwd = MainCamera.transform.forward;
            Vector3 camRight = MainCamera.transform.right;
            camFwd.y = 0f;
            camRight.y = 0f;
            camFwd.Normalize();
            camRight.Normalize();

            Vector3 worldDirection = (camFwd * input.y + camRight * input.x).normalized;

            bool sprintActive = isSprinting && input.sqrMagnitude > 0.01f;
            float currentSpeed = (Stats != null ? Stats.MoveSpeed : walkSpeed) * (sprintActive ? (Stats != null ? Stats.SprintMultiplier : 1.4f) : 1f);

            if (sprintActive && Stats != null)
            {
                Stats.ConsumeStamina(10f * Time.deltaTime);
            }

            Controller.Move(worldDirection * (currentSpeed * Time.deltaTime));

            if (worldDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(worldDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        public void ExecuteGuardStrafing(Vector2 input)
        {
            if (MainCamera == null) MainCamera = Camera.main;
            if (MainCamera == null) return;

            Vector3 camFwd = MainCamera.transform.forward;
            Vector3 camRight = MainCamera.transform.right;
            camFwd.y = 0f;
            camRight.y = 0f;
            camFwd.Normalize();
            camRight.Normalize();

            Vector3 worldDirection = (camFwd * input.y + camRight * input.x).normalized;
            float guardSpeed = (Stats != null ? Stats.MoveSpeed : walkSpeed) * 0.45f; // Strafe tático com escudo levantado

            Controller.Move(worldDirection * (guardSpeed * Time.deltaTime));

            // Permanece voltado para a mira da câmera enquanto defende
            if (camFwd.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camFwd, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * 1.5f * Time.deltaTime);
            }
        }

        public void ExecuteJump()
        {
            if (Stats == null || Stats.ConsumeStamina(jumpStaminaCost))
            {
                _velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
            }
        }

        public void ApplyGravityAndVerticalMovement()
        {
            if (Controller.isGrounded && _velocity.y < 0f)
            {
                _velocity.y = -2f;
            }
            else
            {
                _velocity.y -= gravity * Time.deltaTime;
            }

            Controller.Move(_velocity * Time.deltaTime);
        }

        public Vector3 CalculateDodgeDirection(Vector2 input)
        {
            if (MainCamera == null) MainCamera = Camera.main;

            if (input.sqrMagnitude > 0.01f && MainCamera != null)
            {
                Vector3 camFwd = MainCamera.transform.forward;
                Vector3 camRight = MainCamera.transform.right;
                camFwd.y = 0f;
                camRight.y = 0f;
                return (camFwd.normalized * input.y + camRight.normalized * input.x).normalized;
            }

            return transform.forward;
        }

        public void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        public bool CanDodge()
        {
            return Time.time >= _lastDodgeEndTime + dodgeCooldown && (Stats == null || Stats.CurrentStamina >= dodgeStaminaCost);
        }

        public void SetLastDodgeTime(float time)
        {
            _lastDodgeEndTime = time;
        }

        public void TriggerCardDodgeEffects()
        {
            if (Stats == null) return;

            foreach (var card in Stats.CollectedCards)
            {
                if (card != null && card.ShockwaveOnDodge)
                {
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, card.ShockwaveRadius);
                    foreach (var col in hitColliders)
                    {
                        if (col.gameObject == gameObject) continue;
                        if (col.TryGetComponent<IDamageable>(out var target))
                        {
                            Vector3 dir = (col.transform.position - transform.position).normalized;
                            target.TakeDamage(new DamageData(card.ShockwaveDamage, col.transform.position, dir, 8f, gameObject));
                        }
                    }

                    GenerateImpulseShake(0.25f);
                }
            }
        }

        // ================= Interaction Check =================

        private void CheckInteraction()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 1.0f, interactionRadius, interactableLayers, QueryTriggerInteraction.Collide);
            IInteractable found = null;

            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<IInteractable>(out var interactable) ||
                    col.GetComponentInParent<IInteractable>() is { } parentInteractable && (interactable = parentInteractable) != null)
                {
                    if (interactable.CanInteract)
                    {
                        found = interactable;
                        break;
                    }
                }
            }

            if (found != CurrentInteractable)
            {
                CurrentInteractable = found;
                OnInteractableFoundEvent?.Invoke(CurrentInteractable);
            }
        }

        public void TryInteract()
        {
            if (CurrentInteractable != null && CurrentInteractable.CanInteract)
            {
                CurrentInteractable.Interact(gameObject);
            }
        }

        // ================= Screen Shake & Impulse =================

        public void GenerateImpulseShake(float intensity)
        {
            // Cinemachine Impulse Source
            if (_impulseSourceComponent != null && _generateImpulseMethod != null)
            {
                try
                {
                    var parameters = _generateImpulseMethod.GetParameters();
                    if (parameters.Length == 1)
                    {
                        _generateImpulseMethod.Invoke(_impulseSourceComponent, new object[] { intensity });
                    }
                    else
                    {
                        _generateImpulseMethod.Invoke(_impulseSourceComponent, null);
                    }
                }
                catch { }
            }

            // Built-in Camera Shake Fallback
            if (ThirdPersonCameraController.Instance != null)
            {
                ThirdPersonCameraController.Instance.AddShake(intensity);
            }
        }

        // ================= Combat & Damage Triggers =================

        public void TriggerShieldBlockFeedback(DamageData data)
        {
            GenerateImpulseShake(0.15f);
            Combat?.OnShieldBlockEvent?.Invoke();
        }

        public void TriggerHurt(DamageData data)
        {
            if (StateMachine.CurrentState != DeadState && StateMachine.CurrentState != DodgeState)
            {
                StateMachine.ChangeState(HurtState);
            }
        }

        public void TriggerDeath()
        {
            StateMachine.ChangeState(DeadState);
        }

        private void HandleDamageTaken(DamageInfo info)
        {
            OnDamageTakenEvent?.Invoke(info);

            if (Health == null)
            {
                if (!IsBlocking && StateMachine.CurrentState != DodgeState && StateMachine.CurrentState != DeadState)
                {
                    StateMachine.ChangeState(HurtState);
                }
            }
        }

        private void HandleDeath()
        {
            if (Health == null)
            {
                StateMachine.ChangeState(DeadState);
            }
        }
    }
}
