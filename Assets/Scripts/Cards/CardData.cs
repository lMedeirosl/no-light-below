using System.Collections.Generic;
using UnityEngine;

namespace NoLightBelow.Cards
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "No Light Below/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Visual & Identity")]
        public string CardId;
        public string CardTitle;
        [TextArea(2, 3)]
        public string Description;
        [TextArea(2, 2)]
        public string LoreQuote;
        public CardRarity Rarity;
        public CardCategory Category;
        public Sprite Artwork;
        public Sprite CustomFrame;

        [Header("Stat Modifiers")]
        public List<StatModifier> Modifiers = new List<StatModifier>();

        [Header("Passive Gameplay Triggers")]
        public bool TriggersBleedOnHit;
        [Range(0f, 1f)] public float BleedChance = 0.2f;
        public float BleedDamagePerSecond = 5f;

        public bool TriggersLifestealOnHit;
        [Range(0f, 1f)] public float LifestealPercent = 0.05f;

        public bool ShockwaveOnDodge;
        public float ShockwaveDamage = 15f;
        public float ShockwaveRadius = 3.5f;

        public bool BonusDamageOnLowHealth;
        public float LowHealthThreshold = 0.35f;
        public float LowHealthDamageMultiplier = 1.4f;

        public Color GetRarityColor()
        {
            return Rarity switch
            {
                CardRarity.Common => new Color(0.72f, 0.70f, 0.65f),      // Rusty Steel
                CardRarity.Rare => new Color(0.35f, 0.65f, 0.95f),        // Cool Mystic Blue
                CardRarity.Epic => new Color(0.85f, 0.65f, 0.20f),        // Ancient Gold / Amber
                CardRarity.Cursed => new Color(0.85f, 0.15f, 0.25f),      // Blood Crimson
                _ => Color.white
            };
        }

        public string BuildFormattedDescription()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var mod in Modifiers)
            {
                string sign = mod.PercentMultiplier >= 0 ? "+" : "";
                string statName = GetReadableStatName(mod.TargetStat);

                if (Mathf.Abs(mod.PercentMultiplier) > 0.001f)
                {
                    sb.AppendLine($"{sign}{mod.PercentMultiplier * 100f:0.#}% {statName}");
                }
                else if (Mathf.Abs(mod.FlatValue) > 0.001f)
                {
                    string flatSign = mod.FlatValue >= 0 ? "+" : "";
                    sb.AppendLine($"{flatSign}{mod.FlatValue:0.#} {statName}");
                }
            }

            if (TriggersBleedOnHit)
                sb.AppendLine($"• {BleedChance * 100f:0}% de chance de causar Sangramento.");

            if (TriggersLifestealOnHit)
                sb.AppendLine($"• Recupera {LifestealPercent * 100f:0}% do dano causado como vida.");

            if (ShockwaveOnDodge)
                sb.AppendLine($"• Esquiva libera uma onda de choque que repele inimigos.");

            if (BonusDamageOnLowHealth)
                sb.AppendLine($"• +{(LowHealthDamageMultiplier - 1f) * 100f:0}% de dano ao estar com menos de {LowHealthThreshold * 100f:0}% de vida.");

            if (!string.IsNullOrEmpty(Description))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(Description);
            }

            return sb.ToString().TrimEnd();
        }

        private string GetReadableStatName(StatType stat)
        {
            return stat switch
            {
                StatType.MaxHealth => "Vida Máxima",
                StatType.MaxStamina => "Estamina Máxima",
                StatType.MoveSpeed => "Velocidade de Movimento",
                StatType.AttackPower => "Poder de Ataque",
                StatType.AttackSpeed => "Velocidade de Ataque",
                StatType.CriticalChance => "Chance Crítica",
                StatType.CriticalDamage => "Dano Crítico",
                StatType.ArmorReduction => "Redução de Dano",
                StatType.DodgeInvulnerabilityTime => "Tempo de Invulnerabilidade",
                StatType.LifeStealPercent => "Roubo de Vida",
                StatType.BleedChance => "Chance de Sangrar",
                _ => stat.ToString()
            };
        }
    }
}
