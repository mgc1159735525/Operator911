using System;
using System.Collections.Generic;
using System.Linq;

namespace Minglu
{
    public enum Duty { Rest, Recon, Sabotage, Rescue, CutContact, Contact, Propaganda }
    public enum Condition { Active, Captured, Lost, Undercover }

    [Serializable]
    public class Agent
    {
        public int id;
        public string name;
        public Appearance appearance;
        public int intel, stealth, stamina = 100, exposure;
        public int persuade=2, organize=2;
        public string coverRole="";
        public int[] skillExperience=new int[4];
        public int handler = -1;
        public Condition condition;
        public int interrogation;
        public bool disclosed;
        public Duty order;
        public int target = -1;
        public int district;
        public int courierTarget=-1;
        public int knownStation;
        public int consecutiveShifts;
        public string mission = "", site="";
        public int spirit=100, suspicion, candidateTarget=-1, spyStarted, spyNights;
        public bool maintainCover=true, recall;
        public OrganizationDuty organizationDuty;
    }

    [Serializable]
    public class RunState
    {
        public Appearance chiefLook;
        public CommandState command;
        public string runId="";
        public uint initialSeed;
        public int difficulty=1, doctrine;
        public bool roguelikeRun;
        public bool courierEnabled;
        public CourierConfig courier;
        public SecurityState security;
        public int nextDispatchId;
        public List<DispatchPacket> dispatchPackets=new List<DispatchPacket>();
        public bool cityEnabled;
        public CityContent city;
        public List<CityQuarter> quarters=new List<CityQuarter>();
        public List<string> cityEvidence=new List<string>(),visitedSites=new List<string>();
        public bool campaignEnabled;
        public CampaignContent campaign;
        public List<string> campaignFlags=new List<string>();
        public List<PendingConsequence> consequences=new List<PendingConsequence>();
        public bool organizationEnabled;
        public int money, nextPacketId;
        public OrganizationConfig organization;
        public List<RecruitCandidate> candidates=new List<RecruitCandidate>();
        public List<IntelPacket> packets=new List<IntelPacket>();
        public int version = 3, turn = 1, progress, intelligence = 1, pressure = 15;
        public uint random = 911;
        public bool ended, won;
        public string ending = "";
        public List<Agent> agents = new List<Agent>();
        public List<string> journal = new List<string>();
        public GameConfig config;
        public bool storyEnabled, dispatchEnabled;
        public List<string> completedMissions = new List<string>(), expiredMissions = new List<string>(), verifiedMissions = new List<string>();
        public string pendingEvent = "";
        public List<string> completedEvents = new List<string>();
    }

    // Pure C# rules: no scene objects or Unity random state in the simulation.
    public static class PrototypeRules
    {
        public const int Turns = 8, Goal = 24, ActionPoints = 3;
        public static RunState NewRun(bool crisis, uint seed = 911, GameConfig config = null, bool story = false, bool dispatch = false)
        {
            var c = config ?? ConfigStore.Load();
            var s = new RunState { random = seed == 0 ? 911u : seed, config = c, storyEnabled = story, dispatchEnabled = dispatch, intelligence = c.Value("startingIntel"), pressure = c.Value("startingPressure") };
            foreach (var row in c.agents) s.agents.Add(new Agent { id = row.id, name = row.name, intel = row.intel, stealth = row.stealth, handler = row.handler });
            Log(s, c.Value("turns") + "回合内积累" + c.Value("goal") + "点行动成果。侦察取得情报，破坏推进目标；未派遣成员自动休整。");
            if (crisis)
            {
                Capture(s, s.agents[0]);
                Log(s, "危机开局：" + s.agents[0].name + "已被捕。可先营救，或让其下线断线转移。");
            }
            return s;
        }

        public static int UsedAP(RunState s) { return s.agents.Where(a => a.condition == Condition.Active).Sum(a => a.organizationDuty==OrganizationDuty.None?s.config.Action(a.order).ap:1)+s.agents.Count(a=>a.condition==Condition.Undercover&&a.recall); }
        public static int ReservedIntel(RunState s) { return s.agents.Where(a => a.condition == Condition.Active).Sum(a => s.config.Action(a.order).intelCost); }

        public static string Assign(RunState s, int id, Duty duty, int target = -1, int district = 0)
        {
            var a = s.agents.Find(x => x.id == id);
            if (s.ended || a == null || a.condition != Condition.Active) return "该成员目前无法行动。";
            if (!string.IsNullOrEmpty(s.pendingEvent)) return "请先处理当前密报。";
            if (!s.config.districts.Any(x => x.id == district)) return "行动区域不存在。";
            if (!Enum.IsDefined(typeof(Duty), duty)||!s.config.actions.Any(x=>x.id==duty.ToString())) return "未知行动。";
            if(s.organizationEnabled&&a.spirit<15&&duty!=Duty.Rest)return "精神濒临崩溃，需要休整后再外出。";
            if (duty == Duty.Rescue)
            {
                var prisoner = s.agents.Find(x => x.id == target && x.condition == Condition.Captured);
                if (prisoner == null) return "请先选择一名被捕成员。";
                if (district != prisoner.district) return "营救必须在被捕成员所在区域进行。";
                if (s.agents.Any(x => x.id != id && x.order == Duty.Rescue && x.target == target)) return "已有成员负责营救此人。";
            }
            if (duty == Duty.CutContact && a.handler < 0) return "此人直接联系领导者，无需断线。";
            var old = a.order;
            var oldTarget = a.target;
            var oldOrganization=a.organizationDuty;a.organizationDuty=OrganizationDuty.None;
            a.order = duty;
            a.target = target;
            if (UsedAP(s) > s.config.Value("ap") || ReservedIntel(s) > s.intelligence)
            {
                a.order = old; a.target = oldTarget;a.organizationDuty=oldOrganization;
                return "行动点或已有情报不足。侦察获得的情报下回合才能使用。";
            }
            a.district = district; a.mission = "";a.site="";a.courierTarget=-1;
            return "";
        }

