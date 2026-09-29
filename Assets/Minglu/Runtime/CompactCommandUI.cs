using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawMemberDetails()
        {
            var p=CommandModal("人物档案 / "+CommandName(selectedAgent),780);
            var content=Box("MemberDetailContent",p,30,87,838,578,Panel);DrawCommandPeople(content);
            Button("MemberDetailsClose",p,"返回指挥",28,706,844,46,()=>{modal="";Draw();});
        }
        void DrawCommandOverview()
        {
            var p=CommandModal("组织概况 / 负责人、目标与预算",690);
            DrawLook(p,State.chiefLook,28,96,176,250);
            Label(p,"地区负责人",28,357,176,30,21,Gold,UnityEngine.TextAnchor.MiddleCenter);
            Button("CommandChiefAppearance",p,"编辑负责人外观",28,401,176,42,()=>OpenAppearance(-1));
            Label(p,GateProgress(),236,99,630,90,24,Gold);
            Label(p,"完成任务 "+State.command.completed+" / 送达情报 "+State.command.delivered+"\n在岗身份2级以上 "+State.command.people.Count(x=>x.level>=2&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人 / 3级以上 "+State.command.people.Count(x=>x.level>=3&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人\n\n银两：可用 "+(State.money-CommandRules.ReservedMoney(State))+" / 已预留 "+CommandRules.ReservedMoney(State)+"\n情报：可用 "+(State.intelligence-CommandRules.ReservedIntel(State))+" / 已预留 "+CommandRules.ReservedIntel(State)+"\n压力 "+State.pressure+" / 追查 "+State.command.strategy.trace+" / 民心 "+State.command.strategy.hearts,236,205,630,253,22);
            Label(p,"你负责人员、任务和联络链。空闲成员会恢复状态并积累表身份经验；及时轮休，才能兼顾潜伏安全与长期成长。",28,493,844,78,21,Muted);
            Button("OverviewClose",p,"返回指挥",28,610,844,46,()=>{modal="";Draw();});
        }
    }
}
