using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    [InitializeOnLoad]
    internal static class IdleAutoDefenseControlCenter
    {
        private const string PackageId =
            "com.deucarian.template.game.idle-auto-defense";
        private const string ToolId = "deucarian.template.idle-auto-defense";

        static IdleAutoDefenseControlCenter()
        {
            DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                ToolId,
                "Idle Auto Defense Setup",
                "Create and validate an Idle Auto Defense game.",
                DeucarianControlCenterArea.Authoring,
                IdleAutoDefenseTemplateMenu.CreateGameFromTemplate,
                PackageId,
                searchTerms: new[] { "idle", "defense", "template", "setup" },
                order: 200));
            DeucarianControlCenterRegistry.RegisterSectionProvider(new Provider());
        }

        private sealed class Provider : IDeucarianControlCenterSectionProvider
        {
            public string Id => PackageId + ".control-center";

            public IEnumerable<DeucarianControlCenterSection> Capture(
                DeucarianControlCenterContext context)
            {
                yield return new DeucarianControlCenterSection(
                    PackageId + ".authoring-section",
                    DeucarianControlCenterArea.Authoring,
                    "Idle Auto Defense",
                    new[] { CreateAuthoringCard() },
                    order: 200);
                yield return new DeucarianControlCenterSection(
                    PackageId + ".developer-section",
                    DeucarianControlCenterArea.Authoring,
                    "Idle Auto Defense Audits and Inspection",
                    new[] { CreateDeveloperCard() },
                    order: 210);
            }

            private static DeucarianControlCenterCard CreateAuthoringCard()
            {
                bool hasMainContentSet =
                    IdleAutoDefensePlayableContentAuditMenu.HasMainContentSet();
                return new DeucarianControlCenterCard(
                    PackageId + ".authoring",
                    DeucarianControlCenterArea.Authoring,
                    "Create and Validate",
                    "Create a playable template and run domain-owned content validation.",
                    PackageId,
                    hasMainContentSet
                        ? DeucarianControlCenterStatus.Success
                        : DeucarianControlCenterStatus.Info,
                    hasMainContentSet
                        ? "Main Idle Auto Defense content set found"
                        : "Main Idle Auto Defense content set not found",
                    actions: new[]
                    {
                        new DeucarianControlCenterAction(
                            "create",
                            "Create Playable Game",
                            IdleAutoDefenseTemplateMenu.CreateGameFromTemplate),
                        new DeucarianControlCenterAction(
                            "validate-authored",
                            "Validate Authored Content",
                            IdleAutoDefenseAuthoredContentValidationMenu.ValidateAuthoredContent),
                        new DeucarianControlCenterAction(
                            "docs",
                            "Open Documentation",
                            IdleAutoDefenseTemplateMenu.OpenTemplateDocs)
                    },
                    searchTerms: new[] { "idle", "defense", "setup", "validate" });
            }

            private static DeucarianControlCenterCard CreateDeveloperCard()
            {
                return new DeucarianControlCenterCard(
                    PackageId + ".developer",
                    DeucarianControlCenterArea.Authoring,
                    "Playable Content Diagnostics",
                    "Inspect content and generate local, domain-owned audit reports.",
                    PackageId,
                    DeucarianControlCenterStatus.Info,
                    "Local project diagnostics",
                    actions: new[]
                    {
                        new DeucarianControlCenterAction(
                            "validate-playable",
                            "Validate Playable Content",
                            IdleAutoDefensePlayableContentAuditMenu.ValidatePlayableContent),
                        new DeucarianControlCenterAction(
                            "open-content-set",
                            "Open Main Content Set",
                            IdleAutoDefensePlayableContentAuditMenu.OpenMainContentSet),
                        new DeucarianControlCenterAction(
                            "find-unauthored",
                            "Find Unauthored Visible Assets",
                            IdleAutoDefensePlayableContentAuditMenu.FindUnauthoredVisibleAssets),
                        new DeucarianControlCenterAction(
                            "runtime-audit",
                            "Generate Runtime Audit",
                            IdleAutoDefensePlayableContentAuditMenu.GenerateRuntimeContentAudit,
                            requiresConfirmation: true),
                        new DeucarianControlCenterAction(
                            "parity-report",
                            "Generate Parity Report",
                            IdleAutoDefensePlayableContentAuditMenu.GenerateAuthoringRuntimeParityReport,
                            requiresConfirmation: true)
                    },
                    searchTerms: new[] { "audit", "parity", "debug", "inspect" });
            }
        }
    }
}
