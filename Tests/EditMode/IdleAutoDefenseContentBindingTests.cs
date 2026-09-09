using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.WeaponSystems.Authoring;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseContentBindingTests
    {
        [Test]
        public void DiagnosticResetPreservesStartupDecisionAndResolvedContent()
        {
            using var binding = CreateBinding();
            AttackDefinitionAsset[] resolved = binding.Attacks;
            binding.StartupBlocked = true;
            binding.StartupError = "Retain the strict-startup diagnosis.";
            binding.FallbackModeActive = true;
            binding.UsingContentSetRuntimeSettings = true;
            binding.InvalidAssignedRecipeCount = 3;
            binding.InvalidAssignedContentSetIssueCount = 2;
            binding.UsingAssignedContentSet = true;
            binding.AssignedContentSetStatus = "Previous assignment.";

            binding.ResetDiagnostics();

            Assert.That(binding.StartupBlocked, Is.True);
            Assert.That(binding.StartupError, Is.EqualTo("Retain the strict-startup diagnosis."));
            Assert.That(binding.FallbackModeActive, Is.True);
            Assert.That(binding.Attacks, Is.SameAs(resolved));
            Assert.That(binding.UsingContentSetRuntimeSettings, Is.False);
            Assert.That(binding.InvalidAssignedRecipeCount, Is.Zero);
            Assert.That(binding.InvalidAssignedContentSetIssueCount, Is.Zero);
            Assert.That(binding.UsingAssignedContentSet, Is.False);
            Assert.That(binding.AssignedContentSetStatus, Is.Empty);
        }

        [Test]
        public void ReleasingBindingDestroysGeneratedCoreAndRecipeSections()
        {
            using var binding = CreateBinding();
            binding.TryUseAssignedContentSet(null, null, false);
            binding.Attacks = binding.ResolveAttackRecipes(null);
            binding.Enemies = binding.ResolveEnemyDefinitions(null);
            binding.Waves = binding.ResolveWaveDefinitions(null, binding.Enemies);
            binding.Weapons = binding.ResolveWeaponDefinitions(null, binding.Attacks);
            binding.Upgrades = binding.ResolveUpgradeDefinitions(null);
            binding.BindExplicitFallbackCore(IdleAutoDefenseRewardDraftSettings.CreateDefault(), IdleAutoDefenseRewardDraftCatalog.CreateDefault(), 10);
            Object[] allocated =
            {
                binding.Attacks[0], binding.Attacks[0].Mechanics, binding.Attacks[0].Presentation,
                binding.Enemies[0], binding.Enemies[0].Stats, binding.Enemies[0].Presentation,
                binding.Waves[0], binding.Waves[0].Schedule, binding.Waves[0].Entries.Entries[0].Enemy,
                binding.Weapons[0], binding.Weapons[0].Stats, binding.Weapons[0].Presentation.Prefab,
                binding.Upgrades[0], binding.Upgrades[0].Economy, binding.Upgrades[0].Effects,
                binding.RewardCatalog, binding.Economy, binding.RunProfile, binding.Progression,
                binding.OfflineProgression, binding.GameRules
            };

            binding.Dispose();
            binding.Dispose();

            for (int i = 0; i < allocated.Length; i++)
                Assert.That(allocated[i] == null, Is.True, "Generated resource at index " + i + " must be released.");
        }

        [Test]
        public void ReleasingBindingKeepsAssignedTransientAttacksAlive()
        {
            AttackDefinitionAsset[] assigned = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            using var binding = CreateBinding();
            try
            {
                binding.TryUseAssignedContentSet(null, null, false);
                binding.Attacks = binding.ResolveAttackRecipes(assigned);
                binding.Weapons = binding.ResolveWeaponDefinitions(null, binding.Attacks);
                binding.Dispose();

                foreach (AttackDefinitionAsset attack in assigned)
                {
                    Assert.That(attack != null, Is.True);
                    Assert.That(attack.Mechanics != null, Is.True);
                    Assert.That(attack.Delivery != null, Is.True);
                }
            }
            finally
            {
                binding.Dispose();
                foreach (AttackDefinitionAsset attack in assigned) DestroyAttack(attack);
            }
        }

        [Test]
        public void UpgradeFallbackOwnsDefaultWeaponsEvenWhenOnlyShardIsReferenced()
        {
            var before = new HashSet<WeaponDefinitionAsset>(Resources.FindObjectsOfTypeAll<WeaponDefinitionAsset>());
            using var binding = CreateBinding();
            binding.Upgrades = binding.ResolveUpgradeDefinitions(null);
            var allocated = new List<WeaponDefinitionAsset>();
            foreach (WeaponDefinitionAsset weapon in Resources.FindObjectsOfTypeAll<WeaponDefinitionAsset>())
                if (!before.Contains(weapon)) allocated.Add(weapon);
            Assert.That(allocated.Count, Is.EqualTo(4));

            binding.Dispose();

            foreach (WeaponDefinitionAsset weapon in allocated)
                Assert.That(weapon == null, Is.True, "Unused fallback weapon siblings must be released too.");
        }

        [Test]
        public void StrictMissingAssignmentBlocksWithoutAllocatingFallbackCore()
        {
            var errors = new List<string>();
            using var binding = new IdleAutoDefenseContentBinding(_ => { }, errors.Add, _ => { });
            Assert.That(binding.TryUseAssignedContentSet(null, null, true), Is.False);
            binding.BlockStrictStartup();

            Assert.That(binding.StartupBlocked, Is.True);
            Assert.That(binding.FallbackModeActive, Is.False);
            Assert.That(binding.GameRules, Is.Null);
            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.Contain("Strict authored startup blocked"));
        }

        private static IdleAutoDefenseContentBinding CreateBinding() => new IdleAutoDefenseContentBinding(_ => { }, _ => { }, _ => { });

        private static void DestroyAttack(AttackDefinitionAsset attack)
        {
            if (attack == null) return;
            Object.DestroyImmediate(attack.Mechanics);
            Object.DestroyImmediate(attack.Targeting);
            Object.DestroyImmediate(attack.Delivery);
            Object.DestroyImmediate(attack.StatusEffects);
            Object.DestroyImmediate(attack.Presentation);
            Object.DestroyImmediate(attack);
        }
    }
}
