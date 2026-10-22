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
        private Font runtimeFont;
        public Button RestartButton => restartButton;
        public GameObject EndPanel => endPanel;

        private void Awake()
        {
            // Use the Windows system font without copying or distributing a font file.
            runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = runtimeFont;
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
            endPanel.SetActive(ended);
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
            if (stage != null)
            {
                stage.Changed -= Refresh;
                if (stage.Player != null) stage.Player.Damaged -= OnDamaged;
                restartButton.onClick.RemoveListener(stage.Restart);
            }
            if (runtimeFont != null) Destroy(runtimeFont);
        }
    }
}
