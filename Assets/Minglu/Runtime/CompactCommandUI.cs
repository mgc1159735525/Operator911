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
            var p=CommandModal("组织概况 / 关卡与预算",610);var a=CommandRules.Agent(State,selectedAgent);
            Label(p,CommandRules.GateText(State),28,100,844,65,25,Gold);
            Label(p,"完成任务 "+State.command.completed+" / 送达情报 "+State.command.delivered+"\n在岗身份2级以上 "+State.command.people.Count(x=>x.level>=2&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人 / 3级以上 "+State.command.people.Count(x=>x.level>=3&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人\n\n银两：总额 "+State.money+" / 预留 "+CommandRules.ReservedMoney(State)+"\n情报：总额 "+State.intelligence+" / 预留 "+CommandRules.ReservedIntel(State)+"\n压力 "+State.pressure+" / 追查 "+State.command.strategy.trace+" / 民心 "+State.command.strategy.hearts,28,189,844,256,22);
            Label(p,"当前成员能力：情报 "+a.intel+" / 潜行 "+a.stealth+" / 说服 "+a.persuade+" / 组织 "+a.organize,28,455,844,35,18,Muted);
            Button("OverviewClose",p,"返回指挥",28,532,844,46,()=>{modal="";Draw();});
        }
    }
}
