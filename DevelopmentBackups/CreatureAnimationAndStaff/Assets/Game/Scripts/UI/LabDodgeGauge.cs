using UnityEngine;
using UnityEngine.UI;

namespace MadLab
{
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public sealed class LabDodgeGauge : MonoBehaviour
    {
        [SerializeField] private LabPlayerDodge dodge;
        [SerializeField] private Image fill;
        [SerializeField] private Image frame;
        [SerializeField] private Image readyMarker;
        private bool wasReady;
        private float flashUntil;
        private static readonly Color ReadyColor = new Color(1f, 0.76f, 0.28f);
        private static readonly Color ChargingColor = new Color(0.76f, 0.52f, 0.18f);
        private static readonly Color InactiveColor = new Color(0.24f, 0.27f, 0.28f);

        private void Awake()
        {
            if (dodge == null || fill == null || frame == null || readyMarker == null)
            {
                Debug.LogError("LabDodgeGauge needs its player and image references.", this);
                enabled = false;
                return;
            }
            wasReady = dodge.IsReady;
        }

        private void LateUpdate()
        {
            bool available = dodge.isActiveAndEnabled;
            bool ready = available && dodge.IsReady;
            float progress = available ? dodge.RechargeProgress : 0f;
            // The bar and TryStart read the same cooldown; no separate UI timer can drift.
            fill.rectTransform.anchorMax = new Vector2(ready ? 1f : Mathf.Min(progress, 0.995f), 1f);
            if (ready && !wasReady) flashUntil = Time.unscaledTime + 0.25f;
            float flash = ready ? Mathf.Clamp01((flashUntil - Time.unscaledTime) / 0.25f) : 0f;
            fill.color = ready ? Color.Lerp(ReadyColor, new Color(1f, 0.95f, 0.72f), flash) : ChargingColor;
            frame.color = !available ? InactiveColor : ready
                ? Color.Lerp(new Color(0.68f, 0.47f, 0.16f), ReadyColor, flash)
                : new Color(0.32f, 0.28f, 0.20f);
            readyMarker.enabled = ready;
            readyMarker.color = Color.Lerp(ReadyColor, Color.white, flash);
            wasReady = ready;
        }
    }
}
