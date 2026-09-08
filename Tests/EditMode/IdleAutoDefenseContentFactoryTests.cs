using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseContentFactoryTests
    {
        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void DisposeGeneratedContent()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
        }

        [Test]
        public void AttackAdmissionRetainsCompleteAuthoredSetWhenIndividualEntriesAreRejected()
        {
            AttackDefinitionAsset[] attacks = CreateAttacks();
            Array.Reverse(attacks);
            var assigned = new List<AttackDefinitionAsset>(attacks) { null, attacks[0] };

            AttackDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveAttackRecipesForTemplate(assigned, out int rejected);

            Assert.That(rejected, Is.EqualTo(2));
            Assert.That(resolved, Is.EqualTo(attacks));
            Assert.That(assigned.Count, Is.EqualTo(6));
            Assert.That(assigned[4], Is.Null);
            Assert.That(assigned[5], Is.SameAs(attacks[0]));
        }

        [Test]
        public void WaveAdmissionKeepsValidAuthoredWaveWhileStrictConversionRejectsNull()
        {
            EnemyDefinitionAsset enemy = EnemyDefinitionAsset.CreateTransient(
                "enemy.factory.authored", "Authored Enemy", EnemyRole.Basic, 20, 1, 5, 4,
                BasicIdleAutoDefenseGame.DamageType.Value);
            Track(enemy, enemy.Stats, enemy.Presentation);
            WaveDefinitionAsset wave = WaveDefinitionAsset.CreateTransient("wave.factory.authored", "Authored Wave", 17,
                new[] { new WaveEntryRecipe("authored-entry", enemy, 3, 1, 2, 4, "perimeter-east") });
            Track(wave, wave.Schedule, wave.Entries);

            WaveDefinitionAsset[] resolved = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(
                new[] { wave, null }, new[] { enemy }, out int rejected);

            Assert.That(rejected, Is.EqualTo(1));
            Assert.That(resolved, Is.EqualTo(new[] { wave }));
            var runtime = BasicIdleAutoDefenseGame.CreateEncounterWaves(resolved);
            Assert.That(runtime[0].Id.Value, Is.EqualTo("wave.factory.authored"));
            Assert.That(runtime[0].SpawnGroups[0].Id.Value, Is.EqualTo("wave.factory.authored.group.authored-entry"));
            Assert.Throws<ArgumentException>(() => BasicIdleAutoDefenseGame.CreateEncounterWaves(new[] { wave, null }));
        }

        [Test]
        public void RuntimeCompositionPreservesAuthoredWeaponOrderAndDoesNotScaleSourceAssets()
        {
            WeaponDefinitionAsset[] weapons = CreateWeapons(CreateAttacks());
            Array.Reverse(weapons);
            EnemyDefinitionAsset[] enemies = BasicIdleAutoDefenseGame.CreateEnemyDefinitions();
            foreach (EnemyDefinitionAsset enemy in enemies) Track(enemy, enemy.Stats, enemy.Presentation);

            AutoDefenseDefinition definition = BasicIdleAutoDefenseGame.CreateDefinition(enemies, weapons, difficultyMultiplier: 2f);

            Assert.That(enemies[0].Stats.MaximumHealth, Is.EqualTo(20f));
            Assert.That(definition.Enemies[0].MaximumHealth, Is.EqualTo(40d));
            Assert.That(definition.Enemies[0].ContactDamage, Is.EqualTo(8d));
            Assert.That(definition.WeaponModules.Count, Is.EqualTo(weapons.Length));
            for (int i = 0; i < weapons.Length; i++)
            {
                var module = definition.WeaponModules[i];
                Assert.That(module.WeaponDefinition.Id.Value, Is.EqualTo(weapons[i].Id));
                Assert.That(module.MountId.Value, Is.EqualTo("mount.idle-auto-defense." + weapons[i].Id));
                Assert.That(module.Source.Id.Value, Is.EqualTo("source.idle-auto-defense." + weapons[i].Id));
            }
        }

        [Test]
        public void ExplicitUpgradeCatalogUsesOnlySuppliedDefinitions()
        {
            RunUpgradeDefinitionAsset[] upgrades = CreateUpgrades(CreateWeapons(CreateAttacks()));
            var assigned = new[] { upgrades[3] };

            var catalog = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog(assigned);
            var strictCatalog = BasicIdleAutoDefenseGame.CreateRunUpgradeCatalogOrEmpty(assigned);

            Assert.That(catalog.Definitions.Count, Is.EqualTo(1));
            Assert.That(strictCatalog.Definitions.Count, Is.EqualTo(1));
            Assert.That(catalog.Definitions[0].Id.Value, Is.EqualTo(upgrades[3].Id));
            Assert.That(strictCatalog.Definitions[0].Id, Is.EqualTo(catalog.Definitions[0].Id));
            Assert.That(BasicIdleAutoDefenseGame.CreateRunUpgradeCatalog().Definitions.Count, Is.EqualTo(14));
        }

        private AttackDefinitionAsset[] CreateAttacks()
        {
            AttackDefinitionAsset[] attacks = BasicIdleAutoDefenseGame.CreateAttackRecipes();
            foreach (AttackDefinitionAsset attack in attacks) TrackAttack(attack);
            return attacks;
        }

        private void TrackAttack(AttackDefinitionAsset attack)
        {
            if (attack != null)
                Track(attack, attack.Mechanics, attack.Targeting, attack.Delivery, attack.StatusEffects, attack.Presentation);
        }

        private WeaponDefinitionAsset[] CreateWeapons(AttackDefinitionAsset[] attacks)
        {
            WeaponDefinitionAsset[] weapons = BasicIdleAutoDefenseGame.CreateWeaponDefinitionAssets(attacks);
            foreach (WeaponDefinitionAsset weapon in weapons) TrackReferencedWeapon(weapon);
            return weapons;
        }

        private void TrackReferencedWeapon(WeaponDefinitionAsset weapon)
        {
            if (weapon == null) return;
            Track(weapon, weapon.Stats, weapon.Presentation, weapon.Presentation.Prefab);
            TrackAttack(weapon.Stats.Attack);
        }

        private RunUpgradeDefinitionAsset[] CreateUpgrades(WeaponDefinitionAsset[] weapons)
        {
            RunUpgradeDefinitionAsset[] upgrades = BasicIdleAutoDefenseGame.CreateRunUpgradeDefinitionAssets(weapons);
            TrackUpgrades(upgrades);
            return upgrades;
        }

        private void TrackUpgrades(RunUpgradeDefinitionAsset[] upgrades)
        {
            foreach (RunUpgradeDefinitionAsset upgrade in upgrades) Track(upgrade, upgrade.Economy, upgrade.Effects);
        }

        private void Track(params Object[] objects)
        {
            foreach (Object value in objects)
                if (value != null && !_owned.Contains(value)) _owned.Add(value);
        }
    }
}
