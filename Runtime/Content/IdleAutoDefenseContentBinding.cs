using System;
using System.Collections.Generic;
using Deucarian.Attacks.Authoring;
using Deucarian.RunUpgrades.Authoring;
using Deucarian.WeaponSystems.Authoring;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Owns the resolved run content and only the fallback resources allocated by this binding.
    internal sealed class IdleAutoDefenseContentBinding : IDisposable
    {
        internal AttackDefinitionAsset[] Attacks { get; set; } = Array.Empty<AttackDefinitionAsset>();

        internal EnemyDefinitionAsset[] Enemies { get; set; } = Array.Empty<EnemyDefinitionAsset>();

        internal WaveDefinitionAsset[] Waves { get; set; } = Array.Empty<WaveDefinitionAsset>();

        internal WeaponDefinitionAsset[] Weapons { get; set; } = Array.Empty<WeaponDefinitionAsset>();

        internal RunUpgradeDefinitionAsset[] Upgrades { get; set; } = Array.Empty<RunUpgradeDefinitionAsset>();

        internal GameContentSetResolution ContentSet { get; set; }

        internal IdleAutoDefenseRewardCatalogAsset RewardCatalog { get; set; }

        internal IdleAutoDefenseEconomyAsset Economy { get; set; }

        internal IdleAutoDefenseRunProfileAsset RunProfile { get; set; }

        internal IdleAutoDefenseProgressionAsset Progression { get; set; }

        internal IdleAutoDefenseOfflineProgressionAsset OfflineProgression { get; set; }

        internal IdleAutoDefenseGameRulesAsset GameRules { get; set; }

        internal bool UsingContentSetRuntimeSettings { get; set; }

        internal int InvalidAssignedRecipeCount { get; set; }

        internal int InvalidAssignedEnemyCount { get; set; }

        internal int InvalidAssignedWaveCount { get; set; }

        internal int InvalidAssignedWeaponCount { get; set; }

        internal int InvalidAssignedUpgradeCount { get; set; }

        internal int InvalidAssignedContentPackIssueCount { get; set; }

        internal int InvalidAssignedContentSetIssueCount { get; set; }

        internal bool UsingAssignedContentPack { get; set; }

        internal bool UsingAssignedContentSet { get; set; }

        internal bool StartupBlocked { get; set; }

        internal string StartupError { get; set; } = string.Empty;

        internal bool FallbackModeActive { get; set; }

        internal string AssignedContentPackStatus { get; set; } = string.Empty;

        internal string AssignedContentSetStatus { get; set; } = string.Empty;

        private readonly Action<string> _warning;
        private readonly Action<string> _error;
        private readonly Action<GameContentSetAsset> _applyRuntimeSettings;
        private readonly IdleAutoDefenseGeneratedContent _generated = new IdleAutoDefenseGeneratedContent();
        private GameContentPackAsset _contentPack;
        private GameContentSetAsset _contentSet;
        private bool _requireAuthoredContent;

        internal IdleAutoDefenseContentBinding(Action<string> warning, Action<string> error, Action<GameContentSetAsset> applyRuntimeSettings)
        {
            _warning = warning ?? throw new ArgumentNullException(nameof(warning));
            _error = error ?? throw new ArgumentNullException(nameof(error));
            _applyRuntimeSettings = applyRuntimeSettings ?? throw new ArgumentNullException(nameof(applyRuntimeSettings));
        }

        public void Dispose() => _generated.Dispose();

        internal void ResetDiagnostics()
        {
            UsingContentSetRuntimeSettings = false;
            InvalidAssignedRecipeCount = 0;
            InvalidAssignedEnemyCount = 0;
            InvalidAssignedWaveCount = 0;
            InvalidAssignedWeaponCount = 0;
            InvalidAssignedUpgradeCount = 0;
            InvalidAssignedContentPackIssueCount = 0;
            InvalidAssignedContentSetIssueCount = 0;
            UsingAssignedContentPack = false;
            UsingAssignedContentSet = false;
            AssignedContentPackStatus = string.Empty;
            AssignedContentSetStatus = string.Empty;
        }

        internal void BlockStrictStartup()
        {
            StartupBlocked = true;
            FallbackModeActive = false;
            string source = _contentPack != null
                ? "content pack '" + _contentPack.name + "'"
                : _contentSet != null
                    ? "content set '" + _contentSet.name + "'"
                    : "the generated scene assignment";
            string details = !string.IsNullOrWhiteSpace(AssignedContentPackStatus)
                ? AssignedContentPackStatus + " " + AssignedContentSetStatus
                : AssignedContentSetStatus;
            StartupError = "Strict authored startup blocked: " + source + " is missing or invalid. " + details;
            _error("[Idle Auto Defense Template] " + StartupError);
        }

        internal void BindExplicitFallbackCore(IdleAutoDefenseRewardDraftSettings settings, IdleAutoDefenseRewardDraftCatalog catalog, long startingCredits)
        {
            FallbackModeActive = true;
            StartupBlocked = false;
            StartupError = string.Empty;
            RewardCatalog = _generated.Own(IdleAutoDefenseRewardCatalogAsset.CreateTransient(settings, catalog));
            Economy = _generated.Own(IdleAutoDefenseEconomyAsset.CreateTransient(startingCredits > int.MaxValue ? int.MaxValue : (int)startingCredits, 0));
            RunProfile = _generated.Own(IdleAutoDefenseRunProfileAsset.CreateTransient(Waves));
            Progression = _generated.Own(IdleAutoDefenseProgressionAsset.CreateTransient());
            OfflineProgression = _generated.Own(IdleAutoDefenseOfflineProgressionAsset.CreateTransient());
            GameRules = _generated.Own(IdleAutoDefenseGameRulesAsset.CreateTransient(Weapons, Enemies));
            AssignedContentPackStatus = string.IsNullOrWhiteSpace(AssignedContentPackStatus)
                ? "Explicit unbound fallback host."
                : AssignedContentPackStatus;
            AssignedContentSetStatus = string.IsNullOrWhiteSpace(AssignedContentSetStatus)
                ? "Using documented transient fallback content for an unbound debug/test host."
                : AssignedContentSetStatus;
        }

        internal bool TryUseAssignedContentSet(GameContentPackAsset contentPack, GameContentSetAsset contentSet, bool requireAuthoredContent)
        {
            _generated.Dispose();
            _contentPack = contentPack;
            _contentSet = contentSet;
            _requireAuthoredContent = requireAuthoredContent;
            UsingAssignedContentPack = false;
            UsingAssignedContentSet = false;
            StartupBlocked = false;
            StartupError = string.Empty;
            FallbackModeActive = false;
            InvalidAssignedContentPackIssueCount = 0;
            InvalidAssignedContentSetIssueCount = 0;
            ContentSet = null;
            RewardCatalog = null;
            Economy = null;
            RunProfile = null;
            Progression = null;
            OfflineProgression = null;
            GameRules = null;

            if (_contentPack != null)
            {
                GameContentPackResolution packResolution = GameContentPackValidator.Resolve(_contentPack, _contentSet);
                InvalidAssignedContentPackIssueCount = packResolution.PackReport.ErrorCount;
                if (packResolution.ContentSetResolution != null)
                    InvalidAssignedContentSetIssueCount = packResolution.ContentSetResolution.Report.ErrorCount;

                if (!packResolution.IsValid)
                {
                    AssignedContentPackStatus = _requireAuthoredContent
                        ? "Assigned content pack is invalid; strict startup will block gameplay."
                        : "Assigned content pack is invalid; explicit fallback mode may be used by this debug/test host.";
                    AssignedContentSetStatus = "Content pack could not resolve a playable content set.";
                    if (!_requireAuthoredContent)
                        _warning(
                            "[Idle Auto Defense Template] Assigned GameContentPackAsset '" + _contentPack.name + "' is incomplete or invalid. Entering explicit fallback mode. " + CreateContentPackIssueSummary(packResolution));
                    return false;
                }

                ApplyResolvedContentSet(packResolution.ContentSetResolution);
                UsingAssignedContentPack = true;
                AssignedContentPackStatus = "Using assigned content pack: " + packResolution.ContentPack.DisplayName;
                AssignedContentSetStatus = "Using content set from pack: " + packResolution.SelectedContentSet.DisplayName;
                if (packResolution.PackReport.WarningCount > 0 || packResolution.ContentSetResolution.Report.WarningCount > 0)
                {
                    _warning(
                        "[Idle Auto Defense Template] Assigned GameContentPackAsset '" + _contentPack.name + "' is playable with warnings. " + CreateContentPackIssueSummary(packResolution));
                }

                return true;
            }

            if (_contentSet == null)
            {
                AssignedContentPackStatus = "No content pack assigned.";
                AssignedContentSetStatus = _requireAuthoredContent
                    ? "No content set assigned; strict startup will block gameplay."
                    : "No content set assigned; this unbound debug/test host may use explicit fallback mode.";
                return false;
            }

            GameContentSetResolution resolution = BasicIdleAutoDefenseGame.ResolveGameContentSetForTemplate(_contentSet);
            ContentSet = resolution;
            InvalidAssignedContentSetIssueCount = resolution.Report.ErrorCount;
            if (!resolution.IsValid)
            {
                AssignedContentSetStatus = _requireAuthoredContent
                    ? "Assigned content set is invalid; strict startup will block gameplay."
                    : "Assigned content set is invalid; this debug/test host may use explicit fallback mode.";
                if (!_requireAuthoredContent)
                    _warning(
                        "[Idle Auto Defense Template] Assigned GameContentSetAsset '" + _contentSet.name + "' is incomplete or invalid. Entering explicit fallback mode. " + CreateContentSetIssueSummary(resolution.Report));
                return false;
            }

            ApplyResolvedContentSet(resolution);
            AssignedContentSetStatus = "Using assigned content set: " + resolution.ContentSet.DisplayName;
            if (resolution.Report.WarningCount > 0)
            {
                _warning(
                    "[Idle Auto Defense Template] Assigned GameContentSetAsset '" + _contentSet.name + "' is playable with warnings. " + CreateContentSetIssueSummary(resolution.Report));
            }

            return true;
        }

        private void ApplyResolvedContentSet(GameContentSetResolution resolution)
        {
            ContentSet = resolution;
            Attacks = CopyResolved(resolution.AttackRecipes);
            Enemies = CopyResolved(resolution.Enemies);
            Waves = CopyResolved(resolution.Waves);
            Weapons = CopyResolved(resolution.Weapons);
            Upgrades = CopyResolved(resolution.Upgrades);
            RewardCatalog = resolution.ContentSet.RewardCatalog;
            Economy = resolution.ContentSet.Economy;
            RunProfile = resolution.ContentSet.RunProfile;
            Progression = resolution.ContentSet.Progression;
            OfflineProgression = resolution.ContentSet.OfflineProgression;
            GameRules = resolution.ContentSet.GameRules;
            _applyRuntimeSettings(resolution.ContentSet);
            FallbackModeActive = false;
            StartupBlocked = false;
            StartupError = string.Empty;
            UsingAssignedContentSet = true;
        }

        internal WeaponDefinitionAsset[] ResolveActiveWeaponDefinitionsForRun()
        {
            WeaponDefinitionAsset startingWeapon = null;
            if (ContentSet != null && ContentSet.IsValid && ContentSet.ContentSet != null)
                startingWeapon = FindWeaponDefinitionForRun(Weapons, ContentSet.ContentSet.StartingWeapon);
            startingWeapon ??= FindWeaponDefinitionForRun(Weapons, BasicIdleAutoDefenseGame.ShardLauncherWeaponId.Value);
            startingWeapon ??= Weapons.Length > 0 ? Weapons[0] : null;
            return startingWeapon == null ? Weapons : new[] { startingWeapon };
        }

        private static WeaponDefinitionAsset FindWeaponDefinitionForRun(IReadOnlyList<WeaponDefinitionAsset> weapons, WeaponDefinitionAsset target)
        {
            return target == null ? null : FindWeaponDefinitionForRun(weapons, target.Id);
        }

        private static WeaponDefinitionAsset FindWeaponDefinitionForRun(IReadOnlyList<WeaponDefinitionAsset> weapons, string id)
        {
            if (weapons == null || string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponDefinitionAsset weapon = weapons[i];
                if (weapon != null && string.Equals(weapon.Id, id, StringComparison.OrdinalIgnoreCase))
                    return weapon;
            }

            return null;
        }

        private static string CreateContentSetIssueSummary(GameContentSetValidationReport report)
        {
            if (report == null || report.Issues.Count == 0) return "No validation details were reported.";
            var messages = new List<string>();
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                messages.Add(issue.Path + ": " + issue.Message);
            }

            return string.Join(" | ", messages);
        }

        private static string CreateContentPackIssueSummary(GameContentPackResolution resolution)
        {
            if (resolution == null) return "No validation details were reported.";
            var messages = new List<string>();
            AddContentPackIssues(messages, resolution.PackReport);
            if (resolution.ContentSetResolution != null)
                AddContentSetIssues(messages, resolution.ContentSetResolution.Report);
            return messages.Count == 0 ? "No validation details were reported." : string.Join(" | ", messages);
        }

        private static void AddContentPackIssues(List<string> messages, GameContentPackValidationReport report)
        {
            if (messages == null || report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = report.Issues[i];
                messages.Add(issue.Path + ": " + issue.Message);
            }
        }

        private static void AddContentSetIssues(List<string> messages, GameContentSetValidationReport report)
        {
            if (messages == null || report == null) return;
            for (int i = 0; i < report.Issues.Count; i++)
            {
                GameContentSetValidationIssue issue = report.Issues[i];
                messages.Add("ContentSet." + issue.Path + ": " + issue.Message);
            }
        }

        private static T[] CopyResolved<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }

        internal AttackDefinitionAsset[] ResolveAttackRecipes(IReadOnlyList<AttackDefinitionAsset> assigned)
        {
            AttackDefinitionAsset[] recipes = BasicIdleAutoDefenseGame.ResolveAttackRecipesForTemplate(assigned, out int rejectedRecipeCount);
            InvalidAssignedRecipeCount = rejectedRecipeCount;
            if (InvalidAssignedRecipeCount > 0)
            {
                _warning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or incomplete assigned attack recipe entries. The starter weapons require pulse, shard, arc, and homing attack recipes; missing required recipes fall back to built-in transient recipes.");
            }

            _generated.OwnGenerated(assigned, recipes);
            return recipes;
        }

        internal EnemyDefinitionAsset[] ResolveEnemyDefinitions(IReadOnlyList<EnemyDefinitionAsset> assigned)
        {
            EnemyDefinitionAsset[] definitions = BasicIdleAutoDefenseGame.ResolveEnemyDefinitionsForTemplate(assigned, out int rejectedDefinitionCount);
            InvalidAssignedEnemyCount = rejectedDefinitionCount;
            if (InvalidAssignedEnemyCount > 0)
            {
                _warning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, prefabless, or incomplete assigned enemy definition entries. The starter waves require swarm, runner, tank, shielded, elite, and boss enemy IDs; missing required enemies fall back to built-in transient enemies.");
            }

            _generated.OwnGenerated(assigned, definitions);
            return definitions;
        }

        internal WaveDefinitionAsset[] ResolveWaveDefinitions(IReadOnlyList<WaveDefinitionAsset> assigned, IReadOnlyList<EnemyDefinitionAsset> enemyDefinitions)
        {
            WaveDefinitionAsset[] definitions = BasicIdleAutoDefenseGame.ResolveWaveDefinitionsForTemplate(assigned, enemyDefinitions, out int rejectedDefinitionCount);
            InvalidAssignedWaveCount = rejectedDefinitionCount;
            if (InvalidAssignedWaveCount > 0)
            {
                _warning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or enemy-mismatched assigned wave definition entries. Missing or invalid waves fall back to built-in transient starter waves.");
            }

            _generated.OwnGenerated(assigned, definitions);
            return definitions;
        }

        internal WeaponDefinitionAsset[] ResolveWeaponDefinitions(IReadOnlyList<WeaponDefinitionAsset> assigned, IReadOnlyList<AttackDefinitionAsset> attackRecipes)
        {
            WeaponDefinitionAsset[] definitions = BasicIdleAutoDefenseGame.ResolveWeaponDefinitionsForTemplate(assigned, attackRecipes, out int rejectedDefinitionCount);
            InvalidAssignedWeaponCount = rejectedDefinitionCount;
            if (InvalidAssignedWeaponCount > 0)
            {
                _warning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or attack-mismatched assigned weapon definition entries. The starter mounts require shard launcher, pulse beam, arc burst, and homing pulse weapon IDs; missing required weapons fall back to built-in transient weapons.");
            }

            _generated.OwnGenerated(assigned, definitions);
            return definitions;
        }

        internal RunUpgradeDefinitionAsset[] ResolveUpgradeDefinitions(IReadOnlyList<RunUpgradeDefinitionAsset> assigned)
        {
            RunUpgradeDefinitionAsset[] definitions = IdleAutoDefenseUpgradeContent.ResolveUpgradeDefinitionsForTemplate(assigned, out int rejectedDefinitionCount, _generated);
            InvalidAssignedUpgradeCount = rejectedDefinitionCount;
            if (InvalidAssignedUpgradeCount > 0)
            {
                _warning(
                    "Idle Auto Defense template ignored invalid, duplicate, empty, or incomplete assigned upgrade definition entries. Missing or invalid upgrade sets fall back to built-in transient upgrades.");
            }

            return definitions;
        }
    }
}
