using UnityEngine;

namespace MadLab
{
    public static class LabMeleeQuery
    {
        private static readonly RaycastHit[] SightHits = new RaycastHit[64];

        public static bool HasLineOfSight(Transform attacker, Transform target)
        {
            Vector3 origin = attacker.position + Vector3.up * 0.9f;
            Vector3 destination = target.position + Vector3.up * 0.9f;
            Vector3 difference = destination - origin;
            float length = difference.magnitude;
            if (length < 0.001f) return true;
            int count = Physics.RaycastNonAlloc(origin, difference / length, SightHits,
                length, ~0, QueryTriggerInteraction.Ignore);
            if (count == SightHits.Length) return false; // A saturated query must not permit a hit through a wall.
            for (int i = 0; i < count; i++)
            {
                Transform hit = SightHits[i].transform;
                if (hit == attacker || hit.IsChildOf(attacker) || hit == target || hit.IsChildOf(target)) continue;
                return false;
            }
            return true;
        }

        public static bool CanReach(Transform attacker, Transform target, float range, float angle)
        {
            Vector3 difference = target.position - attacker.position;
            if (Mathf.Abs(difference.y) > 1.5f) return false;
            difference.y = 0f;
            if (difference.sqrMagnitude > range * range) return false;
            if (difference.sqrMagnitude > 0.001f &&
                Vector3.Dot(attacker.forward, difference.normalized) < Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad))
                return false;
            return HasLineOfSight(attacker, target);
        }
    }
}
