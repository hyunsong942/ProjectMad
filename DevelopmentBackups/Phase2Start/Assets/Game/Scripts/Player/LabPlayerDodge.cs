using UnityEngine;

namespace MadLab
{
    [DisallowMultipleComponent]
    public sealed class LabPlayerDodge : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float distance = 3f;
        [SerializeField, Min(0.01f)] private float duration = 0.3f;
        [Tooltip("Recovery time after the dodge finishes.")]
        [SerializeField, Min(0f)] private float cooldown = 0.8f;

        private Vector3 direction;
        private float remaining, speed, nextReadyTime;
        public bool IsDodging => remaining > 0f;
        public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

        public bool TryStart(Vector3 desiredDirection)
        {
            if (IsDodging || Time.time < nextReadyTime) return false;
            desiredDirection.y = 0f;
            direction = desiredDirection.sqrMagnitude > 0.001f
                ? desiredDirection.normalized : transform.forward;
            remaining = Mathf.Max(0.01f, duration);
            speed = Mathf.Max(0.1f, distance) / remaining;
            nextReadyTime = Time.time + remaining + Mathf.Max(0f, cooldown);
            return true;
        }

        // Returns displacement, not velocity. The last frame is clamped to the remaining time.
        // Only LabPlayerMotor moves the CharacterController, so collision stays centralized.
        public Vector3 ConsumeDisplacement(float deltaTime)
        {
            if (!IsDodging || deltaTime <= 0f) return Vector3.zero;
            float step = Mathf.Min(deltaTime, remaining);
            remaining = Mathf.Max(0f, remaining - step);
            return direction * (speed * step);
        }

        public void Cancel() { remaining = 0f; }
        private void OnDisable() { Cancel(); }
    }
}
