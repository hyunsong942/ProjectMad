using UnityEngine;
using UnityEngine.UI;

namespace MadLab
{
    public sealed class LabStageHUD : MonoBehaviour
    {
        [SerializeField] private LabStageController stage;
        [SerializeField] private Text healthText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text enemyText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text doorText;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text endTitle;
        [SerializeField] private Text endDescription;
        [SerializeField] private Button restartButton;
        [SerializeField] private Text restartLabel;
        private static Font sharedFont;
        private Text[] labels;
        private bool fontRefreshPending;
        private bool waitingForDeath;
        private float deathPanelDeadline;
        private LabPlayerAnimation deathAnimation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFontCache()
        {
            ReleaseFont();
            Application.quitting -= ReleaseFont;
            Application.quitting += ReleaseFont;
        }

        private static void ReleaseFont()
        {
            if (sharedFont != null) Destroy(sharedFont);
            sharedFont = null;
        }
        public Button RestartButton => restartButton;
        public GameObject EndPanel => endPanel;

        private void Awake()
        {
            // Use the Windows system font without copying or distributing a font file.
            // Keep one font atlas across scene reloads so existing glyph meshes remain valid.
            if (sharedFont == null)
            {
                sharedFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
                sharedFont.hideFlags = HideFlags.HideAndDontSave;
            }
            labels = GetComponentsInChildren<Text>(true);
            foreach (var label in labels) label.font = sharedFont;
            Font.textureRebuilt += OnFontTextureRebuilt;
            fontRefreshPending = true;
        }

        private void OnFontTextureRebuilt(Font font)
        {
            if (font == sharedFont) fontRefreshPending = true;
        }

        private void LateUpdate()
        {
            if (waitingForDeath && (deathAnimation == null || deathAnimation.DeathFinished || Time.time >= deathPanelDeadline))
            {
                waitingForDeath = false;
                endPanel.SetActive(true);
                fontRefreshPending = true;
            }
            if (!fontRefreshPending) return;
            fontRefreshPending = false;
            // Rebuild on the following frame when new result text expands the dynamic atlas.
            foreach (var label in labels)
                if (label.isActiveAndEnabled) label.FontTextureChanged();
        }

        private void Start()
        {
            stage.Changed += Refresh;
            stage.Player.Damaged += OnDamaged;
            restartButton.onClick.AddListener(stage.Restart);
            Refresh();
        }

        private void OnDamaged(float _) => Refresh();

        private void Refresh()
        {
            fontRefreshPending = true;
            var player = stage.Player;
            float fraction = Mathf.Clamp01(player.CurrentHealth / player.MaximumHealth);
            healthText.text = "체력  " + Mathf.CeilToInt(player.CurrentHealth) + " / " + Mathf.CeilToInt(player.MaximumHealth);
            healthFill.fillAmount = fraction;
            healthFill.color = fraction <= .3f ? new Color(1f, .3f, .25f) : new Color(.2f, .85f, .73f);
            enemyText.text = "남은 경비 로봇   " + stage.RemainingEnemies + " / 3";
            bool unlocked = stage.RemainingEnemies == 0;
            objectiveText.text = unlocked ? "목표  북쪽 출구로 탈출하세요" : "목표  경비 로봇 3기를 모두 처치하세요";
            doorText.text = unlocked ? "출구 개방  •  초록색 문으로 이동" : "출구 잠김  •  경비 시스템 작동 중";
            doorText.color = unlocked ? new Color(.3f, 1f, .72f) : new Color(1f, .65f, .34f);
            bool ended = stage.State != LabStageController.StageState.Running;
            if (stage.State == LabStageController.StageState.GameOver)
            {
                deathAnimation = player.GetComponent<LabPlayerAnimation>();
                bool wait = deathAnimation != null && deathAnimation.DeathDuration > 0f && !deathAnimation.DeathFinished;
                if (wait && !waitingForDeath) deathPanelDeadline = Time.time + deathAnimation.DeathDuration + 0.35f;
                waitingForDeath = wait;
            }
            else waitingForDeath = false;
            endPanel.SetActive(ended && !waitingForDeath);
            if (!ended) return;
            bool cleared = stage.State == LabStageController.StageState.Cleared;
            endTitle.text = cleared ? "연구소 탈출 성공" : "게임 오버";
            endTitle.color = cleared ? new Color(.3f, 1f, .72f) : new Color(1f, .45f, .36f);
            endDescription.text = cleared ? "경비 시스템을 정지하고 연구소를 빠져나왔습니다." :
                "경비 로봇의 공격에 쓰러졌습니다.\n붉은 공격 예고를 보고 Space로 회피해 보세요.";
            restartLabel.text = stage.IsRestarting ? "불러오는 중…" : "다시 시작";
            restartButton.interactable = !stage.IsRestarting;
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
            if (stage != null)
            {
                stage.Changed -= Refresh;
                if (stage.Player != null) stage.Player.Damaged -= OnDamaged;
                restartButton.onClick.RemoveListener(stage.Restart);
            }
        }
    }
}
