using UnityEngine;

namespace MadLab
{
    [DisallowMultipleComponent]
    public sealed class LabPlayerDodge : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float distance = 3f;
        [SerializeField, Min(0.01f)] private float duration = 0.75f;
        [Tooltip("Recovery time after the dodge finishes.")]
        [SerializeField, Min(0f)] private float cooldown = 0.8f;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.18f;

        // Cumulative travel follows the supplied roll: push off, roll, then stand up.
        // Integrating by position differences keeps distance independent of frame rate.
        private readonly AnimationCurve travel = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.2f, 0.192f, 1.45f, 1.45f),
            new Keyframe(0.4f, 0.553f, 1.45f, 1.45f),
            new Keyframe(0.6f, 0.786f, 0.95f, 0.95f),
            new Keyframe(0.8f, 0.937f, 0.48f, 0.48f),
            new Keyframe(1f, 1f, 0f, 0f));
        private Vector3 direction;
        private float remaining, activeDuration, activeDistance, nextReadyTime, invulnerableUntil, rechargeDuration;
        private int lastMovementFrame = -1;
        public bool IsDodging => remaining > 0f;
        public bool MovedThisFrame => lastMovementFrame == Time.frameCount;
        public float Progress => activeDuration > 0f ? Mathf.Clamp01(1f - remaining / activeDuration) : 0f;
        public float Duration => Mathf.Max(0.01f, duration);
        public bool IsInvulnerable => isActiveAndEnabled && IsDodging && Time.time < invulnerableUntil;
        public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);
        public bool IsReady => isActiveAndEnabled && !IsDodging && Time.time >= nextReadyTime;
        public float RechargeProgress => IsReady ? 1f :
            Mathf.Clamp01(1f - CooldownRemaining / Mathf.Max(0.01f, rechargeDuration));

        public bool TryStart(Vector3 desiredDirection, float entrySpeed = 0f)
        {
            if (!IsReady) return false;
            desiredDirection.y = 0f;
            direction = desiredDirection.sqrMagnitude > 0.001f
                ? desiredDirection.normalized : transform.forward;
            activeDuration = Duration;
            activeDistance = Mathf.Max(0.1f, distance);
            remaining = activeDuration;
            // Match existing movement at push-off without changing the three-metre endpoint.
            Keyframe first = travel[0];
            first.outTangent = Mathf.Clamp(entrySpeed * activeDuration / activeDistance, 0f, 1.8f);
            travel.MoveKey(0, first);
            rechargeDuration = remaining + Mathf.Max(0f, cooldown);
            nextReadyTime = Time.time + rechargeDuration;
            invulnerableUntil = Time.time + Mathf.Clamp(invulnerabilityDuration, 0f, remaining);
            return true;
        }

        public Vector3 ConsumeDisplacement(float deltaTime)
        {
            if (!IsDodging || deltaTime <= 0f) return Vector3.zero;
            float before = 1f - remaining / activeDuration;
            remaining = Mathf.Max(0f, remaining - deltaTime);
            float after = 1f - remaining / activeDuration;
            lastMovementFrame = Time.frameCount;
            return direction * (activeDistance * (travel.Evaluate(after) - travel.Evaluate(before)));
        }

        public void Cancel() { remaining = 0f; lastMovementFrame = -1; }
        private void OnDisable() { Cancel(); }
    }
}

