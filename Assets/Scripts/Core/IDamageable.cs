using UnityEngine;

namespace NoLightBelow.Core
{
    /// <summary>
    /// Dados completos de um impacto de dano no sistema de combate.
    /// Contém quantidade de dano, ponto de impacto, normal/direção, knockback, se foi bloqueado e origem.
    /// </summary>
    [System.Serializable]
    public struct DamageData
    {
        public float Amount;
        public Vector3 ImpactPoint;
        public Vector3 ImpactNormal;
        public float KnockbackForce;
        public bool IsBlocked;
        public GameObject Source;
        public bool IsCritical;

        public DamageData(float amount, Vector3 impactPoint, Vector3 impactNormal, float knockback = 0f, bool isBlocked = false, GameObject source = null, bool isCrit = false)
        {
            Amount = amount;
            ImpactPoint = impactPoint;
            ImpactNormal = impactNormal;
            KnockbackForce = knockback;
            IsBlocked = isBlocked;
            Source = source;
            IsCritical = isCrit;
        }

        // Conversões implícitas bidirecionais entre DamageData e DamageInfo para compatibilidade universal
        public static implicit operator DamageData(DamageInfo info)
        {
            return new DamageData(info.Amount, info.HitPoint, info.Direction, info.KnockbackForce, false, info.Source, info.IsCritical);
        }

        public static implicit operator DamageInfo(DamageData data)
        {
            return new DamageInfo(data.Amount, DamageType.PhysicalSlash, data.ImpactPoint, data.ImpactNormal, data.KnockbackForce, data.Source, data.IsCritical);
        }
    }

    /// <summary>
    /// Interface padrão para todas as entidades suscetíveis a dano (Player, Inimigos, Boneco de Treino).
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(DamageData data);
        void TakeDamage(DamageInfo damageInfo);
        bool IsDead { get; }
        Transform Transform { get; }
    }
}
