using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MadLab
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class LabGameSession : MonoBehaviour
    {
        public enum ScreenState { MainMenu, Playing, Paused, Settings }
        public static LabGameSession Instance { get; private set; }
        public static event Action VolumeChanged;
        public static bool GameplayBlocked => Instance != null && (Instance.Screen != ScreenState.Playing || Instance.IsLoading);
        public const string MasterKey = "MadLab.Audio.Master";
        public const string EffectsKey = "MadLab.Audio.Effects";
        public const string MusicKey = "MadLab.Audio.Music";
        [SerializeField] private AudioClip backgroundMusic;
        public ScreenState Screen { get; private set; } = ScreenState.MainMenu;
        public bool IsLoading { get; private set; }
        public float MasterVolume { get; private set; }
        public float EffectsVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public AudioSource MusicSource => music;
        public LabMenuView Menu { get; private set; }
        public LabDamageNumbers DamageNumbers { get; private set; }
        private ScreenState settingsReturn;
        private AudioSource music;
        private LabPlayerInput input;
        private LabStageController stage;
        private Canvas hud;
        private InputAction escape;
        private string scenePath;
        private float previousScale, previousVolume;
        private bool previousAudioPause;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; VolumeChanged = null; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            previousScale = Time.timeScale;
            previousVolume = AudioListener.volume;
            previousAudioPause = AudioListener.pause;
            MasterVolume = ReadVolume(MasterKey, 1f);
            EffectsVolume = ReadVolume(EffectsKey, 0.8f);
            MusicVolume = ReadVolume(MusicKey, 0.35f);
            music = GetComponent<AudioSource>();
            music.clip = backgroundMusic;
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.ignoreListenerPause = true;
            music.priority = 32;
            ApplyVolumes();
            if (music.clip != null) music.Play();
            else Debug.LogError("LabGameSession needs background music.", this);
            Menu = gameObject.AddComponent<LabMenuView>();
            Menu.Build(this);
            DamageNumbers = gameObject.AddComponent<LabDamageNumbers>();
            DamageNumbers.Build(Menu.Font);
            escape = new InputAction("MenuEscape", InputActionType.Button, "<Keyboard>/escape");
            escape.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyScreen();
        }
        private static float ReadVolume(string key, float fallback)
        {
            float value = PlayerPrefs.GetFloat(key, fallback);
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp01(value);
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Instance != this) return;
            stage = FindFirstObjectByType<LabStageController>();
            input = stage != null ? stage.Player.GetComponent<LabPlayerInput>() : null;
            var stageHud = FindFirstObjectByType<LabStageHUD>();
            hud = stageHud != null ? stageHud.GetComponent<Canvas>() : null;
            scenePath = scene.path;
            IsLoading = false;
            DamageNumbers.Clear();
            ApplyScreen();
        }
        private void Update()
        {
            if (IsLoading || (stage != null && stage.IsRestarting)) return;
            if (escape != null && escape.WasPressedThisFrame())
            {
                if (Screen == ScreenState.Settings) CloseSettings();
                else if (Screen == ScreenState.Paused) ResumeGame();
                else if (Screen == ScreenState.Playing) PauseGame();
            }
        }
        public void StartGame()
        {
            if (Screen != ScreenState.MainMenu || IsLoading) return;
            Screen = ScreenState.Playing;
            ApplyScreen();
        }
        public void PauseGame()
        {
            if (Screen != ScreenState.Playing || IsLoading) return;
            Screen = ScreenState.Paused;
            ApplyScreen();
        }
        public void ResumeGame()
        {
            if (Screen != ScreenState.Paused || IsLoading) return;
            Screen = ScreenState.Playing;
            ApplyScreen();
        }
        public void OpenSettings()
        {
            if (IsLoading || (Screen != ScreenState.MainMenu && Screen != ScreenState.Paused)) return;
            settingsReturn = Screen;
            Screen = ScreenState.Settings;
            ApplyScreen();
        }
        public void CloseSettings()
        {
            if (Screen != ScreenState.Settings) return;
            PlayerPrefs.Save();
            Screen = settingsReturn;
            ApplyScreen();
        }
        public void ReturnToMenu()
        {
            if (IsLoading || Screen != ScreenState.Paused || string.IsNullOrEmpty(scenePath)) return;
            IsLoading = true;
            Screen = ScreenState.MainMenu;
            DamageNumbers.Clear();
            ApplyScreen();
            try
            {
                var operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("Scene reload did not start.");
            }
            catch (Exception exception)
            {
                IsLoading = false;
                Screen = ScreenState.Paused;
                ApplyScreen();
                Debug.LogException(exception);
            }
        }
        public void QuitGame()
        {
            if (Screen != ScreenState.MainMenu || IsLoading) return;
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public void SetMasterVolume(float value) { MasterVolume = Sanitize(value); SaveVolumes(); }
        public void SetEffectsVolume(float value) { EffectsVolume = Sanitize(value); SaveVolumes(); }
        public void SetMusicVolume(float value) { MusicVolume = Sanitize(value); SaveVolumes(); }
        private static float Sanitize(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);
        private void SaveVolumes()
        {
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            PlayerPrefs.SetFloat(EffectsKey, EffectsVolume);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            ApplyVolumes();
        }
        private void ApplyVolumes()
        {
            AudioListener.volume = MasterVolume;
            if (music != null) music.volume = MusicVolume;
            VolumeChanged?.Invoke();
        }
        private void ApplyScreen()
        {
            bool playing = Screen == ScreenState.Playing && !IsLoading;
            Time.timeScale = playing ? 1f : 0f;
            AudioListener.pause = !playing;
            if (hud != null) hud.enabled = playing;
            if (input != null)
            {
                if (playing && input.isActiveAndEnabled) input.CaptureCursor();
                else input.ReleaseCursor();
            }
            if (!playing || input == null || !input.isActiveAndEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Menu != null) Menu.Show(Screen, IsLoading);
            if (DamageNumbers != null) DamageNumbers.SetVisible(playing);
        }
        private void OnApplicationQuit() { if (Instance == this) PlayerPrefs.Save(); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (escape != null) { escape.Disable(); escape.Dispose(); }
            PlayerPrefs.Save();
            Time.timeScale = previousScale;
            AudioListener.volume = previousVolume;
            AudioListener.pause = previousAudioPause;
            Instance = null;
        }
    }
}
