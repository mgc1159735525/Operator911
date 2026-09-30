using System.Linq;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawDistrictContext()
        {
            var district=State.config.District(selectedDistrict);
            var strategy=State.command.strategy;
            var tasks=State.command.tasks.Where(t=>t.status==0&&t.district==district.id)
                .OrderBy(t=>t.actor>=0?1:0).ThenBy(t=>t.deadline).ToList();
            var p=Box("DistrictContext",root,1146,122,422,578,Ink);

            Label(p,"城区档案",20,0,379,26,15,Muted);
            Label(p,district.name,20,39,245,40,30,Gold);
            Label(p,"区域暴露 +"+Mathf.Clamp(district.risk,0,6),257,49,143,28,16,Red);
            Label(p,district.subtitle+(strategy.baseDistrict==district.id?"  ·  当前据点":""),20,87,379,27,17,TextColor);
            Label(p,"待授权 "+tasks.Count(t=>t.actor<0)+" 项     已授权 "+tasks.Count(t=>t.actor>=0)+" 项",20,124,379,28,19,Teal);
            Rule(p,20,169,379);

            Label(p,"全区追查 "+strategy.trace+" / "+strategy.config.raid+"   ·   民心 "+strategy.hearts,20,185,379,25,16,Muted);
            Label(p,"本区行动增加 "+Mathf.Clamp(district.risk,0,6)+" 暴露；具体代价见任务方案。",20,218,379,47,16,Muted);
            Label(p,"当地待办",20,274,379,27,19,Gold);

            if(tasks.Count==0)
            {
                Label(p,"此区暂无待办。\n成员仍会按已启用的联络链与日常政策行动。\n可在地方事务查看能够发起的委托。",20,315,379,131,18,Muted);
            }
            else
            {
                var list=Scroll(p,"DistrictTasks_"+district.id,20,313,382,174,Mathf.Max(174,tasks.Count*128));
                int row=0;
                foreach(var task in tasks)
                {
                    int y=row++*128;
                    Label(list,task.title,0,y,356,29,20,Gold);
                    string deadline=task.deadline<=State.turn?"今夜截止":"第 "+task.deadline+" 夜截止";
                    string state=task.actor<0?"待授权":"已授权 · "+CommandName(task.actor);
                    Label(list,deadline+"  /  "+state,0,y+34,356,23,14,task.deadline<=State.turn&&task.actor<0?Red:Muted);
                    Label(list,"来源："+(string.IsNullOrEmpty(task.origin)?"组织事务":task.origin),0,y+60,356,24,14,Muted);
                    Button("DistrictTask_"+task.id,list,"查看任务  ›",0,y+85,356,31,()=>{FocusCommandTask(task.id);toast="已打开「"+task.title+"」。比较成员与方案后再签发。";Draw();});
                    Rule(list,0,y+123,356);
                }
            }

            Rule(p,20,499,379);
            Button("DistrictAffairs",p,"查看地方事务  ›",20,507,379,31,()=>{commandTab=5;Draw();});
            Button("DistrictRoutes",p,"检查联络网  ›",20,544,379,31,()=>{commandTab=1;Draw();});
        }
    }
}
