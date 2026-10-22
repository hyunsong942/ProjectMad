using UnityEngine;

namespace MadLab
{
    [RequireComponent(typeof(LabHealth))]
    [DisallowMultipleComponent]
    public sealed class LabCombatFeedback : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private GameObject swingVisual;
        [SerializeField] private GameObject telegraphVisual;
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;
        private LabHealth health;
        private MaterialPropertyBlock block;
        private Color[] colors;
        private float flashUntil, swingUntil;

        private void Awake()
        {
            health = GetComponent<LabHealth>();
            block = new MaterialPropertyBlock();
            colors = new Color[bodyRenderers.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = bodyRenderers[i].sharedMaterial.GetColor("_BaseColor");
        }
        private void OnEnable() { health.Damaged += OnDamaged; health.Died += OnDeath; }
        private void OnDisable()
        {
            if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDeath; }
            SetTelegraph(false);
            if (swingVisual != null) swingVisual.SetActive(false);
        }
        private void OnDamaged(float amount) { flashUntil = Time.time + hitFlashDuration; }
        private void OnDeath(LabHealth dead) { SetTelegraph(false); swingUntil = 0f; }
        public void PlaySwing() { swingUntil = Time.time + 0.14f; }
        public void SetTelegraph(bool active)
        {
            if (telegraphVisual != null) telegraphVisual.SetActive(active);
        }
        private void LateUpdate()
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] == null) continue;
                bodyRenderers[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", health.IsDead ? new Color(0.13f, 0.14f, 0.15f) :
                    Time.time < flashUntil ? new Color(1f, 0.15f, 0.08f) : colors[i]);
                bodyRenderers[i].SetPropertyBlock(block);
            }
            if (swingVisual != null) swingVisual.SetActive(!health.IsDead && Time.time < swingUntil);
        }
    }
}
