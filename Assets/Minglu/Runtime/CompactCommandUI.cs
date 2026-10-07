using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawMemberDetails()
        {
            var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,selectedAgent);
            var p=CommandModal(a.name+" / 人物档案",850);
            DrawLook(p,a.appearance,28,99,192,280);
            Label(p,member.profession+" · "+member.level+"级 / "+member.age+"岁 / "+MemberStatus(a),248,99,624,29,21,Gold);
            Label(p,"任职："+CommandRules.Employment(member)+" · 地下岗位："+CommandRole(a.id),248,140,624,27,18);
            Label(p,"情报 "+a.intel+" / 潜行 "+a.stealth+" / 说服 "+a.persuade+" / 组织 "+a.organize,248,179,624,28,20);
            Label(p,"体力 "+a.stamina+" / 精神 "+a.spirit+" / 暴露 "+a.exposure+" / 信任 "+member.loyalty,248,221,624,26,18,Muted);
            Label(p,CommandRules.TemperEffect(member),248,260,624,26,18,Muted);
            var detail=Scroll(p,"PersonDetails_"+a.id,248,303,624,92,92);
            var text=Label(detail,"生活牵挂："+member.life+" · 连勤 "+member.neglect+" 夜\n近期情况："+member.last,0,0,592,92,18,Muted);
            float h=UnityEngine.Mathf.Max(92,text.preferredHeight+8);text.rectTransform.sizeDelta=new UnityEngine.Vector2(592,h);detail.sizeDelta=new UnityEngine.Vector2(detail.sizeDelta.x,h);
            Rule(p,28,415,844);Label(p,"两种成长，需要轮换安排",28,431,844,27,21,TextColor);
            int[] xp=a.skillExperience??new int[4];
            Label(p,"地下能力 / 成功经验",28,474,404,25,18,Gold);
            Label(p,"完成任务或岗位成功累计4次，对应能力+1。\n情报 "+xp[0]+"/4 · 潜行 "+xp[1]+"/4 · 说服 "+xp[2]+"/4 · 组织 "+xp[3]+"/4\n能力上限6；中间阶段与调查不计经验。",28,508,404,84,17,Muted).name="MemberSkillGrowth";
            Label(p,"表身份 / 日常经验",468,474,404,25,18,Gold);
            Label(p,(member.level>=4?"身份已达4级上限。":"经验 "+member.xp+" / "+(State.command.config.coverXP*member.level)+"；空闲一夜+1。")+"\n日常恢复体力20、精神15，暴露−10。\n连勤第三夜起：信任−7、可疑+8。",468,508,404,84,17,Muted).name="MemberGrowthChoice";
            string[] policies={"平衡安排","生活优先","主动发现"};
            for(int i=0;i<3;i++){int policy=i;Button("LifePolicy_"+i,p,policies[i],28+i*284,612,272,41,()=>{CommandRules.SetPolicy(State,member.id,policy);toast="已设为"+policies[policy]+"。";Save(false);Draw();},a.condition==Condition.Active,member.policy==i);}
            Label(p,member.policy==1?"生活优先：撤回已签发任务；今夜处理日常，原联络岗位等待，可安排替补。":member.policy==2?"主动发现：空闲时可能上报新机会，仍由你授权；执行地下工作时不享受日常恢复。":"平衡安排：有任务或联络待办就出勤；没有地下工作时，自动处理日常并恢复。",28,669,844,46,18,Muted);
            Button("CommandEmployment",p,CommandRules.HasInstitution(member)?"已任职 · 可担任情报链内线":"建立机构任职 / 查看任职任务  ›",28,728,548,43,OpenEmployment,!CommandRules.HasInstitution(member)&&a.condition==Condition.Active);
            Button("CommandEditLook",p,"编辑人物外观  ›",596,728,276,43,()=>OpenAppearance(a.id));
            Button("MemberDetailsClose",p,"返回"+CommandPageName(commandTab),28,789,276,42,()=>{modal="";Draw();});
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
            Button("OverviewClose",p,"返回"+CommandPageName(commandTab),28,610,276,48,()=>{modal="";Draw();});
        }
    }
}
