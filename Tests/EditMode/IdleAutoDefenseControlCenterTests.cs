using System.Linq;
using Deucarian.Editor;
using Deucarian.TemplateGameIdleAutoDefense.Editor;
using NUnit.Framework;

namespace Deucarian.TemplateGameIdleAutoDefense.Tests
{
    public sealed class IdleAutoDefenseControlCenterTests
    {
        [Test]
        public void ContributionKeepsSetupAuditsAndInspectionInAuthoring()
        {
            DeucarianControlCenterSnapshot snapshot =
                DeucarianControlCenterSnapshotBuilder.Capture();
            DeucarianToolDescriptor tool = snapshot.Tools.Single(candidate =>
                candidate.Id == "deucarian.template.idle-auto-defense");
            DeucarianControlCenterCard setup = snapshot
                .GetCards(DeucarianControlCenterArea.Authoring)
                .Single(candidate =>
                    candidate.Id == "com.deucarian.template.game.idle-auto-defense.authoring");
            DeucarianControlCenterCard audits = snapshot
                .GetCards(DeucarianControlCenterArea.Authoring)
                .Single(candidate =>
                    candidate.Id == "com.deucarian.template.game.idle-auto-defense.developer");

            Assert.That(tool.Area, Is.EqualTo(DeucarianControlCenterArea.Authoring));
            Assert.That(setup.Area, Is.EqualTo(DeucarianControlCenterArea.Authoring));
            Assert.That(audits.Area, Is.EqualTo(DeucarianControlCenterArea.Authoring));
            CollectionAssert.AreEqual(
                new[] { "create", "validate-authored", "docs" },
                setup.Actions.Select(action => action.Id).ToArray());
            CollectionAssert.AreEqual(
                new[]
                {
                    "validate-playable",
                    "open-content-set",
                    "find-unauthored",
                    "runtime-audit",
                    "parity-report"
                },
                audits.Actions.Select(action => action.Id).ToArray());
        }

        [Test]
        public void SetupStatusUsesTheDomainOwnedMainContentSetResolver()
        {
            DeucarianControlCenterCard setup =
                DeucarianControlCenterSnapshotBuilder.Capture()
                    .GetCards(DeucarianControlCenterArea.Authoring)
                    .Single(candidate =>
                        candidate.Id == "com.deucarian.template.game.idle-auto-defense.authoring");

            Assert.That(
                setup.Status,
                Is.EqualTo(IdleAutoDefensePlayableContentAuditMenu.HasMainContentSet()
                    ? DeucarianControlCenterStatus.Success
                    : DeucarianControlCenterStatus.Info));
        }
    }
}