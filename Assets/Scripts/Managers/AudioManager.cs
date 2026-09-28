using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDispatch
{
    // 全局音频：SFX + 英雄配音（Resources/Audio 下）
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource _sfx;
        AudioSource _voice;
        readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

        void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
            _sfx = gameObject.AddComponent<AudioSource>();
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.priority = 0;
        }

        AudioClip Load(string path)
        {
            if (_cache.TryGetValue(path, out var c)) return c;
            c = Resources.Load<AudioClip>(path);
            if (c == null)
                Debug.LogWarning("[Audio] 未找到: " + path);
            else
                _cache[path] = c;
            return c;
        }

        // ==================== SFX ====================
        public void PlaySFX(string name, float volume = 0.8f)
        {
            var clip = Load("Audio/SFX/" + name);
            if (clip != null) _sfx.PlayOneShot(clip, volume);
        }

        // 常用音效便捷方法
        public void UIClick() => PlaySFX("ui_click", 0.6f);
        public void UIHover() => PlaySFX("ui_hover", 0.35f);
        public void UITab() => PlaySFX("ui_tab_switch", 0.5f);
        public void Notification() => PlaySFX("ui_notification", 0.7f);
        public void Alert() => PlaySFX("ui_alert", 0.7f);
        public void MissionStart() => PlaySFX("event_mission_start", 0.9f);
        public void MissionSuccess() => PlaySFX("event_mission_success", 0.9f);
        public void MissionFail() => PlaySFX("event_mission_fail", 0.9f);
        public void HeroSelected() => PlaySFX("event_hero_selected", 0.7f);
        public void HeroInjured() => PlaySFX("event_hero_injured", 0.9f);
        public void HeroArrested() => PlaySFX("event_hero_arrested", 0.9f);
        public void DayTransition() => PlaySFX("event_day_transition", 0.7f);
        public void UltimateCharge() => PlaySFX("event_ultimate_charge", 0.9f);
        public void MenuOpen() => PlaySFX("ui_menu_open", 0.6f);
        public void MenuClose() => PlaySFX("ui_menu_close", 0.6f);

        // 战斗音效（按事件类型自动选）
        public void CombatHit() => PlaySFX("combat_hit", 0.8f);
        public void CombatHeavyHit() => PlaySFX("combat_heavy_hit", 0.9f);
        public void CombatExplosion() => PlaySFX("combat_explosion", 0.9f);

        // ==================== 英雄配音 ====================
        public void PlayHeroVoice(string heroId, VoiceType type, float volume = 1f)
        {
            var suffix = type switch
            {
                VoiceType.Selection => "selection",
                VoiceType.BattleStart => "battle_start",
                VoiceType.Skill => "skill",
                VoiceType.Ultimate => "ultimate",
                VoiceType.Hurt => "hurt",
                VoiceType.Victory => "victory",
                _ => "selection"
            };
            var clip = Load($"Audio/Voice/voice_{heroId}_{suffix}");
            if (clip != null)
            {
                _voice.Stop();
                _voice.PlayOneShot(clip, volume);
            }
        }

        // 根据事件类型播放对应的战斗音效
        public void PlayCombatSFX(EventType t)
        {
            string clip = t switch
            {
                EventType.Disaster => "combat_explosion",
                EventType.Crime => "combat_hit",
                EventType.Villain => "combat_heavy_hit",
                EventType.Rescue => "combat_shield_block",
                EventType.Mystery => "combat_dark_magic",
                _ => "combat_hit"
            };
            PlaySFX(clip, 0.9f);
        }
    }
}
