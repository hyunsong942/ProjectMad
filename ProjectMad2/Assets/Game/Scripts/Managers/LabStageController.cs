using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MadLab
{
    public sealed class LabStageController : MonoBehaviour
    {
        public enum StageState { Running, Cleared, GameOver }
        [SerializeField] private LabHealth player;
        [SerializeField] private LabHealth[] guards;
        [SerializeField] private LabExitDoor exitDoor;
        [SerializeField] private LabBossGolem boss;
        public LabBossGolem Boss => boss;
        public bool ExitReady => RemainingEnemies == 0 && (boss == null || boss.IsDefeated);
        private readonly HashSet<LabHealth> livingGuards = new HashSet<LabHealth>();
        public StageState State { get; private set; }
        public int RemainingEnemies => livingGuards.Count;
        public bool IsRestarting { get; private set; }
        public LabHealth Player => player;
        public event Action Changed;

        private void Start()
        {
            State = StageState.Running;
            exitDoor.SetUnlocked(false);
            player.Died += OnPlayerDied;
            if (boss != null) boss.Changed += OnBossChanged;
            foreach (var guard in guards)
                if (guard != null && !guard.IsDead && livingGuards.Add(guard))
                    guard.Died += OnGuardDied;
            if (player.IsDead) EndStage(StageState.GameOver);
            else
            {
                if (RemainingEnemies == 0) AdvanceEncounter();
                Changed?.Invoke();
            }
        }

        private void OnGuardDied(LabHealth guard)
        {
            if (!livingGuards.Remove(guard)) return;
            guard.Died -= OnGuardDied;
            if (State == StageState.Running && RemainingEnemies == 0)
                AdvanceEncounter();
            Changed?.Invoke();
        }

        private void AdvanceEncounter()
        {
            if (boss != null && !boss.HasAwakened) boss.Awaken(player);
            exitDoor.SetUnlocked(ExitReady);
        }

        private void OnBossChanged()
        {
            if (State == StageState.Running && !player.IsDead) exitDoor.SetUnlocked(ExitReady);
            Changed?.Invoke();
        }

        private void OnPlayerDied(LabHealth _) => EndStage(StageState.GameOver);

        public bool TryExit(LabHealth entrant)
        {
            if (LabGameSession.GameplayBlocked || State != StageState.Running || entrant != player || player.IsDead ||
                !ExitReady || !exitDoor.IsUnlocked) return false;
            EndStage(StageState.Cleared);
            return true;
        }

        private void EndStage(StageState result)
        {
            if (State != StageState.Running) return;
            State = result;
            player.GetComponent<LabPlayerDodge>().Cancel();
            player.GetComponent<LabPlayerMotor>().enabled = false;
            player.GetComponent<LabPlayerDodge>().enabled = false;
            player.GetComponent<LabPlayerCombat>().enabled = false;
            player.GetComponent<LabPlayerInput>().enabled = false;
            player.GetComponent<CharacterController>().enabled = false;
            if (result == StageState.Cleared) player.enabled = false;
            foreach (var guard in guards)
                if (guard != null) guard.GetComponent<LabEnemyGuard>().enabled = false;
            if (boss != null) boss.Suspend();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Changed?.Invoke();
        }

        public void Restart()
        {
            if (LabGameSession.GameplayBlocked || State == StageState.Running || IsRestarting) return;
            IsRestarting = true;
            Changed?.Invoke();
            try
            {
                SceneManager.LoadSceneAsync(gameObject.scene.path, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                IsRestarting = false;
                Changed?.Invoke();
                Debug.LogException(exception);
            }
        }

        private void OnDestroy()
        {
            if (player != null) player.Died -= OnPlayerDied;
            if (boss != null) boss.Changed -= OnBossChanged;
            foreach (var guard in livingGuards)
                if (guard != null) guard.Died -= OnGuardDied;
        }
    }
}
