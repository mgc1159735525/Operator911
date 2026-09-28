from pathlib import Path
root=Path(__file__).resolve().parents[2]
def patch(file, replacements):
    p=root/file
    s=p.read_text(encoding='utf-8-sig')
    for old,new in replacements:
        if old not in s: raise RuntimeError(f'Missing anchor {file}: {old[:80]}')
        s=s.replace(old,new)
    p.write_text(s,encoding='utf-8')
patch('Assets/Minglu/Runtime/PrototypeRules.cs',[
('public int district;', 'public int district;\n        public int consecutiveShifts;\n        public string mission = "";'),
('public bool storyEnabled;', 'public bool storyEnabled, dispatchEnabled;\n        public List<string> completedMissions = new List<string>(), expiredMissions = new List<string>(), verifiedMissions = new List<string>();'),
('bool story = false)', 'bool story = false, bool dispatch = false)'),
('storyEnabled = story,', 'storyEnabled = story, dispatchEnabled = dispatch,'),
('a.district = district;', 'a.district = district; a.mission = "";'),
('- (a.exposure >= 50 ? 1 : 0));','- (a.exposure >= 50 ? 1 : 0) - (a.consecutiveShifts >= 2 ? 1 : 0));'),
('int dice = Dice(a, a.order)', 'int dice = DispatchRules.Dice(s, a, a.order, a.mission)'),
('if (success)\n                {','if (success)\n                {\n                    DispatchRules.Complete(s, a);'),
('s.pressure = Math.Min(100, s.pressure + c.Value("pressurePerTurn"));','foreach (var a in activeAtStart) if (s.dispatchEnabled) a.consecutiveShifts = a.order == Duty.Rest ? 0 : a.consecutiveShifts + 1;\n            DispatchRules.EndNight(s);\n            s.pressure = Math.Min(100, s.pressure + c.Value("pressurePerTurn"));'),
('a.order = Duty.Rest; a.target = -1;', 'a.order = Duty.Rest; a.target = -1; a.mission = "";')])
patch('Assets/Minglu/Runtime/GameConfig.cs',[
('public List<AssetRow> assets = new List<AssetRow>();','public List<AssetRow> assets = new List<AssetRow>();\n        public List<MissionRow> missions = new List<MissionRow>();'),
('static void Unique(IEnumerable<string> ids, string table)','static void Unique(IEnumerable<string> ids, string table)'),
('foreach (string id in new[] { "city", "portraits" })','DispatchRules.Validate(c);\n            foreach (string id in new[] { "city", "portraits" })')])
patch('Assets/Minglu/Editor/ConfigTableTools.cs',[
('outputs/minglu-v02/明路地下_游戏配置.xlsx','outputs/minglu-v03/明路地下_游戏配置.xlsx'),
('"Events", "Assets" };','"Events", "Assets", "Missions" };'),
('"events", "assets" };','"events", "assets", "missions" };'),
('Sheets.ToDictionary(s => s, s => ParseCsv', 'Sheets.Where(s => s != "Missions" || File.Exists(Path.Combine(path, s + ".csv"))).ToDictionary(s => s, s => ParseCsv'),
('if (!tables.ContainsKey(Sheets[i])) throw', 'if (Sheets[i] == "Missions" && !tables.ContainsKey(Sheets[i])) continue;\n                if (!tables.ContainsKey(Sheets[i])) throw'),
('" | 资源 " + c.assets.Count', '" | 任务 " + c.missions.Count + " | 资源 " + c.assets.Count')])
patch('Assets/Minglu/Runtime/PrototypeSave.cs',[
('minglu-city-v2.json','minglu-city-v3.json'),
('a.interrogation < 0 ||', 'a.interrogation < 0 || a.consecutiveShifts < 0 || a.consecutiveShifts > s.config.Value("turns") ||'),
('return s;', 'DispatchRules.ValidateSave(s);\n            return s;')])
patch('Assets/Minglu/Runtime/CityGameUI.cs',[
('ConfigStore.Load(), true)', 'ConfigStore.Load(), true, true)'),
('ConfigStore.Load(),true)', 'ConfigStore.Load(),true,true)'),
('public class CityGameUI', 'public partial class CityGameUI'),
('int selectedAgent = 1, selectedDistrict, rescueTarget = -1, tab;', 'int selectedAgent = 1, selectedDistrict, rescueTarget = -1, tab = 3;'),
('string[] tabs={"城坊地图","组织联系","行动日志"};','string[] tabs={"城坊地图","组织联系","行动日志","限时任务"};'),
('for(int i=0;i<3;i++){int index=i;', 'for(int i=0;i<4;i++){int index=i;'),
('"单线联络 · 信息隔离"','"任务完成 " + State.completedMissions.Count'),
('else if(tab==1)DrawNetwork();else DrawJournal();','else if(tab==1)DrawNetwork();else if(tab==3)DrawMissions();else DrawJournal();'),
('a.stamina+"   暴露 "+a.exposure,78,232', 'a.stamina+"   暴露 "+a.exposure+"   连勤 "+a.consecutiveShifts,78,232'),
('int dice=PrototypeRules.Dice(a,selectedDuty)', 'int dice=DispatchRules.Dice(State,a,selectedDuty,SelectedMission())'),
('Button("Assign",p,"签发指令 · "+action.name','Button("Assign",p,string.IsNullOrEmpty(SelectedMission())?"签发指令 · "+action.name:"承接任务 · "+action.name'),
('string error=PrototypeRules.Assign(State,selectedAgent,selectedDuty,rescueTarget,selectedDistrict);','string mission=SelectedMission();\n            string error=string.IsNullOrEmpty(mission)?PrototypeRules.Assign(State,selectedAgent,selectedDuty,rescueTarget,selectedDistrict):DispatchRules.Assign(State,selectedAgent,mission);'),
('return p>=.75?"颇有把握":p>=.45?"胜负难料":"凶多吉少";', 'return "成功率 "+(p*100).ToString("0.#")+"%";'),
('modal="";tab=0;reviewingEnding=false;', 'modal="";tab=3;selectedMission="";reviewingEnding=false;'),
('Label(p,"行动点 "+PrototypeRules.UsedAP(State)+" / "+State.config.Value("ap")+"   情报消耗 "+PrototypeRules.ReservedIntel(State)+"\\n未派遣成员自动休整。审讯即将突破的成员，需要在本轮营救或断线。",36,448,828,66,18,Muted);','Label(p,DispatchWarnings(),36,438,828,91,17,Muted);'),
('Label(p,"密报局行动手册",36,30,760,55,33,Gold);Label(p,','Label(p,"密报局行动手册",36,30,760,55,33,Gold);Label(p,'),
('一  选人\\n从左侧名录选择成员，查看体力、暴露和已安排的任务。','一  分流任务\\n限时任务按截止夜排序。先看失守代价，再决定是否承接。'),
('二  选地与行动\\n点击地图区域，再选择侦察、破坏或营救。右侧显示真实风险。','二  核实与派遣\\n任务板可花1份未预留情报永久加1骰，选择任务后在右侧签发。'),
('三  签发与封缄\\n签发指令后仍可修改。所有未派遣成员自动休整。','三  轮休\\n连续出勤两夜后少1骰，完整休整一夜清除。推荐只选择未派遣成员。'),
('string path=args[capture+1];Capture(path);','string path=args[capture+1];Capture(path);\n            yield return Click("Tab_0");')])
