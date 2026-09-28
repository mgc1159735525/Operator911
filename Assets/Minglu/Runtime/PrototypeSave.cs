using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public static class PrototypeSave
    {
        public static string PathName { get { return Path.Combine(Application.persistentDataPath, "minglu-city-v3.json"); } }
        public static void Write(RunState state, string path)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(state, true));
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
        public static RunState Read(string path)
        {
            var s = JsonUtility.FromJson<RunState>(File.ReadAllText(path));
            if (s == null || s.version != 3 || s.config == null) throw new InvalidDataException("存档格式版本不匹配，当前对局未被替换。");
            ConfigStore.Validate(s.config);
            if(s.command!=null){CommandRules.Validate(s);return s;}
            if(s.roguelikeRun&&(string.IsNullOrEmpty(s.runId)||s.initialSeed==0||s.difficulty<0||s.difficulty>2||s.doctrine<0||s.doctrine>2))throw new InvalidDataException("新局初始化信息无效。");
            if (s.turn < 1 || s.turn > s.config.Value("turns") || s.random == 0 ||
                s.agents == null || s.agents.Count != s.config.agents.Count || s.journal == null || s.intelligence < 0 || s.progress < 0 || s.pressure < 0 || s.pressure > 100 || s.completedEvents == null ||
                s.agents.Any(a => a == null || !s.config.agents.Any(x => x.id == a.id) || !s.config.districts.Any(x => x.id == a.district) || a.stamina < 0 || a.stamina > 100 || a.exposure < 0 || a.exposure > 100 || a.interrogation < 0 || a.consecutiveShifts < 0 || a.consecutiveShifts > s.config.Value("turns") || !Enum.IsDefined(typeof(Condition), a.condition) || !Enum.IsDefined(typeof(Duty), a.order)) ||
                s.agents.Select(a => a.id).Distinct().Count() != s.agents.Count)
                throw new InvalidDataException("存档版本或内容无效，当前对局未被替换。");
            if (PrototypeRules.UsedAP(s) > s.config.Value("ap") || PrototypeRules.ReservedIntel(s) > s.intelligence ||
                s.agents.Any(a => a.handler != -1 && !s.agents.Any(b => b.id == a.handler && b.id != a.id)) ||
                s.agents.Any(a => a.order == Duty.Rescue && !s.agents.Any(b => b.id == a.target && b.condition == Condition.Captured)))
                throw new InvalidDataException("存档行动或联系数据不合法。");
            if ((!string.IsNullOrEmpty(s.pendingEvent) && !s.config.events.Any(e => e.id == s.pendingEvent)) || s.completedEvents.Any(id => !s.config.events.Any(e => e.id == id))) throw new InvalidDataException("存档事件引用不存在。");
            DispatchRules.ValidateSave(s);
            OrganizationRules.ValidateSave(s);
            CampaignRules.ValidateSave(s);
            CityContentRules.ValidateSave(s);
            CourierRules.ValidateSave(s);
            AppearanceRules.Validate(s.chiefLook);
            foreach(var a in s.agents)AppearanceRules.Validate(a.appearance);
            return s;
        }
    }
}
