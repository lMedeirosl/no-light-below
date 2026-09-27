using System.Collections.Generic;
using UnityEngine;
using NoLightBelow.Core;
using NoLightBelow.Cards;

namespace NoLightBelow.Combat
{
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private DamageType damageType = DamageType.PhysicalSlash;
        [SerializeField] private float baseKnockback = 5f;

        private Collider _collider;
        private PlayerStats _ownerStats;
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private bool _isActive;

        public event System.Action<IDamageable, DamageInfo> OnHitConfirmed;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        public void Initialize(PlayerStats ownerStats)
        {
            _ownerStats = ownerStats;
        }

        public void EnableHitbox()
        {
            _hitTargets.Clear();
            _collider.enabled = true;
            _isActive = true;
        }

        public void DisableHitbox()
        {
            _collider.enabled = false;
            _isActive = false;
            _hitTargets.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            // Check layer
            if (((1 << other.gameObject.layer) & targetLayers) == 0 && targetLayers != 0) return;

            // Avoid hitting self
            if (_ownerStats != null && other.gameObject == _ownerStats.gameObject) return;

            if (other.TryGetComponent<IDamageable>(out var damageable) ||
                other.GetComponentInParent<IDamageable>() is { } parentDamageable && (damageable = parentDamageable) != null)
            {
                if (_hitTargets.Contains(damageable)) return;
                _hitTargets.Add(damageable);

                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 direction = (other.transform.position - transform.position).normalized;
                if (direction == Vector3.zero) direction = transform.forward;

                DamageInfo damageInfo;
                if (_ownerStats != null)
                {
                    damageInfo = _ownerStats.CalculateOutgoingDamage(damageType, hitPoint, direction);
                    damageInfo.KnockbackForce = baseKnockback;

                    // Apply Lifesteal
                    if (_ownerStats.LifeStealPercent > 0f)
                    {
                        _ownerStats.Heal(damageInfo.Amount * _ownerStats.LifeStealPercent);
                    }
                }
                else
                {
                    damageInfo = new DamageInfo(20f, damageType, hitPoint, direction, baseKnockback, gameObject);
                }

                damageable.TakeDamage(damageInfo);
                OnHitConfirmed?.Invoke(damageable, damageInfo);
            }
        }
    }
}