        public static int Dice(Agent a, Duty duty)
        {
            int ability = (duty == Duty.Recon ? a.intel : duty==Duty.Contact?a.persuade:duty==Duty.Propaganda?a.organize:a.stealth)+CampaignRules.IdentityBonus(a,duty);
            return Math.Max(1, ability - (a.stamina < 40 ? 2 : a.stamina < 70 ? 1 : 0) - (a.exposure >= 50 ? 1 : 0) - (a.consecutiveShifts >= 2 ? 1 : 0) - (a.spirit<30?1:0));
        }

        public static int Roll(RunState s)
        {
            uint x = s.random;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            s.random = x;
            return (int)(x % 6) + 1;
        }

        public static void Resolve(RunState s)
        {
            if(s.command!=null){CommandRules.Resolve(s);return;}
            if (s.ended || !string.IsNullOrEmpty(s.pendingEvent)) return;
            var c = s.config;
            var prisonersAtStart = s.agents.Where(a => a.condition == Condition.Captured).ToList();
            var activeAtStart = s.agents.Where(a => a.condition == Condition.Active).ToList();
            Log(s, "—— 第" + s.turn + "回合行动 ——");
            s.intelligence -= ReservedIntel(s);
            // Transfer before interrogation; it changes contact location, not the prisoner's memory.
            foreach (var a in activeAtStart.Where(a => a.order == Duty.CutContact))
            {
                a.handler = -1; a.exposure = Math.Max(0, a.exposure - c.Value("cutExposure"));
                Log(s, a.name + "断线并转移接头地点。旧联络地址失效，暂由领导者直接联系。");
            }
            foreach (var a in activeAtStart)
            {
                if(OrganizationRules.Execute(s,a))continue;
                if (a.order == Duty.CutContact) continue;
                if (a.order == Duty.Rest)
                {
                    a.stamina = Math.Min(100, a.stamina + c.Value("restStamina"));
                    a.exposure = Math.Max(0, a.exposure - c.Value("restExposure"));
                    Log(s, a.name + "休整：体力+" + c.Value("restStamina") + "，暴露-" + c.Value("restExposure") + "。");
                    continue;
                }
                int dice = DispatchRules.Dice(s, a, a.order, a.mission), successes = 0;
                var action = c.Action(a.order);
                var area = c.District(a.district);
                var faces = new List<int>();
                for (int i = 0; i < dice; i++) { int face = Roll(s); faces.Add(face); if (face >= 4) successes++; }
                int required = action.required + area.difficulty;
                bool success = successes >= required;
                a.stamina = Math.Max(0, a.stamina - action.staminaCost);
                int exposure = action.exposure + area.risk + CityContentRules.Exposure(s,a.district) + (success ? 0 : action.failExposure);
                a.exposure = Math.Min(100, a.exposure + exposure);
                Log(s, a.name + " · " + area.name + " / " + action.name + "：骰面[" + string.Join(",", faces) + "]，成功" + successes + "/需求" + required + (success ? "，完成。" : "，失手。") + " 体力-" + action.staminaCost + "；暴露+" + exposure + "。");
                CityContentRules.ActionResult(s,a,success);
                if (success)
                {
                    DispatchRules.Complete(s, a);
                    CampaignRules.SuccessfulAction(s,a);
                    int intel = action.intelReward + (a.order == Duty.Recon ? area.reconBonus : 0);
                    int progress = action.progressReward + (a.order == Duty.Sabotage ? area.sabotageBonus : 0);
                    s.intelligence += intel; s.progress += progress; s.pressure += action.pressure;
                    if (intel > 0 || progress > 0) Log(s, "情报+" + intel + "，行动成果+" + progress + "，敌方压力+" + action.pressure + "。");
                }
                if (success && a.order == Duty.Rescue)
                {
                    var rescued = s.agents.Find(x => x.id == a.target);
                    if (rescued != null && rescued.condition == Condition.Captured)
                    {
                        rescued.condition = Condition.Active; rescued.interrogation = 0;
                        rescued.exposure = c.Value("rescuedExposure"); rescued.stamina = c.Value("rescuedStamina");
                        Log(s, rescued.name + "获救；审讯取消，本回合不再执行其他行动。已经泄露的信息不会撤回。");
                    }
                }
            }
            foreach (var p in prisonersAtStart.Where(a => a.condition == Condition.Captured))
            {
                p.interrogation++;
                if (p.interrogation >= c.Value("leakTurn") && !p.disclosed)
                {
                    p.disclosed = true;
                    SecurityRules.Disclose(s,p);
                    foreach (var child in s.agents.Where(a => a.handler == p.id && (a.condition == Condition.Active||a.condition==Condition.Undercover)))
                    {
                        child.exposure = Math.Min(100, child.exposure + c.Value("leakExposure"));
                        Log(s, p.name + "供出旧联络线索：" + child.name + "暴露+" + c.Value("leakExposure") + "。其他线未受影响。");
                    }
                    Log(s, p.name + "的审讯已突破。审讯达到" + c.Value("lostTurn") + "时永久失联。");
                }
                if (p.interrogation >= c.Value("lostTurn")) { p.condition = Condition.Lost; Log(s, p.name + "被秘密转押，永久失联，无法继续营救。"); }
            }
            foreach (var a in activeAtStart) if (s.dispatchEnabled) a.consecutiveShifts = a.order == Duty.Rest && a.organizationDuty==OrganizationDuty.None ? 0 : a.consecutiveShifts + 1;
            OrganizationRules.EndNight(s,activeAtStart);
            CourierRules.EndNight(s);
            SecurityRules.EndNight(s);
            DispatchRules.EndNight(s);
            CampaignRules.SettleConsequences(s);
            CityContentRules.EndNight(s);
            s.pressure = Math.Min(100, s.pressure + c.Value("pressurePerTurn"));
            foreach (var a in s.agents.Where(a => a.condition == Condition.Active))
            {
                if (s.pressure >= c.Value("patrolThreshold")) a.exposure = Math.Min(100, a.exposure + c.Value("patrolExposure"));
                if (a.exposure >= c.Value("captureThreshold")) Capture(s, a);
            }
            foreach (var a in s.agents) { a.order = Duty.Rest; a.target = -1; a.mission = "";a.site="";a.courierTarget=-1;a.organizationDuty=OrganizationDuty.None; }
            if (!s.agents.Any(a => a.condition == Condition.Active||a.condition==Condition.Undercover)) Finish(s, false, "所有可行动成员均已折损，联络网停止运转。");
            else if (s.progress >= c.Value("goal") && CampaignRules.CanWin(s)) Finish(s, true, "行动目标完成。留下的人，将记住这一条地下之路。");
            else if (s.turn >= c.Value("turns")) Finish(s, false, "行动窗口关闭，未能完成目标。复盘派遣、休整与情报使用，再试一次。");
            else
            {
                s.turn++;
                CampaignRules.BeginTurn(s);
                var e = c.events.Find(x => x.turn == s.turn && !s.completedEvents.Contains(x.id));
                if (s.storyEnabled && e != null) s.pendingEvent = e.id;
            }
        }

