using UnityEngine;

namespace MadLab
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController), typeof(LabPlayerInput), typeof(LabPlayerDodge))]
    [DisallowMultipleComponent]
    public sealed class LabPlayerMotor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;
        [SerializeField, Min(0.1f)] private float gravity = 25f;
        [SerializeField, Min(0.1f)] private float groundStickSpeed = 3f;
        [SerializeField, Min(1f)] private float terminalFallSpeed = 40f;

        private CharacterController controller;
        private LabPlayerInput input;
        private LabPlayerDodge dodge;
        private LabPlayerCombat combat;
        private float verticalSpeed;
        public bool IsGrounded => controller != null && controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<LabPlayerInput>();
            dodge = GetComponent<LabPlayerDodge>();
            combat = GetComponent<LabPlayerCombat>();
            if (cameraTransform == null)
            {
                Debug.LogError("LabPlayerMotor needs a camera reference.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!controller.enabled) return;
            float dt = Time.deltaTime;
            Vector2 axes = input.Move;
            Quaternion cameraYaw = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
            Vector3 moveDirection = cameraYaw * new Vector3(axes.x, 0f, axes.y);

            if (!input.HasControl) dodge.Cancel();
            if (input.HasControl && input.DodgePressed && controller.isGrounded)
            {
                Vector3 direction = moveDirection.sqrMagnitude > 0.001f
                    ? moveDirection : transform.forward;
                if (dodge.TryStart(direction))
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            Vector3 displacement;
            if (dodge.IsDodging)
                displacement = dodge.ConsumeDisplacement(dt);
            else if (combat != null && combat.IsAttacking)
                displacement = Vector3.zero;
            else
            {
                displacement = moveDirection * (moveSpeed * dt);
                if (moveDirection.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(moveDirection, Vector3.up), turnSpeed * dt);
            }

            if (controller.isGrounded && verticalSpeed <= 0f)
                verticalSpeed = -groundStickSpeed;
            else
                verticalSpeed = Mathf.Max(verticalSpeed - gravity * dt, -terminalFallSpeed);

            displacement.y = verticalSpeed * dt;
            CollisionFlags flags = controller.Move(displacement);
            if ((flags & CollisionFlags.Below) != 0) verticalSpeed = -groundStickSpeed;
        }

        private void OnDisable()
        {
            verticalSpeed = 0f;
            if (dodge != null) dodge.Cancel();
        }
    }
}
