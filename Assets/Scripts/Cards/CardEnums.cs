using UnityEngine;

namespace NoLightBelow.Cards
{
    public enum CardRarity
    {
        Common,      // Rusty Iron / Wood border
        Rare,        // Reinforced Bronze / Silver border
        Epic,        // Obsidian / Gold border
        Cursed       // Dark Blood / Demonic rune border (High risk, high reward)
    }

    public enum CardCategory
    {
        Offensive,   // Weapons, blade coatings, bleed, damage
        Defensive,   // Armor, parry, bone shielding, health
        Agility,     // Dash distance, attack speed, movement speed
        Occult       // Dungeon-specific passive curses, void powers, lifesteal
    }

    public enum StatType
    {
        MaxHealth,
        MaxStamina,
        MoveSpeed,
        AttackPower,
        AttackSpeed,
        CriticalChance,
        CriticalDamage,
        ArmorReduction,
        DodgeInvulnerabilityTime,
        LifeStealPercent,
        BleedChance
    }

    [System.Serializable]
    public struct StatModifier
    {
        public StatType TargetStat;
        public float FlatValue;
        public float PercentMultiplier; // e.g. 0.15 = +15%
    }
}
