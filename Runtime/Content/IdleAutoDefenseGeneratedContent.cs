using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.Common;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Records known factory allocations. Authored assets are borrowed regardless of hide flags.
    internal sealed class IdleAutoDefenseGeneratedContent : IDisposable
    {
        private readonly List<Object> _owned = new List<Object>();

        internal T Own<T>(T value) where T : Object
        {
            if (value != null && !_owned.Contains(value)) _owned.Add(value);
            return value;
        }

        internal T[] OwnGenerated<T>(IReadOnlyList<T> assigned, T[] resolved) where T : Object
        {
            foreach (T value in resolved)
                if (value != null && !ContainsReference(assigned, value)) OwnGraph(value);
            return resolved;
        }

        private static bool ContainsReference<T>(IReadOnlyList<T> values, T target) where T : Object
        {
            if (values == null) return false;
            for (int i = 0; i < values.Count; i++)
                if (ReferenceEquals(values[i], target)) return true;
            return false;
        }

        private void OwnGraph(Object value)
        {
            Own(value);
            switch (value)
            {
                case AttackDefinitionAsset attack:
                    Own(attack.Mechanics);
                    Own(attack.Targeting);
                    Own(attack.Delivery);
                    Own(attack.StatusEffects);
                    Own(attack.Presentation);
                    break;
                case EnemyDefinitionAsset enemy:
                    Own(enemy.Stats);
                    Own(enemy.Presentation);
                    break;
                case WaveDefinitionAsset wave:
                    Own(wave.Schedule);
                    Own(wave.Entries);
                    // Starter waves allocate their own enemy set; assigned waves never reach this branch.
                    foreach (WaveEntryRecipe entry in wave.Entries.Entries)
                        if (entry != null && entry.Enemy != null) OwnGraph(entry.Enemy);
                    break;
                case WeaponDefinitionAsset weapon:
                    Own(weapon.Stats);
                    Own(weapon.Presentation);
                    Own(weapon.Presentation.Prefab);
                    // Attack recipes are tracked at their allocation site, or borrowed from the caller.
                    break;
                case RunUpgradeDefinitionAsset upgrade:
                    Own(upgrade.Economy);
                    Own(upgrade.Effects);
                    break;
            }
        }

        public void Dispose()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                UnityObjectUtility.DestroySafely(_owned[i]);
            _owned.Clear();
        }
    }
}
