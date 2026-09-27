using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using NoLightBelow.Cards;
using NoLightBelow.Core;
using NoLightBelow.Player;

namespace NoLightBelow.Combat
{
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Weapon Transforms")]
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private Hitbox weaponHitbox;

        [Header("Shield Configuration")]
        [SerializeField] private Transform shieldPivot;
        [SerializeField] private Vector3 shieldRestLocalPos = new Vector3(-0.45f, 0.95f, 0.1f);
        [SerializeField] private Vector3 shieldBlockLocalPos = new Vector3(-0.05f, 1.15f, 0.45f);
        [SerializeField] private Vector3 shieldRestLocalEuler = new Vector3(0f, 15f, 15f);
        [SerializeField] private Vector3 shieldBlockLocalEuler = new Vector3(10f, 45f, -10f);

        [Header("Combo Configuration")]
        [SerializeField] private float comboResetDelay = 1.1f;
        [SerializeField] private float baseAttackDuration = 0.32f;
        [SerializeField] private float attackStaminaCost = 12f;

        [Header("Crossbow / Ranged Shot (Optional)")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private float rangedStaminaCost = 18f;
        [SerializeField] private float rangedCooldown = 0.65f;

        [Header("Audio / Visual Feedback")]
        [SerializeField] private float hitStopDuration = 0.05f;
        [SerializeField] private float screenShakePerHit = 0.16f;

        [Header("Unity Events for Audio & VFX")]
        public UnityEvent<DamageInfo> OnHitLandedEvent;
        public UnityEvent OnShieldBlockEvent;

        private PlayerStats _stats;
        private int _currentComboStep = 0;
        private float _lastAttackTime;
        private bool _isAttacking;
        private float _lastRangedTime;
        private Quaternion _originalPivotLocalRotation;
        private bool _isShieldGuarding;
        private System.Action _onAttackComplete;
        private System.Action _currentOnHitCallback;
        private bool _hasBufferedAttack;

        public bool IsAttacking => _isAttacking;
        public bool IsShieldGuarding => _isShieldGuarding;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            if (weaponPivot != null)
            {
                _originalPivotLocalRotation = weaponPivot.localRotation;
            }
        }

        private void Start()
        {
            if (weaponHitbox != null && _stats != null)
            {
                weaponHitbox.Initialize(_stats);
                weaponHitbox.OnHitConfirmed += HandleHitConfirmed;
            }
        }

        private void OnDestroy()
        {
            if (weaponHitbox != null)
            {
                weaponHitbox.OnHitConfirmed -= HandleHitConfirmed;
            }
        }

        private void Update()
        {
            if (_stats != null && _stats.IsDead) return;

            // Reset combo if idle too long
            if (Time.time > _lastAttackTime + comboResetDelay)
            {
                _currentComboStep = 0;
            }

            // Smoothly interpolate shield position based on _isShieldGuarding
            if (shieldPivot != null)
            {
                Vector3 targetPos = _isShieldGuarding ? shieldBlockLocalPos : shieldRestLocalPos;
                Quaternion targetRot = Quaternion.Euler(_isShieldGuarding ? shieldBlockLocalEuler : shieldRestLocalEuler);

                shieldPivot.localPosition = Vector3.Lerp(shieldPivot.localPosition, targetPos, Time.deltaTime * 18f);
                shieldPivot.localRotation = Quaternion.Slerp(shieldPivot.localRotation, targetRot, Time.deltaTime * 18f);
            }
        }

        public void SetShieldGuard(bool isGuarding)
        {
            _isShieldGuarding = isGuarding;
            if (_stats != null) _stats.IsBlocking = isGuarding;
        }

        public void ExecuteComboAttack(System.Action onComplete)
        {
            _onAttackComplete = onComplete;
            _hasBufferedAttack = false;
            TryMeleeAttack();
        }

        public void ExecuteComboAttackIndex(int index, System.Action onHitCallback = null, System.Action onComplete = null)
        {
            if (_stats != null && !_stats.ConsumeStamina(attackStaminaCost)) return;

            _currentComboStep = Mathf.Clamp(index, 1, 3);
            _lastAttackTime = Time.time;
            _currentOnHitCallback = onHitCallback;
            _onAttackComplete = onComplete;
            StopAllCoroutines();
            StartCoroutine(ExecuteMeleeAttackRoutine(_currentComboStep, onHitCallback, onComplete));
        }

        public void BufferComboInput()
        {
            _hasBufferedAttack = true;
        }

        private void TryMeleeAttack()
        {
            if (_stats != null && !_stats.ConsumeStamina(attackStaminaCost)) return;

            _currentComboStep = (_currentComboStep % 3) + 1;
            _lastAttackTime = Time.time;
            StartCoroutine(ExecuteMeleeAttackRoutine(_currentComboStep, null, _onAttackComplete));
        }

