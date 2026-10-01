using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawMemberDetails()
        {
            var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,selectedAgent);
            var p=CommandModal(a.name+" / 人物档案",800);
            DrawLook(p,a.appearance,28,100,192,280);
            Label(p,member.profession+" · "+member.level+"级 / "+member.age+"岁 / 性情"+member.temper,248,100,624,28,19,Muted);
            Label(p,"任职："+CommandRules.Employment(member)+"\n地下岗位："+CommandRole(a.id)+"\n情报 "+a.intel+" / 潜行 "+a.stealth+" / 说服 "+a.persuade+" / 组织 "+a.organize+"\n体力 "+a.stamina+" / 精神 "+a.spirit+" / 暴露 "+a.exposure,248,148,624,100,18);
            var detail=Scroll(p,"PersonDetails_"+a.id,248,272,624,126,126);
            var text=Label(detail,"生活牵挂："+member.life+"\n近期情况："+member.last+"\n身份经验："+member.xp+" / "+(State.command.config.coverXP*member.level)+" · 组织信任："+member.loyalty+" / 100",0,0,592,126,17,Muted);
            float h=UnityEngine.Mathf.Max(126,text.preferredHeight+8);text.rectTransform.sizeDelta=new UnityEngine.Vector2(592,h);detail.sizeDelta=new UnityEngine.Vector2(detail.sizeDelta.x,h);
            Rule(p,28,422,844);Label(p,"日常安排",28,442,844,27,20,TextColor);
            string[] policies={"平衡安排","生活优先","主动发现"};
            for(int i=0;i<3;i++){int policy=i;Button("LifePolicy_"+i,p,policies[i],28+i*284,486,272,40,()=>{CommandRules.SetPolicy(State,member.id,policy);Save(false);toast="已设为"+policies[policy]+"。";Draw();},true,member.policy==i);}
            Label(p,member.policy==1?"优先恢复与处理家事；本夜不执行任务，链条岗位暂停。":member.policy==2?"空闲时主动发现地方事务；仍需你的授权，不直接产出情报。":"兼顾表身份工作与休养；空闲时恢复状态，积累身份经验。",28,542,844,52,17,Muted);
            Button("CommandEmployment",p,CommandRules.HasInstitution(member)?"已任职 · 可担任情报链内线":"建立机构任职 / 查看任职任务  ›",28,621,548,45,OpenEmployment,!CommandRules.HasInstitution(member));
            Button("CommandEditLook",p,"编辑人物外观  ›",596,621,276,45,()=>OpenAppearance(a.id));
            Button("MemberDetailsClose",p,"返回指挥",28,720,276,48,()=>{modal="";Draw();});
        }
        void DrawCommandOverview()
        {
            var p=CommandModal("组织概况",690);
            DrawLook(p,State.chiefLook,28,96,176,250);
            Label(p,"地区负责人",28,362,176,25,17,Muted,UnityEngine.TextAnchor.MiddleCenter);
            Button("CommandChiefAppearance",p,"编辑负责人外观",28,401,176,42,()=>OpenAppearance(-1));
            Label(p,GateProgress(),236,100,636,90,25,Gold);
            Rule(p,236,214,636);
            Label(p,"完成任务 "+State.command.completed+" / 送达情报 "+State.command.delivered+"\n在岗身份2级以上 "+State.command.people.Count(x=>x.level>=2&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人 / 3级以上 "+State.command.people.Count(x=>x.level>=3&&CommandRules.Agent(State,x.id).condition==Condition.Active)+"人",236,236,636,62,18);
            Label(p,"银两：可用 "+(State.money-CommandRules.ReservedMoney(State))+" / 已预留 "+CommandRules.ReservedMoney(State)+"\n情报：可用 "+(State.intelligence-CommandRules.ReservedIntel(State))+" / 已预留 "+CommandRules.ReservedIntel(State)+"\n压力 "+State.pressure+" / 追查 "+State.command.strategy.trace+" / 民心 "+State.command.strategy.hearts,236,334,636,93,18,Muted);
            Label(p,"可用资源已扣除任务预留。撤回任务可释放预算；空闲成员自动恢复状态并积累身份经验。",28,494,844,62,17,Muted);
            Button("OverviewClose",p,"返回指挥",28,610,276,48,()=>{modal="";Draw();});
        }
    }
}
