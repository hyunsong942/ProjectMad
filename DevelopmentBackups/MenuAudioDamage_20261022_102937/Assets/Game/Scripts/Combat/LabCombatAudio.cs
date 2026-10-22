using UnityEngine;

namespace MadLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource), typeof(LabHealth))]
    public sealed class LabCombatAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] firstSwings;
        [SerializeField] private AudioClip[] secondSwings;
        [SerializeField] private AudioClip[] finisherSwings;
        [SerializeField] private AudioClip robotAttack;
        private AudioSource source;
        private LabHealth health;
        private readonly int[] variant = new int[3];
        public int SwingPlayCount { get; private set; }
        public int RobotPlayCount { get; private set; }
        public int LastComboStep { get; private set; }
        public AudioClip LastPlayedClip { get; private set; }
        public float LastPlayedAt { get; private set; }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            health = GetComponent<LabHealth>();
            source.playOnAwake = false;
            source.loop = false;
        }
        private void OnEnable() { health.Died += OnDeath; }
        private void OnDisable()
        {
            if (health != null) health.Died -= OnDeath;
            StopAction();
        }
        public void PlayPlayerSwing(int step)
        {
            if (!isActiveAndEnabled || health.IsDead || step < 1 || step > 3) return;
            AudioClip[] bank = step == 1 ? firstSwings : step == 2 ? secondSwings : finisherSwings;
            if (bank == null || bank.Length == 0) return;
            AudioClip clip = bank[variant[step - 1]++ % bank.Length];
            if (clip == null) return;
            source.Stop();
            source.pitch = step == 3 ? 0.94f : step == 2 ? 1.03f : 1f;
            source.PlayOneShot(clip, step == 3 ? 0.78f : 0.64f);
            LastPlayedClip = clip;
            LastPlayedAt = Time.time;
            LastComboStep = step;
            SwingPlayCount++;
        }
        public void PlayRobotAttack()
        {
            if (!isActiveAndEnabled || health.IsDead || robotAttack == null) return;
            source.Stop();
            source.pitch = 1f;
            source.PlayOneShot(robotAttack, 0.72f);
            LastPlayedClip = robotAttack;
            LastPlayedAt = Time.time;
            RobotPlayCount++;
        }
        public void StopAction() { if (source != null) source.Stop(); }
        private void OnDeath(LabHealth _) { StopAction(); }
    }
}
