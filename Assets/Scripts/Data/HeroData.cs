using System;
using UnityEngine;

namespace HeroDispatch
{
    // 英雄数据（独立文件：类名必须与文件名一致，Unity 才能正确绑定 MonoScript）
    [CreateAssetMenu(fileName = "NewHero", menuName = "HeroDispatch/HeroData")]
    public class HeroData : ScriptableObject
    {
        public string heroId;
        public string codename;
        public string displayName;
        public HeroArchetype archetype;
        public string emoji;
        public Color color;
        public int level = 1;
        public int salary;

        [Header("超能力")]
        public string powerName;

        [Header("五维属性 (0-10)")]
        public float str, spd, intl, end, per;

        [Header("怪癖")]
        public string quirkName;
        public string quirkDesc;

        [Header("技能（被动/主动/终极）")]
        public SkillData[] skills = new SkillData[3];

        [Header("立绘")]
        public Sprite portraitNormal;
        public Sprite portraitInjured;

        [TextArea(2, 4)]
        public string backstory;

        public string GetVoicePath(VoiceType t)
        {
            string suffix = t switch
            {
                VoiceType.Selection => "selection",
                VoiceType.BattleStart => "battle_start",
                VoiceType.Skill => "skill",
                VoiceType.Ultimate => "ultimate",
                VoiceType.Hurt => "hurt",
                VoiceType.Victory => "victory",
                _ => "selection"
            };
            return $"Audio/Voice/voice_{heroId}_{suffix}";
        }

        public float GetStat(string statName)
        {
            switch (statName)
            {
                case "str": return str;
                case "spd": return spd;
                case "int": case "intl": return intl;
                case "end": return end;
                case "per": return per;
            }
            return 0;
        }

        public float[] GetPentagon()
        {
            return new float[] { str, intl, end, per, spd };
        }
    }
}
