using System;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        string selectedMission = "";
        string SelectedMission()
        {
            return DispatchRules.Open(State).Any(m => m.id == selectedMission && m.district == selectedDistrict && m.duty == selectedDuty.ToString()) ? selectedMission : "";
        }
        void SelectMission(MissionRow m, bool recommend)
        {
            selectedSite=CityContentRules.Case(State,m.id)?.site??"";
            selectedMission = m.id; selectedDistrict = m.district; selectedDuty = (Duty)Enum.Parse(typeof(Duty), m.duty);
            if (recommend)
            {
                var candidate = DispatchRules.Recommend(State, m).FirstOrDefault();
                if (candidate != null) { selectedAgent = candidate.id; toast = "推荐" + candidate.name + "：先比较成功率，再比较暴露和体力。请在右侧签发。"; }
                else toast = "没有空闲成员。请先撤回一项指令，或手动选择已派遣成员改派。";
            }
            else
            {
                var owner=State.agents.FirstOrDefault(a=>a.mission==m.id&&a.condition==Condition.Active);
                if(owner!=null)selectedAgent=owner.id;
                toast = "已选任务「" + m.title + "」。点击左侧成员比较成功率，再在右侧签发。";
            }
            Draw();
        }
        void DrawMissions()
        {
            var p = Box("MissionBoard", root, 288, 186, 868, 570, Panel);
            Label(p, "今夜待办 / 限时线报", 22, 16, 620, 33, 24, Gold);
            Label(p, "到期夜仍可行动；失败可重试。核实花1份情报，本任务永久+1骰。", 22, 58, 822, 28, 16, Muted);
            var missions = DispatchRules.Open(State).ToList();
            float step=State.cityEnabled?249:207, height=State.cityEnabled?235:193, buttons=State.cityEnabled?179:137;
            var content = Scroll(p, "MissionScroll", 20, 99, 828, 443, missions.Count * step);
            if (missions.Count == 0) Label(content, "暂无开放任务。可以在城坊地图安排常规行动或让成员休整。", 12, 22, 780, 90, 23, Muted);
            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i]; float y = i * step;
                var card = Box("Mission_" + m.id, content, 0, y, 805, height, Ink);
                var owner = State.agents.FirstOrDefault(a => a.mission == m.id && a.condition == Condition.Active);
                bool urgent = m.deadline == State.turn;
                Label(card, m.title + "  /  " + State.config.District(m.district).name, 14, 12, 545, 29, 21, Gold);
                Label(card, urgent ? "今夜截止" : "第" + m.deadline + "夜截止", 622, 14, 169, 26, 18, urgent ? Red : Teal, TextAnchor.MiddleRight);
                Label(card, m.body, 14, 50, 776, 40, 16, Muted);
                Label(card, "优先级 " + m.priority + "  逾期压力+" + m.failurePressure + "  额外成果+" + m.progressReward + " / 情报+" + m.intelReward + "  " + (owner == null ? "未承接" : owner.name + "执行"), 14, 96, 776, 25, 16, owner == null && urgent ? Red : TextColor);
                var cc=CityContentRules.Case(State,m.id);
                if(State.cityEnabled)Label(card,cc==null?"战役主线 / 支线 · 奖励叠加常规行动收益":CityContentRules.Summary(cc),14,132,776,30,16,Teal);
                Button("MissionSelect_" + m.id, card, "选择任务", 14, buttons, 144, 40, () => SelectMission(m, false), true, selectedMission == m.id);
                Button("MissionRecommend_" + m.id, card, owner==null?"推荐空闲人选":"撤回"+owner.name, 170, buttons, 180, 40, () => { if(owner==null)SelectMission(m,true);else CancelMission(m); });
                bool verified = State.verifiedMissions.Contains(m.id);
                Button("MissionVerify_" + m.id, card, verified ? "已核实 · +1骰" : "核实 · 1情报", 362, buttons, 174, 40, () => { var error = DispatchRules.Verify(State, m.id); toast = error == "" ? "线报核实完成，本任务行动骰+1。" : error; Draw(); }, !verified && State.intelligence - PrototypeRules.ReservedIntel(State) >= 1);
                var duty = (Duty)Enum.Parse(typeof(Duty), m.duty);
                var a = State.agents.First(x => x.id == selectedAgent);
                Button("MissionLocate_"+m.id,card,"地图定位",548,buttons,160,40,()=>{tab=0;SelectMission(m,false);});
                if (selectedMission == m.id) Border(card, 0, 0, 805, height, Gold);
            }
        }
        string DispatchWarnings()
        {
            int overdue = DispatchRules.Open(State).Count(m => m.deadline == State.turn && !State.agents.Any(a => a.mission == m.id));
            int exhausted = State.agents.Count(a => a.condition == Condition.Active && a.order != Duty.Rest && a.consecutiveShifts >= 2);
            int danger = State.agents.Count(a => a.condition == Condition.Captured && a.interrogation + 1 >= State.config.Value("leakTurn"));
            var urgentMain=DispatchRules.Open(State).FirstOrDefault(m=>new[]{"shelter","network","garrison","uprising"}.Contains(m.id)&&m.deadline<=State.turn+1&&!State.agents.Any(a=>a.mission==m.id));
            string safety=State.courierEnabled?"\n交通站追查 "+State.security.trace+"/100；今夜到期密件 "+State.dispatchPackets.Count(p=>(p.status==DispatchStatus.Waiting||p.status==DispatchStatus.Received)&&p.expires==State.turn)+" 份。":"";
            return "行动点 " + PrototypeRules.UsedAP(State) + " / " + State.config.Value("ap") + "   预留情报 " + PrototypeRules.ReservedIntel(State) +
                "\n今夜到期未承接 " + overdue + " 项   疲劳出勤 " + exhausted + " 人   审讯危险 " + danger + " 人" +
                (urgentMain==null?"\n未派遣成员自动休整；有剩余AP也可以结算。":"\n主线警告："+urgentMain.title+"第"+urgentMain.deadline+"夜截止，未安排；错过将中断起义路线。")+
                (State.organizationEnabled?"\n银两 "+State.money+" / 招募预留 "+OrganizationRules.ReservedMoney(State)+" / 身份维持预计 "+(State.agents.Count(a=>a.condition==Condition.Active&&a.maintainCover&&a.organizationDuty!=OrganizationDuty.Infiltrate)*State.organization.maintenance)+"（不足者将增加可疑度）":"")+safety;
        }
    }
}
