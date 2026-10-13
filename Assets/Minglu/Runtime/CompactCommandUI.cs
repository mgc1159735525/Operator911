using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawMemberDetails()
        {
            var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,selectedAgent);
            var p=CommandModal(a.name+" / 人物档案",850);
            DrawLook(p,EffectiveMemberLook(a),28,99,192,280);
            Label(p,member.profession+" "+member.level+"级 · "+member.age+"岁 · "+MemberStatus(a),248,99,624,29,21,Gold);
            Label(p,CommandRules.Employment(member)+" · "+CommandRole(a.id),248,144,624,27,18);
            string[] skills={"情报","潜行","说服","组织"};string[] icons={"intel","lock","people","routes"};int[] values={a.intel,a.stealth,a.persuade,a.organize};
            for(int i=0;i<4;i++)CompactMetric(p,icons[i],skills[i],values[i].ToString(),248+i*156,191,145,TextColor);
            Rule(p,248,263,624);
            Label(p,"体力 "+a.stamina+"     精神 "+a.spirit+"     暴露 "+a.exposure+"     信任 "+member.loyalty,248,283,624,27,18,a.exposure>=State.command.strategy.config.warning?Red:Muted).name="MemberVitals";
            Label(p,CommandRules.TemperEffect(member),248,327,624,27,18,Muted);
            IconButton("MemberHistoryHelp",p,"report","生活与近况",248,367,220,39,()=>OpenBriefing(a.name+" / 生活与近况","生活牵挂："+member.life+"\n连勤 "+member.neglect+" 夜\n\n近期情况："+member.last));
            Label(p,"连勤 "+member.neglect+" 夜"+(member.neglect>=2?" · 留意生活":""),486,375,386,27,18,member.neglect>=2?Red:Muted);
            Rule(p,28,426,844);Label(p,"成长",28,446,404,27,21,TextColor);
            IconButton("MemberGrowthHelp",p,"report","成长规则",700,435,172,40,()=>OpenBriefing("成员成长与轮休","地下能力\n完成任务或岗位成功累计4次，对应能力+1，能力上限6。中间阶段与调查不计经验。\n\n表身份\n空闲一夜获得1点身份经验；升级所需经验为 "+State.command.config.coverXP+" × 当前等级，等级上限4。\n\n日常恢复\n体力+20、精神+15、暴露−10。连勤第三夜起：信任−7、可疑+8。执行地下工作时不享受日常恢复。"));
            int[] xp=a.skillExperience??new int[4];
            Label(p,"能力经验",28,492,404,25,18,Gold);
            Label(p,"情报 "+xp[0]+"/4     潜行 "+xp[1]+"/4\n说服 "+xp[2]+"/4     组织 "+xp[3]+"/4",28,528,404,58,18,Muted).name="MemberSkillGrowth";
            Label(p,"身份经验",468,492,404,25,18,Gold);
            Label(p,(member.level>=4?"4级 · 已满级":"Lv."+member.level+"   "+member.xp+" / "+(State.command.config.coverXP*member.level))+"\n空闲夜：成长与恢复",468,528,404,58,18,Muted).name="MemberGrowthChoice";
            string[] policies={"平衡安排","生活优先","主动发现"};
            string[] policyIcons={"routes","home","intel"};
            for(int i=0;i<3;i++){int policy=i;IconButton("LifePolicy_"+i,p,policyIcons[i],policies[i],28+i*284,611,272,41,()=>{CommandRules.SetPolicy(State,member.id,policy);toast="已设为"+policies[policy]+"。";Save(false);Draw();},a.condition==Condition.Active,member.policy==i);}
            Label(p,"生活优先会撤回任务、暂停岗位。",312,657,278,25,15,Muted).name="MemberPolicyWarning";
            Label(p,member.policy==1?"今夜恢复体力与精神；原联络岗位等待。":member.policy==2?"空闲时寻找机会，仍需你签发。":"有安排就出勤，空闲时自动恢复。",28,691,844,26,18,Muted);
            IconButton("CommandEmployment",p,"home",CommandRules.HasInstitution(member)?"已任职 · 可作内线":a.condition!=Condition.Active?"任职需可行动成员":"机构任职",28,736,548,43,OpenEmployment,!CommandRules.HasInstitution(member)&&a.condition==Condition.Active);
            IconButton("CommandEditLook",p,"people","外观",596,736,276,43,()=>OpenAppearance(a.id));
            Button("MemberDetailsClose",p,returnToWorkOverview?"返回今夜安排":"返回"+CommandPageName(commandTab),28,789,276,42,()=>{if(returnToWorkOverview)ReturnToWorkOverview();else{modal="";Draw();}});
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
