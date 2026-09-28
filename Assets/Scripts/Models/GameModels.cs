using System.Collections.Generic;
using UnityEngine;

namespace HeroDispatch
{
    // ==================== 英雄运行时 ====================
    [System.Serializable]
    public class Hero
    {
        public HeroData data;
        public HeroStatus status = HeroStatus.Ready;
        public float hp = 100f;
        public float maxHp = 100f;
        public float fatigue;        // 0-100
        public float mental = 85f;   // 0-100
        public int xp;
        public int level = 1;
        public int missionsCompleted;
        public int missionsFailed;
        public int consecutiveMissions;
        public int currentEventId = -1;
        public float returnTimer;
        public float restTimer;
        [System.NonSerialized] public bool lastQuirkRolled;

        public string heroId => data.heroId;
        public string codename => data.codename;
        public string displayName => data.displayName;
        public string emoji => data.emoji;

        public bool IsAvailable => status == HeroStatus.Ready;
        public bool IsOnMission => status == HeroStatus.OnMission;

        public float GetStat(string stat) => data.GetStat(stat);

        public void AddXp(int amount)
        {
            xp += amount;
            if (xp >= level * 100)
            {
                level++;
                xp = 0;
                data.str = Mathf.Min(10, data.str + 0.5f);
                data.intl = Mathf.Min(10, data.intl + 0.5f);
                data.end = Mathf.Min(10, data.end + 0.5f);
                data.per = Mathf.Min(10, data.per + 0.5f);
                data.spd = Mathf.Min(10, data.spd + 0.5f);
            }
        }
    }

    // ==================== 事件运行时 ====================
    [System.Serializable]
    public class GameEvent
    {
        public int id;
        public EventTemplate template;
        public string grade;              // D/C/B/A/S
        public string district;           // districtId（字符串）
        public string districtName;
        public float timeLeft;            // 剩余秒（浮点递减，显示时取整）
        public float maxTime;
        public int rewardRep;
        public int rewardBudget;
        public int riskInjury;
        public int riskCollateral;
        public bool revealed;             // 点击后揭示（去迷雾）
        public List<string> assignedHeroIds = new List<string>();
        public string tacticUsed;
        public string resultGrade;

        public bool IsExpired => timeLeft <= 0;
        public bool IsNew => assignedHeroIds.Count == 0 && timeLeft > 0;
    }

    // ==================== 战斗结果 ====================
    [System.Serializable]
    public class CombatResult
    {
        public int eventId;
        public string grade;
        public float powerRatio;
        public int teamPower;
        public int difficulty;
        public int collateral;
        public List<string> injuredHeroes = new List<string>();
        public List<string> quirkTriggered = new List<string>();
        public int repGain;
        public int budgetGain;
        public int xpAwarded;
        public string battleNarrative;
        public string resultNarrative;
        public string mediaHeadline;
        public List<HeroPerformance> performances = new List<HeroPerformance>();
    }

    [System.Serializable]
    public class HeroPerformance
    {
        public string heroId;
        public string codename;
        public float powerContributed;
        public string quirkTriggered;
        public bool wasInjured;
        public int xpGained;
    }

    // ==================== 游戏状态 ====================
    [System.Serializable]
    public class GameState
    {
        public int day = 1;
        public float timeInDay;              // 0-90s
        public float dayLength = 90f;        // 90秒/天
        public int hour = 6;                 // 6am 起
        public float timeScale = 1f;
        public bool isPaused;
        public int reputation = 50;
        public int budget = 10000;
        public int nextEventId = 1;
        public List<Hero> heroes = new List<Hero>();
        public List<GameEvent> events = new List<GameEvent>();
        public List<string> todaysHeadlines = new List<string>();
        public Dictionary<string, RelationType> relationships = new Dictionary<string, RelationType>();
        public Dictionary<string, float> districtSafety = new Dictionary<string, float>();
        public DayStats dayStats = new DayStats();
        public int totalResolved;
        public int totalFailed;
        public bool gameOver;
        public string gameOverReason;
        public int selectedTacticIdx = 1; // 0 aggressive 1 cautious 2 support 3 recon

        public Hero FindHero(string id) => heroes.Find(h => h.heroId == id);
        public GameEvent FindEvent(int id) => events.Find(e => e.id == id);
        public bool IsNight => hour >= 20 || hour < 5;
    }

    [System.Serializable]
    public class DayStats
    {
        public int resolved;
        public int failed;
        public int xpGained;
    }
}