        private IEnumerator ExecuteMeleeAttackRoutine(int comboStep, System.Action onHitCallback = null, System.Action onComplete = null)
        {
            _isAttacking = true;
            _currentOnHitCallback = onHitCallback;

            float speedMult = _stats != null ? _stats.AttackSpeedMultiplier : 1f;
            float duration = baseAttackDuration / speedMult;

            // Activate weapon hitbox
            if (weaponHitbox != null)
            {
                weaponHitbox.EnableHitbox();
            }

            // Procedural swing angles based on combo step
            Quaternion startRot;
            Quaternion midRot;
            Quaternion endRot;

            switch (comboStep)
            {
                case 1: // Horizontal Slash Right-to-Left
                    startRot = _originalPivotLocalRotation * Quaternion.Euler(15f, -65f, -20f);
                    midRot = _originalPivotLocalRotation * Quaternion.Euler(0f, 10f, 10f);
                    endRot = _originalPivotLocalRotation * Quaternion.Euler(-10f, 75f, 30f);
                    break;
                case 2: // Backhand Slash Left-to-Right
                    startRot = _originalPivotLocalRotation * Quaternion.Euler(-15f, 70f, 25f);
                    midRot = _originalPivotLocalRotation * Quaternion.Euler(0f, -10f, -10f);
                    endRot = _originalPivotLocalRotation * Quaternion.Euler(10f, -75f, -30f);
                    break;
                default: // Combo 3: Overhead Heavy Cleave
                    startRot = _originalPivotLocalRotation * Quaternion.Euler(-80f, 0f, 0f);
                    midRot = _originalPivotLocalRotation * Quaternion.Euler(15f, 0f, 0f);
                    endRot = _originalPivotLocalRotation * Quaternion.Euler(55f, 0f, 0f);
                    break;
            }

            // Forward swing
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (weaponPivot != null)
                {
                    weaponPivot.localRotation = t < 0.5f 
                        ? Quaternion.Slerp(startRot, midRot, t * 2f) 
                        : Quaternion.Slerp(midRot, endRot, (t - 0.5f) * 2f);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (weaponHitbox != null)
            {
                weaponHitbox.DisableHitbox();
            }

            // Recovery return
            float recoverElapsed = 0f;
            float recoverDuration = 0.15f / speedMult;
            Quaternion finalSwingRot = weaponPivot != null ? weaponPivot.localRotation : _originalPivotLocalRotation;

            while (recoverElapsed < recoverDuration)
            {
                float t = recoverElapsed / recoverDuration;
                if (weaponPivot != null)
                {
                    weaponPivot.localRotation = Quaternion.Slerp(finalSwingRot, _originalPivotLocalRotation, t);
                }
                recoverElapsed += Time.deltaTime;
                yield return null;
            }

            if (weaponPivot != null)
            {
                weaponPivot.localRotation = _originalPivotLocalRotation;
            }

            _isAttacking = false;
            _currentOnHitCallback = null;

            if (_hasBufferedAttack && _stats != null && _stats.ConsumeStamina(attackStaminaCost))
            {
                _hasBufferedAttack = false;
                _currentComboStep = (_currentComboStep % 3) + 1;
                _lastAttackTime = Time.time;
                StartCoroutine(ExecuteMeleeAttackRoutine(_currentComboStep, onHitCallback, onComplete));
            }
            else
            {
                _onAttackComplete?.Invoke();
                onComplete?.Invoke();
            }
        }

        private void TryRangedAttack()
        {
            if (Time.time < _lastRangedTime + rangedCooldown) return;
            if (!_stats.ConsumeStamina(rangedStaminaCost)) return;

            _lastRangedTime = Time.time;
            ShootRangedProjectile();
        }

        private void ShootRangedProjectile()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 100f) 
                ? hit.point 
                : ray.GetPoint(60f);

            Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position + transform.forward + Vector3.up * 1.2f;
            Vector3 shootDir = (targetPoint - spawnPos).normalized;

            // Spawn projectile
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bolt.name = "CrossbowBolt";
            bolt.transform.position = spawnPos;
            bolt.transform.rotation = Quaternion.LookRotation(shootDir) * Quaternion.Euler(90f, 0f, 0f);
            bolt.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);

            var rb = bolt.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearVelocity = shootDir * 38f;

            var projHitbox = bolt.AddComponent<Hitbox>();
            projHitbox.Initialize(_stats);
            projHitbox.EnableHitbox();

            // Destroy bolt after 4 seconds or on hit
            projHitbox.OnHitConfirmed += (target, info) =>
            {
                Destroy(bolt);
            };
            Destroy(bolt, 4f);

            if (ThirdPersonCameraController.Instance != null)
            {
                ThirdPersonCameraController.Instance.AddShake(0.08f);
            }
        }

        private void HandleHitConfirmed(IDamageable target, DamageInfo info)
        {
            _currentOnHitCallback?.Invoke();
            OnHitLandedEvent?.Invoke(info);

            if (ThirdPersonCameraController.Instance != null)
            {
                float shakeAmount = info.IsCritical ? screenShakePerHit * 1.8f : screenShakePerHit;
                ThirdPersonCameraController.Instance.AddShake(shakeAmount);
            }

            StartCoroutine(HitStopRoutine(hitStopDuration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1.0f;
        }

        public void SetWeaponComponents(Transform pivot, Hitbox hitbox, Transform spawnPoint)
        {
            SetCombatEquipment(pivot, hitbox, null, spawnPoint);
        }

        public void SetCombatEquipment(Transform pivot, Hitbox hitbox, Transform shield, Transform spawnPoint = null)
        {
            weaponPivot = pivot;
            weaponHitbox = hitbox;
            shieldPivot = shield;
            projectileSpawnPoint = spawnPoint;
            if (weaponPivot != null)
            {
                _originalPivotLocalRotation = weaponPivot.localRotation;
            }
            if (weaponHitbox != null && _stats != null)
            {
                weaponHitbox.Initialize(_stats);
                weaponHitbox.OnHitConfirmed += HandleHitConfirmed;
            }
        }
    }
}
