using Deucarian.Attacks.Authoring;
using Deucarian.AutoDefense;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    public partial class IdleAutoDefenseTemplateController
    {
        private void EmitEnemyPresentationEvent(AutoDefenseEnemySnapshot enemy, EnemyPresentationEventKind eventKind) => WorldPresentation.Events.EmitEnemyPresentationEvent(enemy, eventKind);

        private void EmitAttackEvent(AttackDefinitionAsset attack, AttackPresentationEventKind eventKind, Vector3 eventPosition, long targetEnemyId = 0) => WorldPresentation.Events.EmitAttackEvent(attack, eventKind, eventPosition, targetEnemyId);

        private void EmitAttackTracer(Vector3 origin, Vector3 destination, Color color) => WorldPresentation.Events.EmitAttackTracer(origin, destination, color);

        internal Vector3 ResolveTowerMuzzlePosition(AttackDefinitionAsset attack) => WorldPresentation.Targets.ResolveTowerMuzzlePosition(attack);

        private void PlayWeaponFirePresentation(AttackDefinitionAsset attack, Vector3 targetPosition) => WorldPresentation.Targets.PlayWeaponFirePresentation(attack, targetPosition);

        private Color ResolveAttackColor(AttackDefinitionAsset attack) => WorldPresentation.Targets.ResolveAttackColor(attack);

        private IdleAutoDefenseWeaponVisualBinding CreateWeaponPresentation(
            string weaponId,
            string attackId,
            bool enabled) => WorldPresentation.Arena.CreateWeaponPresentation(weaponId, attackId, enabled);

        private void ClearActiveBeamVisuals() => WorldPresentation.Beams.ClearActiveBeamVisuals();
    }
}
