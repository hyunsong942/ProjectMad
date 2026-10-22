using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class LabThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private LabPlayerInput input;
        [SerializeField, Min(0.1f)] private float distance = 5.5f;
        [SerializeField, Min(0f)] private float pivotHeight = 1.4f;
        [SerializeField, Min(0.001f)] private float horizontalSensitivity = 0.12f;
        [SerializeField, Min(0.001f)] private float verticalSensitivity = 0.12f;
        [SerializeField] private float minimumPitch = -20f;
        [SerializeField] private float maximumPitch = 65f;
        [SerializeField] private float initialPitch = 20f;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.22f;
        [SerializeField, Min(0.1f)] private float recoverySpeed = 8f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private float yaw, pitch, currentDistance;
        public float Yaw => yaw;
        public float Pitch => pitch;

        private void Start()
        {
            if (target == null || input == null)
            {
                Debug.LogError("LabThirdPersonCamera needs target and input references.", this);
                enabled = false;
                return;
            }
            yaw = target.eulerAngles.y;
            pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
            currentDistance = distance;
        }

        private void LateUpdate()
        {
            if (target == null || input == null) return;
            // Mouse delta is already per-frame; multiplying it by deltaTime would change sensitivity with FPS.
            yaw = Mathf.Repeat(yaw + input.Look.x * horizontalSensitivity, 360f);
            pitch = Mathf.Clamp(pitch - input.Look.y * verticalSensitivity, minimumPitch, maximumPitch);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Vector3 backwards = rotation * Vector3.back;
            float allowedDistance = distance;
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, backwards, hits,
                distance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform == target || hits[i].transform.IsChildOf(target)) continue;
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0.05f, hits[i].distance - 0.05f));
            }
            currentDistance = allowedDistance < currentDistance
                ? allowedDistance : Mathf.MoveTowards(currentDistance, allowedDistance, recoverySpeed * Time.deltaTime);
            transform.SetPositionAndRotation(pivot + backwards * currentDistance, rotation);
        }

        private void OnValidate()
        {
            maximumPitch = Mathf.Max(minimumPitch, maximumPitch);
        }
    }
}
