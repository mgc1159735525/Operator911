using System;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawCampaign()
        {
            var panel=Box("CampaignBoard",root,288,186,868,570,Panel);
            if(!State.campaignEnabled){Label(panel,"当前为八夜试玩。新局选择「新京战役」可进入20夜主线。",24,28,810,100,24,Gold);return;}
            Label(panel,State.campaign.title+" / "+CampaignRules.Stage(State).name,24,18,810,36,26,Gold);
            Label(panel,"胜利条件：成果 "+State.progress+" / "+State.config.Value("goal")+"，并在决战期完成最终起义。",24,63,810,32,19,Teal);
            var list=Scroll(panel,"CampaignScroll",20,109,828,435,1040);
            int y=0;
            foreach(var phase in State.campaign.stages)
            {
                Label(list,phase.name+" · 第"+phase.start+"—"+phase.end+"夜 · 每夜压力+"+phase.pressure,8,y,795,32,23,State.turn>=phase.start&&State.turn<=phase.end?Gold:Muted);
                Label(list,phase.description,8,y+37,795,32,18,Muted);y+=85;
            }
            Label(list,"起义主线 · 前置任务逾期会中断后续",8,y,790,36,23,Gold);y+=44;
            foreach(string id in new[]{"shelter","network","garrison","uprising"})
            {
                var m=State.config.missions.First(x=>x.id==id);
                string status=State.completedMissions.Contains(id)?"已完成":State.expiredMissions.Contains(id)?"已中断":DispatchRules.Open(State).Any(x=>x.id==id)?"可执行":"等待前置 / 时机";
                Label(list,m.title+" · "+status+" · "+m.start+"—"+m.deadline+"夜",8,y,790,35,21,State.completedMissions.Contains(id)?Teal:TextColor);y+=43;
            }
            Label(list,"延迟后果",8,y+10,790,35,23,Gold);y+=54;
            string effects=State.consequences.Count==0?"暂无已触发的后续搜查。":string.Join("\n",State.consequences.Select(e=>"第"+e.due+"夜 / "+e.cause+" / 压力+"+e.pressure));
            Label(list,effects,8,y,790,130,19,Muted);y+=140;
            Label(list,"成长与身份",8,y,790,35,23,Gold);
            Label(list,"成功行动累计4次，对应能力+1，上限5。身份专长额外+1骰。\n商人：接头；教师：散布思想；军官：破坏；记者：侦察。\n医生专属治疗尚未开放。休整、营救与精神恢复继续生效。",8,y+42,790,114,19,Muted);
        }
    }
}
