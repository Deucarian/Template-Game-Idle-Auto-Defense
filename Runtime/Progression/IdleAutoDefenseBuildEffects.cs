using System;
using Deucarian.Combat;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal interface IIdleAutoDefenseBuildEffects
    {
        bool EncounterRunning { get; }
        bool HasObjective { get; }
        void ChangeObjectiveMaximum(double amount, MaximumChangePolicy policy);
        void HealObjective(double amount);
        void CreateWeapon(string weaponId, string attackId, bool enabled);
        void Feedback(string text, Color color, float scale);
    }

    /// <summary>Composition adapter for the build's explicit world commands.</summary>
    internal sealed class IdleAutoDefenseBuildEffects : IIdleAutoDefenseBuildEffects
    {
        private readonly Func<bool> _running;
        private readonly Func<bool> _hasObjective;
        private readonly Action<double, MaximumChangePolicy> _maximum;
        private readonly Action<double> _heal;
        private readonly Action<string, string, bool> _weapon;
        private readonly Action<string, Color, float> _feedback;

        internal IdleAutoDefenseBuildEffects(Func<bool> running, Func<bool> hasObjective,
            Action<double, MaximumChangePolicy> maximum, Action<double> heal,
            Action<string, string, bool> weapon, Action<string, Color, float> feedback)
        {
            _running = running ?? throw new ArgumentNullException(nameof(running));
            _hasObjective = hasObjective ?? throw new ArgumentNullException(nameof(hasObjective));
            _maximum = maximum ?? throw new ArgumentNullException(nameof(maximum));
            _heal = heal ?? throw new ArgumentNullException(nameof(heal));
            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        public bool EncounterRunning => _running();
        public bool HasObjective => _hasObjective();
        public void ChangeObjectiveMaximum(double amount, MaximumChangePolicy policy) => _maximum(amount, policy);
        public void HealObjective(double amount) => _heal(amount);
        public void CreateWeapon(string weaponId, string attackId, bool enabled) => _weapon(weaponId, attackId, enabled);
        public void Feedback(string text, Color color, float scale) => _feedback(text, color, scale);
    }
}
