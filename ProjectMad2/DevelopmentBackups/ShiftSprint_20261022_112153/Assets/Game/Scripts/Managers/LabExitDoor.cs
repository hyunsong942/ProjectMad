using UnityEngine;

namespace MadLab
{
    public sealed class LabExitDoor : MonoBehaviour
    {
        [SerializeField] private LabStageController stage;
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Collider barrier;
        [SerializeField] private Renderer indicator;
        [SerializeField] private float liftDistance = 3.4f;
        [SerializeField] private float openingSpeed = 3f;
        private Vector3 closedPosition;
        private MaterialPropertyBlock block;
        public bool IsUnlocked { get; private set; }

        private void Awake()
        {
            closedPosition = doorPanel.localPosition;
            block = new MaterialPropertyBlock();
        }

        public void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
            barrier.enabled = !unlocked;
            indicator.GetPropertyBlock(block);
            block.SetColor("_BaseColor", unlocked ? new Color(.1f, .95f, .6f) : new Color(1f, .16f, .08f));
            indicator.SetPropertyBlock(block);
            if (!unlocked) doorPanel.localPosition = closedPosition;
        }

        private void Update()
        {
            if (IsUnlocked)
                doorPanel.localPosition = Vector3.MoveTowards(doorPanel.localPosition,
                    closedPosition + Vector3.up * liftDistance, openingSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other) => CheckEntrant(other);
        private void OnTriggerStay(Collider other) => CheckEntrant(other);
        private void CheckEntrant(Collider other)
        {
            if (!IsUnlocked) return;
            var entrant = other.GetComponentInParent<LabHealth>();
            if (entrant != null) stage.TryExit(entrant);
        }
    }
}
