using UnityEngine;

namespace NoLightBelow.Core
{
    public enum DamageType
    {
        PhysicalSlash,
        PhysicalBlunt,
        PhysicalPierce,
        Fire,
        Void,
        Bleed
    }

    [System.Serializable]
    public struct DamageInfo
    {
        public float Amount;
        public DamageType Type;
        public Vector3 HitPoint;
        public Vector3 Direction;
        public float KnockbackForce;
        public GameObject Source;
        public bool IsCritical;

        public DamageInfo(float amount, DamageType type, Vector3 hitPoint, Vector3 direction, float knockback = 0f, GameObject source = null, bool isCrit = false)
        {
            Amount = amount;
            Type = type;
            HitPoint = hitPoint;
            Direction = direction;
            KnockbackForce = knockback;
            Source = source;
            IsCritical = isCrit;
        }
    }
}
