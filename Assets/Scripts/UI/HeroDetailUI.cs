using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HeroDispatch
{
    // ==================== 英雄详情面板 ====================
    public class HeroDetailUI : MonoBehaviour
    {
        public GameObject panelRoot;
        public Image portraitImage;
        public TMP_Text codenameText;
        public TMP_Text displayNameText;
        public TMP_Text powerText;
        public TMP_Text stateText;
        public TMP_Text fatigueText;
        public TMP_Text mentalText;
        public TMP_Text quirkNameText;
        public TMP_Text quirkDescText;
        public TMP_Text missionsText;
        public TMP_Text salaryText;
        public Image[] skillIcons;
        public TMP_Text[] skillNameTexts;
        public TMP_Text[] skillDescTexts;
        public Transform relationContainer;
        public GameObject relationItemPrefab;
        public Button restButton;
        public Button closeButton;
        public Button playVoiceButton;

        string _heroId;

        void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (restButton != null) restButton.onClick.AddListener(DoRest);
            if (playVoiceButton != null) playVoiceButton.onClick.AddListener(PlayVoice);
        }

        void PlayVoice()
        {
            if (_heroId == null) return;
            var h = GameManager.Instance.state.FindHero(_heroId);
            if (h != null)
            {
                AudioManager.Instance?.PlayHeroVoice(_heroId, VoiceType.Selection);
                if (h.data.skills != null && h.data.skills.Length > 1)
                    AudioManager.Instance?.PlayHeroVoice(_heroId, VoiceType.Skill);
            }
        }

        public void Show(string heroId)
        {
            var g = GameManager.Instance?.state;
            var h = g?.FindHero(heroId);
            if (h == null) return;
            _heroId = heroId;

            if (portraitImage != null)
                portraitImage.sprite = h.status == HeroStatus.Injured && h.data.portraitInjured != null
                    ? h.data.portraitInjured : h.data.portraitNormal;
            if (codenameText != null)
            {
                codenameText.text = h.emoji + " " + h.codename;
                codenameText.color = h.data.color;
            }
            if (displayNameText != null) displayNameText.text = h.displayName + " · Lv." + h.level + "  " + h.xp + "/" + (h.level * 100) + "xp";
            if (powerText != null) powerText.text = "超能力: " + h.data.powerName;
            if (stateText != null)
            {
                stateText.text = h.status switch
                {
                    HeroStatus.Ready => "✓ 待命", HeroStatus.OnMission => "⚔ 任务中",
                    HeroStatus.Resting => "💤 休息中", HeroStatus.Injured => "🏥 恢复中",
                    HeroStatus.Returning => "🔙 返回中", _ => "—"
                };
                stateText.color = h.status == HeroStatus.Ready ? Theme.Success :
                    h.status == HeroStatus.OnMission ? Theme.Orange : Theme.TextDim;
            }
            if (fatigueText != null)
            {
                fatigueText.text = "疲劳: " + h.fatigue.ToString("F0");
                fatigueText.color = h.fatigue > 70 ? Theme.Danger : h.fatigue > 40 ? Theme.Warning : Theme.Success;
            }
            if (mentalText != null)
            {
                mentalText.text = "心理: " + h.mental.ToString("F0");
                mentalText.color = h.mental < 40 ? Theme.Danger : h.mental < 60 ? Theme.Warning : Theme.Success;
            }
            if (quirkNameText != null) quirkNameText.text = "⚠ " + h.data.quirkName;
            if (quirkDescText != null) quirkDescText.text = h.data.quirkDesc;
            if (missionsText != null) missionsText.text = "任务: 完成" + h.missionsCompleted + " / 失败" + h.missionsFailed;
            if (salaryText != null) salaryText.text = "日薪: ¥" + h.data.salary;

            // 技能
            if (h.data.skills != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (skillIcons != null && i < skillIcons.Length && skillIcons[i] != null)
                        skillIcons[i].sprite = h.data.skills[i]?.icon;
                    if (skillNameTexts != null && i < skillNameTexts.Length && skillNameTexts[i] != null)
                        skillNameTexts[i].text = "[" + (i == 0 ? "被动" : i == 1 ? "主动" : "终极") + "] " + h.data.skills[i]?.name;
                    if (skillDescTexts != null && i < skillDescTexts.Length && skillDescTexts[i] != null)
                        skillDescTexts[i].text = h.data.skills[i]?.desc;
                }
            }

            // 关系
            if (relationContainer != null)
            {
                foreach (Transform c in relationContainer) Destroy(c.gameObject);
                var hm = HeroManager.Instance;
                foreach (var other in g.heroes)
                {
                    if (other.heroId == heroId) continue;
                    var rel = hm.GetRelation(g, heroId, other.heroId);
                    if (rel == RelationType.Neutral) continue;
                    if (relationItemPrefab != null)
                    {
                        var item = Instantiate(relationItemPrefab, relationContainer);
                        item.SetActive(true);
                        var n = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
                        if (n != null) n.text = other.emoji + " " + other.codename;
                        var t = item.transform.Find("TypeText")?.GetComponent<TMP_Text>();
                        if (t != null)
                        {
                            t.text = rel switch { RelationType.Partner => "搭档 +15%", RelationType.Rival => "竞争 -5%", _ => "冲突 -20%" };
                            t.color = rel == RelationType.Partner ? Theme.Success : rel == RelationType.Conflict ? Theme.Danger : Theme.Warning;
                        }
                    }
                }
            }

            if (restButton != null) restButton.gameObject.SetActive(h.status == HeroStatus.Ready);
            if (panelRoot != null) panelRoot.SetActive(true);
            AudioManager.Instance?.UIClick();
        }

        void DoRest()
        {
            var g = GameManager.Instance.state;
            HeroManager.Instance?.RestHero(g, _heroId);
            Hide();
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
