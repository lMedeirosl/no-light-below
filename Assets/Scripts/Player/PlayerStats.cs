using System;
using System.Collections.Generic;
using UnityEngine;
using NoLightBelow.Core;

namespace NoLightBelow.Cards
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Base Attributes")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseMaxStamina = 100f;
        [SerializeField] private float baseMoveSpeed = 6.5f;
        [SerializeField] private float baseSprintMultiplier = 1.45f;
        [SerializeField] private float baseAttackPower = 25f;
        [SerializeField] private float baseAttackSpeed = 1.0f; // Multiplier
        [SerializeField] private float baseCritChance = 0.05f; // 5%
        [SerializeField] private float baseCritMultiplier = 1.5f;
        [SerializeField] private float baseDamageReduction = 0f;

        [Header("Stamina Regeneration")]
        [SerializeField] private float staminaRegenRate = 28f;
        [SerializeField] private float staminaRegenDelay = 0.8f;

        // Current Run Values
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public float CurrentStamina { get; private set; }
        public float MaxStamina { get; private set; }
        public float MoveSpeed { get; private set; }
        public float SprintMultiplier => baseSprintMultiplier;
        public float AttackPower { get; private set; }
        public float AttackSpeedMultiplier { get; private set; }
        public float CriticalChance { get; private set; }
        public float CriticalMultiplier { get; private set; }
        public float DamageReduction { get; private set; }
        public float LifeStealPercent { get; private set; }
        public float BleedChance { get; private set; }

        public bool IsDead => CurrentHealth <= 0f;
        public Transform Transform => transform;
        public bool IsInvulnerable { get; set; }

        private float _lastStaminaUseTime;
        private readonly List<CardData> _collectedCards = new List<CardData>();
        public IReadOnlyList<CardData> CollectedCards => _collectedCards;

        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<CardData> OnCardAcquired;
        public event Action OnDeath;
        public event Action<DamageInfo> OnDamageTaken;

        private void Awake()
        {
            RecalculateStats();
            CurrentHealth = MaxHealth;
            CurrentStamina = MaxStamina;
        }

        private void Update()
        {
            RegenerateStamina();
        }

        public void RecalculateStats()
        {
            float bonusHealthFlat = 0f;
            float bonusHealthPct = 0f;
            float bonusStaminaFlat = 0f;
            float bonusStaminaPct = 0f;
            float bonusSpeedPct = 0f;
            float bonusAttackFlat = 0f;
            float bonusAttackPct = 0f;
            float bonusAtkSpeedPct = 0f;
            float bonusCritFlat = 0f;
            float bonusCritDmgPct = 0f;
            float bonusArmorFlat = 0f;
            float lifesteal = 0f;
            float bleed = 0f;

            foreach (var card in _collectedCards)
            {
                if (card == null) continue;

                if (card.TriggersLifestealOnHit) lifesteal += card.LifestealPercent;
                if (card.TriggersBleedOnHit) bleed += card.BleedChance;

                foreach (var mod in card.Modifiers)
                {
                    switch (mod.TargetStat)
                    {
                        case StatType.MaxHealth:
                            bonusHealthFlat += mod.FlatValue;
                            bonusHealthPct += mod.PercentMultiplier;
                            break;
                        case StatType.MaxStamina:
                            bonusStaminaFlat += mod.FlatValue;
                            bonusStaminaPct += mod.PercentMultiplier;
                            break;
                        case StatType.MoveSpeed:
                            bonusSpeedPct += mod.PercentMultiplier;
                            break;
                        case StatType.AttackPower:
                            bonusAttackFlat += mod.FlatValue;
                            bonusAttackPct += mod.PercentMultiplier;
                            break;
                        case StatType.AttackSpeed:
                            bonusAtkSpeedPct += mod.PercentMultiplier;
                            break;
                        case StatType.CriticalChance:
                            bonusCritFlat += mod.FlatValue + mod.PercentMultiplier;
                            break;
                        case StatType.CriticalDamage:
                            bonusCritDmgPct += mod.PercentMultiplier;
                            break;
                        case StatType.ArmorReduction:
                            bonusArmorFlat += mod.FlatValue + mod.PercentMultiplier;
                            break;
                    }
                }
            }

            float prevMaxHp = MaxHealth;
            MaxHealth = Mathf.Max(10f, (baseMaxHealth + bonusHealthFlat) * (1f + bonusHealthPct));
            if (prevMaxHp > 0)
            {
                // Preserve health ratio on max hp change
                CurrentHealth = Mathf.Clamp(CurrentHealth + (MaxHealth - prevMaxHp), 1f, MaxHealth);
            }

            MaxStamina = Mathf.Max(10f, (baseMaxStamina + bonusStaminaFlat) * (1f + bonusStaminaPct));
            MoveSpeed = Mathf.Max(2f, baseMoveSpeed * (1f + bonusSpeedPct));
            AttackPower = Mathf.Max(1f, (baseAttackPower + bonusAttackFlat) * (1f + bonusAttackPct));
            AttackSpeedMultiplier = Mathf.Clamp(baseAttackSpeed * (1f + bonusAtkSpeedPct), 0.3f, 3.5f);
            CriticalChance = Mathf.Clamp01(baseCritChance + bonusCritFlat);
            CriticalMultiplier = Mathf.Max(1.1f, baseCritMultiplier * (1f + bonusCritDmgPct));
            DamageReduction = Mathf.Clamp(baseDamageReduction + bonusArmorFlat, 0f, 0.75f);
            LifeStealPercent = Mathf.Clamp(lifesteal, 0f, 0.5f);
            BleedChance = Mathf.Clamp01(bleed);

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        }

        public void AddCard(CardData card)
        {
            if (card == null) return;
            _collectedCards.Add(card);
            RecalculateStats();
            OnCardAcquired?.Invoke(card);
            Debug.Log($"<color=#D4AF37>[No Light Below]</color> Carta Adquirida: <b>{card.CardTitle}</b> ({card.Rarity})");
        }

        public bool ConsumeStamina(float amount)
        {
            if (CurrentStamina < amount) return false;

            CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
            _lastStaminaUseTime = Time.time;
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
            return true;
        }

        private void RegenerateStamina()
        {
            if (Time.time < _lastStaminaUseTime + staminaRegenDelay) return;
            if (CurrentStamina >= MaxStamina) return;

            CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + staminaRegenRate * Time.deltaTime);
            OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        }

        public bool IsBlocking { get; set; }

        public void TakeDamage(DamageData data)
        {
            TakeDamage((DamageInfo)data);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (IsDead || IsInvulnerable) return;

            float netDamage = damageInfo.Amount * (1f - DamageReduction);

            // Shield Block Check
            if (IsBlocking)
            {
                // Attacker is roughly in front (dot product < 0 because direction points from attacker towards player)
                bool isBlocked = Vector3.Dot(transform.forward, damageInfo.Direction) < 0.2f;
                if (isBlocked)
                {
                    float blockCost = Mathf.Max(8f, damageInfo.Amount * 0.45f);
                    ConsumeStamina(blockCost);

                    // Shield absorbs 85% of damage
                    netDamage *= 0.15f;
                    Debug.Log($"<color=#4682B4>[Escudo]</color> Golpe bloqueado com sucesso! Dano mitigado: {(damageInfo.Amount - netDamage):0.#}");

                    if (TryGetComponent<Combat.PlayerCombat>(out var combat))
                    {
                        combat.OnShieldBlockEvent?.Invoke();
                    }
                    
                    if (Player.ThirdPersonCameraController.Instance != null)
                    {
                        Player.ThirdPersonCameraController.Instance.AddShake(0.12f);
                    }
                }
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - netDamage);

            OnDamageTaken?.Invoke(damageInfo);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth <= 0f)
            {
                OnDeath?.Invoke();
                Debug.Log("<color=#8B0000>[No Light Below]</color> O jogador sucumbiu à escuridão da dungeon.");
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public DamageInfo CalculateOutgoingDamage(DamageType type, Vector3 hitPoint, Vector3 direction)
        {
            bool isCrit = UnityEngine.Random.value < CriticalChance;
            float damage = AttackPower * (isCrit ? CriticalMultiplier : 1f);

            // Low health conditional buff
            if (CurrentHealth / MaxHealth <= 0.35f)
            {
                foreach (var card in _collectedCards)
                {
                    if (card != null && card.BonusDamageOnLowHealth)
                    {
                        damage *= card.LowHealthDamageMultiplier;
                    }
                }
            }

            return new DamageInfo(damage, type, hitPoint, direction, 6f, gameObject, isCrit);
        }
    }
}
