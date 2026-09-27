using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using NoLightBelow.Core;
using NoLightBelow.Combat;
using NoLightBelow.Player;

namespace NoLightBelow.Enemies
{
    [RequireComponent(typeof(Collider))]
    public class CryptSkeletonAI : MonoBehaviour, IDamageable
    {
        public enum AIState { Idle, Chase, Telegraph, Attack, Recovery, Stagger, Dead }

        [Header("Stats")]
        [SerializeField] private float maxHealth = 65f;
        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float detectionRadius = 18f;
        [SerializeField] private float attackRange = 2.1f;
        [SerializeField] private float attackDamage = 18f;
        [SerializeField] private float telegraphDuration = 0.55f;
        [SerializeField] private float attackCooldown = 1.2f;

        [Header("Visual References")]
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private Renderer[] boneRenderers;
        [SerializeField] private Light eyeGlowLight;

        private float _currentHealth;
        private AIState _state = AIState.Idle;
        private Transform _playerTransform;
        private NavMeshAgent _navAgent;
        private float _lastAttackTime;
        private Quaternion _originalWeaponRot;
        private bool _isDead;

        public bool IsDead => _isDead;
        public Transform Transform => transform;
        public AIState CurrentState => _state;

        public event Action<CryptSkeletonAI> OnSkeletonDied;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _navAgent = GetComponent<NavMeshAgent>();
            if (weaponPivot != null)
            {
                _originalWeaponRot = weaponPivot.localRotation;
            }
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _playerTransform = player.transform;
            }

            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.speed = moveSpeed;
                _navAgent.stoppingDistance = attackRange * 0.85f;
            }
        }

        private void Update()
        {
            if (_isDead || _playerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            switch (_state)
            {
                case AIState.Idle:
                    if (distanceToPlayer <= detectionRadius)
                    {
                        _state = AIState.Chase;
                    }
                    break;

                case AIState.Chase:
                    HandleChase(distanceToPlayer);
                    break;

                case AIState.Telegraph:
                case AIState.Attack:
                case AIState.Recovery:
                case AIState.Stagger:
                    // Handled in coroutines
                    break;
            }
        }

        private void HandleChase(float distanceToPlayer)
        {
            // Face player
            Vector3 lookDir = (_playerTransform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }

            // In attack range and ready
            if (distanceToPlayer <= attackRange && Time.time >= _lastAttackTime + attackCooldown)
            {
                if (_navAgent != null && _navAgent.isOnNavMesh)
                {
                    _navAgent.isStopped = true;
                }
                StartCoroutine(AttackSequenceRoutine());
                return;
            }

            // Move towards player
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(_playerTransform.position);
            }
            else
            {
                // Fallback direct translation if NavMesh is baking/offline
                if (distanceToPlayer > attackRange * 0.9f)
                {
                    transform.position += lookDir * (moveSpeed * Time.deltaTime);
                }
            }
        }

        private IEnumerator AttackSequenceRoutine()
        {
            _state = AIState.Telegraph;

            // Telegraph: raise rusted sword high, intensify spectral red eye glow
            if (eyeGlowLight != null) eyeGlowLight.intensity = 2.8f;

            Quaternion telegraphRot = _originalWeaponRot * Quaternion.Euler(-75f, 0f, 0f);
            float elapsed = 0f;

            while (elapsed < telegraphDuration)
            {
                float t = elapsed / telegraphDuration;
                if (weaponPivot != null)
                {
                    weaponPivot.localRotation = Quaternion.Slerp(_originalWeaponRot, telegraphRot, t);
                }

                // Still subtly track player while raising sword
                if (_playerTransform != null)
                {
                    Vector3 aim = (_playerTransform.position - transform.position).normalized;
                    aim.y = 0f;
                    if (aim.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aim), Time.deltaTime * 6f);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Attack Strike: fast downward cleave
            _state = AIState.Attack;
            Quaternion swingRot = _originalWeaponRot * Quaternion.Euler(45f, 0f, 0f);
            float strikeDuration = 0.14f;
            elapsed = 0f;

            while (elapsed < strikeDuration)
            {
                float t = elapsed / strikeDuration;
                if (weaponPivot != null)
                {
                    weaponPivot.localRotation = Quaternion.Slerp(telegraphRot, swingRot, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Deal damage check in forward cone
            ExecuteDamageCheck();

            // Recovery return
            _state = AIState.Recovery;
            if (eyeGlowLight != null) eyeGlowLight.intensity = 0.8f;

            elapsed = 0f;
            float recoverDuration = 0.45f;
            while (elapsed < recoverDuration)
            {
                float t = elapsed / recoverDuration;
                if (weaponPivot != null)
                {
                    weaponPivot.localRotation = Quaternion.Slerp(swingRot, _originalWeaponRot, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (weaponPivot != null) weaponPivot.localRotation = _originalWeaponRot;
            _lastAttackTime = Time.time;
            _state = AIState.Chase;
        }

        private void ExecuteDamageCheck()
        {
            if (_playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            Vector3 toPlayer = (_playerTransform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toPlayer);

            if (distance <= attackRange * 1.3f && angle <= 70f)
            {
                if (_playerTransform.TryGetComponent<IDamageable>(out var playerDamageable))
                {
                    Vector3 hitPoint = _playerTransform.position + Vector3.up * 1.0f;
                    Vector3 dir = transform.forward;
                    playerDamageable.TakeDamage(new DamageInfo(attackDamage, DamageType.PhysicalSlash, hitPoint, dir, 5f, gameObject));
                }
            }
        }

        public void TakeDamage(DamageData data)
        {
            TakeDamage((DamageInfo)data);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (_isDead) return;

            _currentHealth -= damageInfo.Amount;

            // Spawn floating damage text
            SpawnDamageText(damageInfo);

            // Flash bones red
            StartCoroutine(FlashRoutine());

            // Knockback
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.velocity = damageInfo.Direction * damageInfo.KnockbackForce;
            }
            else
            {
                transform.position += damageInfo.Direction * (damageInfo.KnockbackForce * 0.1f);
            }

            if (_currentHealth <= 0f)
            {
                Die();
            }
            else if (_state != AIState.Attack && _state != AIState.Telegraph)
            {
                StartCoroutine(StaggerRoutine());
            }
        }

        private IEnumerator StaggerRoutine()
        {
            _state = AIState.Stagger;
            yield return new WaitForSeconds(0.2f);
            if (!_isDead) _state = AIState.Chase;
        }

        private IEnumerator FlashRoutine()
        {
            if (boneRenderers != null)
            {
                foreach (var r in boneRenderers)
                {
                    if (r != null) r.material.color = new Color(0.9f, 0.2f, 0.2f);
                }
                yield return new WaitForSeconds(0.12f);
                foreach (var r in boneRenderers)
                {
                    if (r != null) r.material.color = new Color(0.72f, 0.70f, 0.62f);
                }
            }
        }

        private void SpawnDamageText(DamageInfo info)
        {
            GameObject textObj = new GameObject("DamageText_Skeleton");
            textObj.transform.position = transform.position + Vector3.up * 1.9f + UnityEngine.Random.insideUnitSphere * 0.2f;

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = info.IsCritical ? $"{info.Amount:0}!" : $"{info.Amount:0}";
            textMesh.characterSize = info.IsCritical ? 0.28f : 0.2f;
            textMesh.fontSize = 48;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = info.IsCritical ? new Color(1f, 0.85f, 0.1f) : Color.white;

            textObj.AddComponent<FloatingDamageNumber>();
        }

        private void Die()
        {
            _isDead = true;
            _state = AIState.Dead;

            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
                _navAgent.enabled = false;
            }

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (eyeGlowLight != null) eyeGlowLight.enabled = false;

            // Physical bone collapse
            StartCoroutine(DeathCollapseRoutine());
            OnSkeletonDied?.Invoke(this);
            Debug.Log("<color=#DC143C>[Inimigo]</color> Esqueleto da Cripta foi destruído!");
        }

        private IEnumerator DeathCollapseRoutine()
        {
            // Drop weapon and sink into the dungeon floor
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < 1.4f)
            {
                transform.position = startPos - new Vector3(0f, elapsed * 0.8f, 0f);
                transform.Rotate(Vector3.right, 35f * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        public void SetComponents(Transform weapon, Renderer[] renderers, Light eyeLight)
        {
            weaponPivot = weapon;
            boneRenderers = renderers;
            eyeGlowLight = eyeLight;
            if (weaponPivot != null) _originalWeaponRot = weaponPivot.localRotation;
        }
    }
}
