using System.IO;
using System.Linq;
using UnityEngine;

namespace HeroDispatch
{
    public class SaveManager : MonoBehaviour
    {
        string SavePath => Path.Combine(Application.persistentDataPath, "herodispatch_save.json");

        [System.Serializable]
        class SaveData
        {
            public int day; public float timeInDay; public int hour;
            public int reputation; public int budget;
            public int totalResolved; public int totalFailed;
            public HeroSave[] heroes;
        }

        [System.Serializable]
        class HeroSave
        {
            public string id; public int level; public int xp;
            public float fatigue; public float mental; public float hp;
            public int missionsCompleted; public int missionsFailed; public int status;
        }

        public void SaveGame(GameState state)
        {
            var save = new SaveData
            {
                day = state.day, timeInDay = state.timeInDay, hour = state.hour,
                reputation = state.reputation, budget = state.budget,
                totalResolved = state.totalResolved, totalFailed = state.totalFailed,
                heroes = state.heroes.Select(h => new HeroSave
                {
                    id = h.heroId, level = h.level, xp = h.xp, fatigue = h.fatigue,
                    mental = h.mental, hp = h.hp,
                    missionsCompleted = h.missionsCompleted, missionsFailed = h.missionsFailed,
                    status = (int)h.status
                }).ToArray()
            };
            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            Debug.Log("[SaveManager] 存档完成 → " + SavePath);
        }

        public void LoadGame(GameState state)
        {
            if (!File.Exists(SavePath)) return;
            var save = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

            state.day = save.day; state.timeInDay = save.timeInDay; state.hour = save.hour;
            state.reputation = save.reputation; state.budget = save.budget;
            state.totalResolved = save.totalResolved; state.totalFailed = save.totalFailed;
            state.gameOver = false;

            foreach (var hs in save.heroes)
            {
                var h = state.FindHero(hs.id);
                if (h == null) continue;
                h.level = hs.level; h.xp = hs.xp; h.fatigue = hs.fatigue;
                h.mental = hs.mental; h.hp = hs.hp;
                h.missionsCompleted = hs.missionsCompleted; h.missionsFailed = hs.missionsFailed;
                h.status = (HeroStatus)hs.status;
            }
            Debug.Log("[SaveManager] 读档完成 ← " + SavePath);
        }

        public bool HasSave() => File.Exists(SavePath);
        public void DeleteSave() { if (File.Exists(SavePath)) File.Delete(SavePath); }
    }
}
