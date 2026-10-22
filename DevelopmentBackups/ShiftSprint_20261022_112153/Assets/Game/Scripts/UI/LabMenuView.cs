using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MadLab
{
    public sealed class LabMenuView : MonoBehaviour
    {
        public Font Font { get; private set; }
        public Button StartButton { get; private set; }
        public Button ResumeButton { get; private set; }
        public Button ExitButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Slider MasterSlider { get; private set; }
        public Slider EffectsSlider { get; private set; }
        public Slider MusicSlider { get; private set; }
        private LabGameSession session;
        private GameObject backdrop, main, pause, settings;
        private Button mainSettings, pauseSettings, settingsBack;
        private Text masterValue, effectsValue, musicValue;
        private readonly List<Text> labels = new List<Text>();
        private bool fontDirty;
        private static readonly Color Brass = new Color(0.86f, 0.66f, 0.34f);
        private static readonly Color Ink = new Color(0.055f, 0.075f, 0.085f);
        private static readonly Color Paper = new Color(0.94f, 0.91f, 0.82f);

        public void Build(LabGameSession owner)
        {
            session = owner;
            Font = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 32);
            Font.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Font.textureRebuilt += OnFontRebuilt;
            var canvasObject = new GameObject("GameMenuCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            backdrop = Rect("MenuBackdrop", canvasObject.transform, Vector2.zero, Vector2.zero).gameObject;
            Stretch((RectTransform)backdrop.transform, 0);
            var shade = backdrop.AddComponent<Image>();
            shade.color = new Color(0.025f, 0.04f, 0.05f, 0.86f);
            Label("Edition", backdrop.transform, "MAD LAB   /   STEAMWORKS", 20, Brass,
                new Vector2(0, 450), new Vector2(1000, 42));
            Label("MenuFooter", backdrop.transform, "WASD  이동     ·     마우스  시점     ·     좌클릭  콤보     ·     Space  회피     ·     Esc  일시정지",
                21, new Color(0.65f, 0.7f, 0.7f), new Vector2(0, -460), new Vector2(1640, 50));

            main = Card("MainMenu", backdrop.transform, new Vector2(780, 720));
            Label("MainEyebrow", main.transform, "EXPERIMENT  01", 20, Brass, new Vector2(0, 280), new Vector2(640, 42));
            Label("MainTitle", main.transform, "매드 사이언티스트의\n연구소", 49, Paper, new Vector2(0, 170), new Vector2(700, 150));
            Label("MainSubtitle", main.transform, "시간과 증기 사이, 탈출을 시작하세요.", 23,
                new Color(0.65f, 0.72f, 0.72f), new Vector2(0, 52), new Vector2(680, 45));
            StartButton = ButtonAt("MenuStartButton", main.transform, "게임 시작", -54, session.StartGame, true);
            mainSettings = ButtonAt("MenuSettingsButton", main.transform, "설정", -148, session.OpenSettings);
            QuitButton = ButtonAt("MenuQuitButton", main.transform, "게임 종료", -242, session.QuitGame);

            pause = Card("PauseMenu", backdrop.transform, new Vector2(780, 640));
            Label("PauseEyebrow", pause.transform, "TAKE A BREATH", 20, Brass, new Vector2(0, 242), new Vector2(640, 42));
            Label("PauseTitle", pause.transform, "일시정지", 50, Paper, new Vector2(0, 160), new Vector2(680, 80));
            Label("PauseSubtitle", pause.transform, "준비되면 이어서 진행하세요.", 23,
                new Color(0.65f, 0.72f, 0.72f), new Vector2(0, 88), new Vector2(680, 46));
            ResumeButton = ButtonAt("PauseResumeButton", pause.transform, "계속하기", -22, session.ResumeGame, true);
            pauseSettings = ButtonAt("PauseSettingsButton", pause.transform, "설정", -116, session.OpenSettings);
            ExitButton = ButtonAt("PauseExitButton", pause.transform, "나가기  ·  시작 메뉴", -210, session.ReturnToMenu);

            settings = Card("AudioSettings", backdrop.transform, new Vector2(860, 720));
            Label("SettingsEyebrow", settings.transform, "AUDIO SETTINGS", 20, Brass, new Vector2(0, 282), new Vector2(700, 42));
            Label("SettingsTitle", settings.transform, "소리 설정", 48, Paper, new Vector2(0, 210), new Vector2(720, 80));
            MasterSlider = VolumeRow("MasterVolume", "마스터 볼륨", 102, session.MasterVolume, session.SetMasterVolume, out masterValue);
            EffectsSlider = VolumeRow("EffectsVolume", "효과음", -24, session.EffectsVolume, session.SetEffectsVolume, out effectsValue);
            MusicSlider = VolumeRow("MusicVolume", "배경음", -150, session.MusicVolume, session.SetMusicVolume, out musicValue);
            settingsBack = ButtonAt("SettingsBackButton", settings.transform, "돌아가기", -270, session.CloseSettings, true);
            fontDirty = true;
        }
        public void Show(LabGameSession.ScreenState screen, bool loading)
        {
            if (backdrop == null) return;
            backdrop.SetActive(screen != LabGameSession.ScreenState.Playing || loading);
            main.SetActive(screen == LabGameSession.ScreenState.MainMenu);
            pause.SetActive(screen == LabGameSession.ScreenState.Paused);
            settings.SetActive(screen == LabGameSession.ScreenState.Settings);
            StartButton.interactable = mainSettings.interactable = QuitButton.interactable = !loading;
            if (screen == LabGameSession.ScreenState.Settings)
            {
                MasterSlider.SetValueWithoutNotify(session.MasterVolume);
                EffectsSlider.SetValueWithoutNotify(session.EffectsVolume);
                MusicSlider.SetValueWithoutNotify(session.MusicVolume);
                masterValue.text = Percent(session.MasterVolume);
                effectsValue.text = Percent(session.EffectsVolume);
                musicValue.text = Percent(session.MusicVolume);
            }
            var selected = screen == LabGameSession.ScreenState.MainMenu ? StartButton :
                screen == LabGameSession.ScreenState.Paused ? ResumeButton :
                screen == LabGameSession.ScreenState.Settings ? (Selectable)MasterSlider : null;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(!loading && selected != null ? selected.gameObject : null);
            fontDirty = true;
        }
        private Slider VolumeRow(string name, string title, float y, float value, Action<float> setter, out Text number)
        {
            var label = Label(name + "Label", settings.transform, title, 27, Paper, new Vector2(-138, y), new Vector2(380, 46));
            label.alignment = TextAnchor.MiddleLeft;
            Text numberLabel = Label(name + "Percent", settings.transform, Percent(value), 24, Brass,
                new Vector2(282, y), new Vector2(120, 46));
            numberLabel.alignment = TextAnchor.MiddleRight;
            number = numberLabel;
            var root = Rect(name + "Slider", settings.transform, new Vector2(0, y - 44), new Vector2(650, 44));
            var hitArea = root.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;
            var rail = Rect("Track", root, Vector2.zero, new Vector2(650, 8));
            rail.gameObject.AddComponent<Image>().color = new Color(.16f, .21f, .23f);
            var fillArea = Rect("FillArea", root, Vector2.zero, new Vector2(626, 8));
            var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.zero); Stretch(fill, 0);
            fill.gameObject.AddComponent<Image>().color = Brass;
            var handleArea = Rect("HandleArea", root, Vector2.zero, new Vector2(626, 30));
            var handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(22, 0));
            var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Paper;
            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = 0; slider.maxValue = 1; slider.value = value;
            slider.onValueChanged.AddListener(v => { setter(v); numberLabel.text = Percent(v); fontDirty = true; });
            return slider;
        }
        private static string Percent(float value) => Mathf.RoundToInt(value * 100) + "%";
        private GameObject Card(string name, Transform parent, Vector2 size)
        {
            var root = Rect(name, parent, Vector2.zero, size);
            root.gameObject.AddComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, .97f);
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Brass.r, Brass.g, Brass.b, .6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var stripe = Rect("BrassRule", root, new Vector2(0, size.y / 2 - 2), new Vector2(size.x, 4));
            stripe.gameObject.AddComponent<Image>().color = Brass;
            return root.gameObject;
        }
        private Button ButtonAt(string name, Transform parent, string title, float y, Action clicked, bool primary = false)
        {
            var root = Rect(name, parent, new Vector2(0, y), new Vector2(610, 72));
            var image = root.gameObject.AddComponent<Image>();
            image.color = primary ? Brass : new Color(.12f, .17f, .19f);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.selectedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(.75f, .8f, .8f);
            button.colors = colors;
            button.onClick.AddListener(() => clicked());
            Label(name + "Label", root, title, 28, primary ? Ink : Paper, Vector2.zero, new Vector2(580, 66));
            return button;
        }
        private Text Label(string name, Transform parent, string value, int size, Color color, Vector2 position, Vector2 bounds)
        {
            var root = Rect(name, parent, position, bounds);
            var text = root.gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = size; text.color = color;
            text.text = value; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            labels.Add(text);
            return text;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * margin; rect.offsetMax = -Vector2.one * margin;
        }
        private void OnFontRebuilt(Font font) { if (font == Font) fontDirty = true; }
        private void LateUpdate()
        {
            if (!fontDirty) return;
            fontDirty = false;
            foreach (var label in labels) if (label != null && label.isActiveAndEnabled) label.FontTextureChanged();
        }
        private void OnDestroy()
        {
            UnityEngine.Font.textureRebuilt -= OnFontRebuilt;
            if (Font != null) Destroy(Font);
        }
    }
}
