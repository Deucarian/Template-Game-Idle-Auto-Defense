
namespace Deucarian.TemplateGameIdleAutoDefense
{
    public enum IdleAutoDefenseRewardRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum IdleAutoDefenseRewardDraftKind
    {
        LevelUp = 0,
        WaveComplete = 1,
        EliteDefeated = 2,
        BossDefeated = 3
    }

    public enum IdleAutoDefenseRewardEffectKind
    {
        None = 0,
        UnlockWeapon = 1,
        DamageRank = 2,
        FireRateRank = 3,
        RangeRank = 4,
        Repair = 5,
        RewardMultiplier = 6,
        ProjectileSpeed = 7,
        ExtraProjectile = 8,
        PulsePower = 9,
        ArcPower = 10,
        HomingPower = 11,
        GlobalDamageMultiplier = 12
    }

    public sealed class IdleAutoDefenseRewardDraftChoice
    {
        internal IdleAutoDefenseRewardDraftChoice(
            string id,
            string displayName,
            IdleAutoDefenseRewardRarity rarity,
            string typeName,
            string targetName,
            string effectDescription,
            string hotkeyLabel,
            bool isUnlock,
            string targetWeaponId,
            IdleAutoDefenseRewardEffectKind effectKind,
            double amount,
            string dedupeKey,
            double weight = 1d)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Rarity = rarity;
            TypeName = typeName ?? string.Empty;
            TargetName = targetName ?? string.Empty;
            EffectDescription = effectDescription ?? string.Empty;
            HotkeyLabel = hotkeyLabel ?? string.Empty;
            IsUnlock = isUnlock;
            TargetWeaponId = targetWeaponId ?? string.Empty;
            EffectKind = effectKind;
            Amount = amount;
            DedupeKey = dedupeKey ?? Id;
            Weight = weight;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IdleAutoDefenseRewardRarity Rarity { get; }
        public string RarityName => Rarity.ToString();
        public string TypeName { get; }
        public string TargetName { get; }
        public string EffectDescription { get; }
        public string HotkeyLabel { get; }
        public bool IsUnlock { get; }
        public double AuthoredAmount => Amount;
        public double AuthoredWeight => Weight;
        public string TargetWeaponId { get; }
        public IdleAutoDefenseRewardEffectKind EffectKind { get; }
        public string EffectKindName => EffectKind.ToString();
        internal double Amount { get; }
        internal string DedupeKey { get; }
        internal double Weight { get; }
    }
}
