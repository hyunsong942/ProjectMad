using UnityEngine;
using UnityEngine.InputSystem;

namespace MadLab
{
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class LabPlayerInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        private InputActionAsset runtimeActions;
        private InputAction move, look, dodge, releaseCursor, captureCursor;
        public bool HasControl { get; private set; }
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool DodgePressed { get; private set; }

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("LabPlayerInput needs the LabControls input asset.", this);
                enabled = false;
                return;
            }
            runtimeActions = Instantiate(inputActions);
            move = runtimeActions.FindAction("Gameplay/Move", true);
            look = runtimeActions.FindAction("Gameplay/Look", true);
            dodge = runtimeActions.FindAction("Gameplay/Dodge", true);
            releaseCursor = runtimeActions.FindAction("Gameplay/ReleaseCursor", true);
            captureCursor = runtimeActions.FindAction("Gameplay/CaptureCursor", true);
        }

        private void OnEnable()
        {
            if (runtimeActions == null) return;
            runtimeActions.Enable();
            CaptureCursor();
        }

        private void Update()
        {
            Move = Look = Vector2.zero;
            DodgePressed = false;
            if (runtimeActions == null) return;
            if (releaseCursor.WasPressedThisFrame())
            {
                ReleaseCursor();
                return;
            }
            if (!HasControl)
            {
                if (captureCursor.WasPressedThisFrame()) CaptureCursor();
                return; // The click that captures the cursor does not perform a game action.
            }
            Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            Look = look.ReadValue<Vector2>();
            DodgePressed = dodge.WasPressedThisFrame();
        }

        public void CaptureCursor()
        {
            HasControl = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ReleaseCursor()
        {
            HasControl = false;
            Move = Look = Vector2.zero;
            DodgePressed = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) ReleaseCursor();
        }

        private void OnDisable()
        {
            if (runtimeActions != null) runtimeActions.Disable();
            ReleaseCursor();
        }

        private void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }
    }
}
