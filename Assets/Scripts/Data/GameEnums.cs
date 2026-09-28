using UnityEngine;

namespace HeroDispatch
{
    // ==================== 枚举（独立文件，避免 MonoScript 文件名绑定问题） ====================
    public enum HeroArchetype { Mage, Frost, Rune, Tech, Hacker, Tank, Holy, Warrior, AntiHero, Demon, Fate, Undead, Beast, Fungal, Psychic, Alien, Light, Fighter, Ninja, Brute }
    public enum TacticType { Aggressive, Cautious, Support, Recon }
    public enum EventGrade { S, A, B, C, D, Fail }
    public enum HeroStatus { Ready, OnMission, Resting, Injured, Returning }
    public enum RelationType { Partner, Rival, Conflict, Neutral }
    public enum EventType { Disaster, Crime, Villain, Rescue, Mystery }
    public enum EventUrgency { Critical, Urgent, Moderate, Routine }
    public enum SkillType { Passive, Active, Ultimate }
    public enum VoiceType { Selection, BattleStart, Skill, Ultimate, Hurt, Victory }

    [System.Serializable]
    public class SkillData
    {
        public string name;
        public SkillType type;
        public string desc;
        public Sprite icon;
    }
}
