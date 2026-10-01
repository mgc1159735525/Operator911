using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        bool RouteSlotWorksTonight(CommandRoute route,int slot)
        {
            return CommandRules.RouteSlotHasWork(State,route,slot);
        }
        int AssignmentChainImpact(int actor)
        {
            if(CommandRules.Availability(State,actor)!="")return 0;
            return State.command.routes.Sum(r=>Enumerable.Range(0,3).Count(slot=>new[]{r.source,r.liaison,r.courier}[slot]==actor&&RouteSlotWorksTonight(r,slot)));
        }
        string AssignmentChainText(int actor)
        {
            var rows=new List<string>();
            foreach(var route in State.command.routes)
            for(int slot=0;slot<3;slot++)
            {
                if(new[]{route.source,route.liaison,route.courier}[slot]!=actor||!RouteSlotWorksTonight(route,slot))continue;
                if(CommandRules.Availability(State,actor)!="")continue;
                rows.Add("链"+(route.id+1)+new[]{"内线产出","接头","外送"}[slot]+"等待一夜");
            }
            if(rows.Count>0)return string.Join("；",rows);
            var assigned=State.command.routes.FirstOrDefault(r=>r.enabled&&new[]{r.source,r.liaison,r.courier}.Contains(actor));
            if(assigned!=null&&CommandRules.RouteError(State,assigned)!="")return "原链已有失效岗位\n需另行修复";
            if(assigned!=null&&State.command.pauseUntil>=State.turn)return "整线今夜已休整\n不追加传递等待";
            if(assigned!=null&&CommandRules.Availability(State,actor)!="")return "成员当前无法出勤\n先处理下方阻碍";
            return CommandRole(actor)=="机动成员"?"不占用联络岗位":"该岗位今晚无待办\n不推迟本夜传递";
        }
        string AssignmentLifeText(Agent a)
        {
            var member=CommandRules.Person(State,a.id);int next=member.neglect+1;
            return "执行后连勤 "+next+" 夜\n"+(next>=3?"家事积压：信任−7、可疑+8":"本夜不积累身份经验");
        }
        IEnumerable<Agent> AssignmentCandidates(CommandTask task)
        {
            return State.agents.OrderBy(a=>CommandRules.TaskError(State,a.id,task)==""?0:1)
                .ThenBy(a=>AssignmentChainImpact(a.id))
                .ThenByDescending(a=>CommandRules.TaskDice(State,a,task))
                .ThenBy(a=>CommandRules.Person(State,a.id).neglect)
                .ThenBy(a=>a.id);
        }
        string AssignmentReason(Agent a,CommandTask task)
        {
            string error=CommandRules.TaskError(State,a.id,task);if(error!="")return error;
            if(AssignmentChainImpact(a.id)>0)return "可执行，但"+AssignmentChainText(a.id).Replace("\n","；");
            var route=State.command.routes.FirstOrDefault(r=>r.enabled&&new[]{r.source,r.liaison,r.courier}.Contains(a.id));
            if(route!=null&&CommandRules.RouteError(State,route)!="")return "可执行 · 原链已有失效岗位，需另行修复";
            if(route!=null&&State.command.pauseUntil>=State.turn)return "可执行 · 本夜整线休整，不追加传递等待";
            return CommandRole(a.id)=="机动成员"?"可执行 · 机动成员，不打断联络":"可执行 · 岗位今晚无待办，不打断传递";
        }
        string RouteStepForecast(CommandRoute route,int slot)
        {
            int id=new[]{route.source,route.liaison,route.courier}[slot];
            if(id<0)return "尚未安排成员";
            string error=CommandRules.RouteError(State,route);if(error!="")return "整线待检查："+error;
            if(!route.enabled)return "整线暂停，积压继续计时";
            if(State.command.pauseUntil>=State.turn)return "本夜隐蔽休整，暂不执行";
            bool work=RouteSlotWorksTonight(route,slot);
            if(!work)return slot==0?"本夜不观察；下夜进入观察周期":"本夜无上游积压，等待下一夜";
            string availability=CommandRules.Availability(State,id);if(availability!="")return "岗位等待："+availability;
            if(State.command.tasks.Any(t=>t.status==0&&t.actor==id))return "岗位等待：成员优先执行已签发任务";
            int dice=CommandRules.Dice(State,CommandRules.Agent(State,id),new[]{0,2,1}[slot]);
            return dice+" 骰 / "+(DispatchRules.Probability(dice,1)*100).ToString("0.#")+"% · "+new[]{"成功增加1份待接","成功将1份送入中继","成功送达1份给上级"}[slot];
        }
    }
}