        public static string ChooseEvent(RunState s, bool optionA)
        {
            if (s.ended || string.IsNullOrEmpty(s.pendingEvent)) return "没有待处理密报。";
            var e = s.config.events.Find(x => x.id == s.pendingEvent);
            if (e == null) return "事件不存在。";
            int intel = optionA ? e.intelA : e.intelB;
            int progress = optionA ? e.progressA : e.progressB;
            if (s.intelligence + intel < 0 || s.progress + progress < 0) return "资源不足，无法选择此项。";
            s.intelligence += intel; s.progress += progress;
            s.pressure = Math.Max(0, Math.Min(100, s.pressure + (optionA ? e.pressureA : e.pressureB)));
            Log(s, e.title + "：" + (optionA ? e.optionA : e.optionB) + "；情报" + intel.ToString("+0;-0;0") + "，成果" + progress.ToString("+0;-0;0") + "。");
            CampaignRules.ChooseEvent(s,e.id,optionA);
            s.completedEvents.Add(e.id); s.pendingEvent = "";
            if (s.progress >= s.config.Value("goal") && CampaignRules.CanWin(s)) Finish(s, true, "密报中的选择促成了行动目标。");
            return "";
        }

        internal static void Capture(RunState s, Agent a)
        {
            a.condition = Condition.Captured; a.interrogation = 0; a.disclosed = false;
            a.order = Duty.Rest; a.target = -1; a.mission = "";a.site="";
            a.organizationDuty=OrganizationDuty.None;a.courierTarget=-1;a.recall=false;
            Log(s, a.name + "被捕。审讯将在后续回合推进；达到" + s.config.Value("leakTurn") + "时泄密，" + s.config.Value("lostTurn") + "时永久失联。");
        }
        static void Finish(RunState s, bool won, string reason) { s.ended = true; s.won = won; s.ending = s.campaignEnabled?CampaignRules.Ending(s,won):reason; Log(s, (won ? "胜利：" : "失败：") + reason); }
        static void Log(RunState s, string message) { s.journal.Add("[" + s.turn + "] " + message); }
        public static string Name(Duty duty) { return new[] { "休整", "侦察", "破坏", "营救", "断线转移", "秘密接头", "散布思想" }[(int)duty]; }
    }
}
