using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        CommandRecruitPreview recruitPreview;
        uint recruitPreviewRandom;
        int recruitPreviewCount;
        void OpenRecruitment()
        {
            recruitPreview=CommandRules.PreviewRecruit(State);
            recruitPreviewRandom=State.random;recruitPreviewCount=State.agents.Count;
            if(recruitPreview==null){toast="组织人数已满。";Draw();return;}
            modal="recruitment";Draw();
        }
        void DrawRecruitment()
        {
            if(recruitPreview==null||recruitPreviewRandom!=State.random||recruitPreviewCount!=State.agents.Count)
            {recruitPreview=CommandRules.PreviewRecruit(State);recruitPreviewRandom=State.random;recruitPreviewCount=State.agents.Count;}
            if(recruitPreview==null){modal="";return;}
            var a=recruitPreview.agent;var person=recruitPreview.person;
            var p=CommandModal("接纳新成员 / 先了解，再安排",690);
            DrawLook(p,a.appearance,28,112,166,244);
            Label(p,a.name,224,104,648,42,29,Gold);
            Label(p,person.age+"岁 · "+person.profession+" "+person.level+"级 · "+person.temper,224,158,648,30,20);
            Label(p,"任职："+CommandRules.Employment(person),224,205,648,29,19);
            Label(p,"情报 "+a.intel+"    潜行 "+a.stealth+"    说服 "+a.persuade+"    组织 "+a.organize,224,254,648,30,20);
            Label(p,"生活牵挂："+person.life+"\n组织信任："+person.loyalty+" / 100",224,305,648,61,18,Muted);
            Rule(p,28,390,844);
            Label(p,CommandRules.HasInstitution(person)?"可补充机构内线，也可承担接头、交通或授权任务。":"可承担接头、交通与授权任务；成为情报来源前须取得敌方机构任职。",28,412,844,55,19,Teal);
            int maintenance=(State.agents.Count(x=>x.condition==Condition.Active)+1)*2;
            Label(p,"接纳：12 银两 · 此后每夜补助 +2 银两\n接纳后在岗成员补助合计 "+maintenance+" 银两 / 夜。查看或暂缓均不花钱。",28,488,844,61,18);
            string error=CommandRules.RecruitError(State);
            Label(p,error!=""?error:"候选随组织进程更新；反复关闭档案不会重新抽取。",28,560,844,25,16,error!=""?Red:Muted);
            Button("RecruitCancel",p,"暂缓 · 返回人事",28,610,276,48,()=>{modal="";commandTab=2;Draw();});
            Button("RecruitConfirm",p,"接纳 "+a.name+" · 12 银两",324,610,548,48,()=>
            {
                toast=CommandRules.Recruit(State);
                if(toast==""){selectedAgent=State.agents.Last().id;toast="已接纳 "+State.agents.Last().name+"，可安排岗位或任务。";modal="";commandTab=2;recruitPreview=null;Save(false);}
                Draw();
            },error=="",true);
        }
    }
}
