using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseRewardSelectionTests
    {
        [Test]
        public void EmptyCatalogDoesNotConsumeSeedAndResetReplaysTheSameAuthoredChoices()
        {
            IdleAutoDefenseRewardDraftCatalog catalog = new IdleAutoDefenseRewardDraftCatalog();
            var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var legendary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inventory = new IdleAutoDefenseRewardInventory(IdleAutoDefenseRewardDraftSettings.CreateDefault,
                () => catalog, () => 1, ranks, ranks, ranks, legendary, selected,
                id => id == BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value, id => "Observed " + id);
            var selector = new IdleAutoDefenseRewardSelection(inventory);
            Assert.That(selector.Generate(IdleAutoDefenseRewardDraftKind.LevelUp), Is.Empty);
            Assert.That(selector.Sequence, Is.Zero);
            catalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();
            IdleAutoDefenseRewardDraftChoice[] first = selector.Generate(IdleAutoDefenseRewardDraftKind.LevelUp);
            Assert.That(first.Length, Is.EqualTo(3));
            Assert.That(first[0].IsUnlock, Is.True);
            Assert.That(first[0].TargetWeaponId, Is.EqualTo(BasicIdleAutoDefenseGame.PulseCannonWeaponId.Value));
            Assert.That(first.Select(choice => choice.HotkeyLabel), Is.EqualTo(new[] { "1", "2", "3" }));
            selector.Generate(IdleAutoDefenseRewardDraftKind.BossDefeated);
            selector.Reset();
            IdleAutoDefenseRewardDraftChoice[] replay = selector.Generate(IdleAutoDefenseRewardDraftKind.LevelUp);
            Assert.That(replay.Select(choice => choice.Id), Is.EqualTo(first.Select(choice => choice.Id)));
            Assert.That(selector.Sequence, Is.EqualTo(1));
            Assert.That(ranks, Is.Empty);
            Assert.That(selected, Is.Empty);
            Assert.That(legendary, Is.Empty);
        }

        [Test]
        public void LiveUnlockObservationExcludesAlreadyOwnedModulesAndKeepsDedupeGroupsUnique()
        {
            var catalog = IdleAutoDefenseRewardDraftCatalog.CreateDefault();
            var ranks = new Dictionary<string, int>();
            var owned = new HashSet<string> { BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value };
            var inventory = new IdleAutoDefenseRewardInventory(IdleAutoDefenseRewardDraftSettings.CreateDefault,
                () => catalog, () => 2, ranks, ranks, ranks, new HashSet<string>(), new HashSet<string>(),
                owned.Contains, id => id);
            var selector = new IdleAutoDefenseRewardSelection(inventory);
            foreach (IdleAutoDefenseWeaponUnlockReward unlock in catalog.WeaponUnlocks) owned.Add(unlock.WeaponId);
            for (int draft = 0; draft < 12; draft++)
            {
                IdleAutoDefenseRewardDraftChoice[] choices = selector.Generate(IdleAutoDefenseRewardDraftKind.LevelUp);
                Assert.That(choices.Any(choice => choice.IsUnlock), Is.False);
                Assert.That(choices.Select(choice => choice.DedupeKey.Trim().ToLowerInvariant()).Distinct().Count(), Is.EqualTo(choices.Length));
            }
            Assert.That(ranks, Is.Empty);
        }
    }
}
