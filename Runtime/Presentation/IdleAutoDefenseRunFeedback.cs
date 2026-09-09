using System.Globalization;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseRunFeedback
    {
        private readonly IdleAutoDefenseWorldPresentation _world;
        private readonly IdleAutoDefenseRuntimeUi _ui;

        internal IdleAutoDefenseRunFeedback(IdleAutoDefenseWorldPresentation world, IdleAutoDefenseRuntimeUi ui)
        {
            _world = world;
            _ui = ui;
        }

        internal void Update(float deltaSeconds)
        {
            _world.Beams.UpdateActiveBeamVisuals(deltaSeconds);
            _ui.UpdateDamageNumbers(deltaSeconds);
            _world.Events.UpdateCameraShake(deltaSeconds);
        }

        internal void EmitObjectiveDamage(int reached)
        {
            Vector3 position = IdleAutoDefenseCombatTargets.CreateTowerMuzzlePosition(Vector3.zero);
            _ui.EmitDamageNumber(position, reached, new Color(1f, 0.25f, 0.18f), "-");
            _world.Events.EmitKenneySpriteBurst("Tower Damage Burst", "Art/impact_flame", position, new Color(1f, 0.18f, 0.08f), 1.1f, 0.46f, 0.25f, 52);
            _world.Events.TriggerCameraShake(0.2f, 0.13f);
        }

        internal void EmitKillCredits(long earned)
        {
            _ui.EmitFloatingStatusText(IdleAutoDefenseCombatTargets.CreateTowerMuzzlePosition(Vector3.zero),
                "+" + earned.ToString(CultureInfo.InvariantCulture) + " credits", new Color(1f, 0.86f, 0.2f));
        }

        internal void EmitCreditPickup(Vector3 position)
            => _world.Events.EmitKenneySpriteBurst("Credit Pickup Burst", "Art/currency_coin_gold", position, new Color(1f, 0.88f, 0.18f), 0.62f, 0.72f, 0.72f, 65);

        internal void EmitRewardChoiceFeedback(IdleAutoDefenseRewardDraftChoice choice)
        {
            if (choice == null) return;
            Color color = ResolveRewardRarityColor(choice.Rarity);
            string prefix = choice.IsUnlock ? "UNLOCK: " : choice.RarityName.ToUpperInvariant() + ": ";
            _ui.EmitFloatingStatusText(IdleAutoDefenseCombatTargets.CreateTowerMuzzlePosition(Vector3.zero), prefix + choice.DisplayName, color);
            _world.Events.EmitKenneySpriteBurst("Reward Choice Burst", "Art/currency_coin_gold", IdleAutoDefenseCombatTargets.CreateTowerMuzzlePosition(Vector3.zero), color, 1.22f, 0.72f, 0.68f, 70);
            bool bigReward = (int)choice.Rarity >= (int)IdleAutoDefenseRewardRarity.Epic;
            _world.Events.TriggerCameraShake(bigReward ? 0.22f : 0.14f, bigReward ? 0.12f : 0.07f);
        }

        internal void EmitUpgradeFeedback(string text, Color color, float scale)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _ui.EmitFloatingStatusText(IdleAutoDefenseCombatTargets.CreateTowerMuzzlePosition(Vector3.zero), text, color);
            _world.Events.EmitKenneySpriteBurst("Upgrade Feedback Burst", "Art/impact_flame", IdleAutoDefenseCombatTargets.CreateTowerMuzzlePosition(Vector3.zero), color, Mathf.Max(0.35f, scale), 0.5f, 0.45f, 68);
            _world.Events.TriggerCameraShake(0.08f, 0.045f);
        }

        internal static Color ResolveRewardRarityColor(IdleAutoDefenseRewardRarity rarity)
        {
            if (rarity == IdleAutoDefenseRewardRarity.Legendary) return new Color(1f, 0.78f, 0.16f);
            if (rarity == IdleAutoDefenseRewardRarity.Epic) return new Color(0.78f, 0.42f, 1f);
            if (rarity == IdleAutoDefenseRewardRarity.Rare) return new Color(0.25f, 0.65f, 1f);
            if (rarity == IdleAutoDefenseRewardRarity.Uncommon) return new Color(0.35f, 1f, 0.55f);
            return new Color(0.92f, 0.96f, 1f);
        }

    }
}
