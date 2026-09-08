using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal static class IdleAutoDefensePresentationGeometry
    {
        internal static Vector3 EnemyAimPosition(Vector3 enemyPosition) => enemyPosition + Vector3.up * 0.35f;
        internal static Vector3 TowerMuzzlePosition(Vector3 objectivePosition) => objectivePosition + Vector3.up * 0.75f;
    }
}
