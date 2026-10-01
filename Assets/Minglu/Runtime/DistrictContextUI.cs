using System.Linq;
using UnityEngine;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawDistrictContext()
        {
            var district=State.config.District(selectedDistrict);
            var tasks=State.command.tasks.Where(t=>t.status==0&&t.district==district.id).ToList();
            var p=Box("DistrictContext",root,1146,122,422,352,Ink);
            Label(p,"已选城区",24,8,374,22,14,Muted);
            Label(p,district.name,24,48,374,34,24,TextColor);
            Label(p,district.subtitle,24,98,374,26,16,Muted);
            Label(p,"待安排 "+tasks.Count(t=>t.actor<0)+" 项 · 已安排 "+tasks.Count(t=>t.actor>=0)+" 项",24,142,374,27,17,TextColor);
            int urgent=tasks.Count(t=>t.actor<0&&CommandRules.TaskUrgent(State,t));
            Label(p,urgent>0?urgent+" 项今夜必须开始":"本区行动暴露 +"+Mathf.Clamp(district.risk,0,6),24,190,374,26,16,urgent>0?Red:Muted);
            Rule(p,24,240,374);
            Button("DistrictOpen",p,"城区档案  ›",12,260,386,32,()=>{modal="districtDetails";Draw();});
            Button("DistrictRoutes",p,"检查联络网  ›",12,308,386,32,()=>{commandTab=1;Draw();});
        }
        void DrawDistrictDetails()
        {
            var district=State.config.District(selectedDistrict);var strategy=State.command.strategy;
            var tasks=State.command.tasks.Where(t=>t.status==0&&t.district==district.id).OrderBy(t=>t.actor>=0?1:0).ThenBy(t=>t.deadline).ToList();
            float listHeight=Mathf.Clamp(tasks.Count*138,140,416),height=listHeight+284;
            var p=CommandModal(district.name+" / 城区档案",height);
            Label(p,district.subtitle+(strategy.baseDistrict==district.id?" · 当前据点":"")+" · 本区行动暴露 +"+Mathf.Clamp(district.risk,0,6),28,94,844,26,17,Muted);
            Label(p,"待安排 "+tasks.Count(t=>t.actor<0)+" 项 / 已安排 "+tasks.Count(t=>t.actor>=0)+" 项",28,138,844,25,18,TextColor);
            var list=Scroll(p,"DistrictTasks_"+district.id,28,180,844,listHeight,Mathf.Max(listHeight,tasks.Count*138));
            if(tasks.Count==0)Label(list,"此区暂无待办。\n联络链与成员生活仍按安排进行；可在地方事务发起新委托。",0,20,810,86,20,Muted);
            int row=0;
            foreach(var task in tasks)
            {
                int y=row++*138;
                Label(list,task.title,0,y+10,560,31,22,Gold);
                Label(list,TaskOrderText(task)+(task.stepsRequired>1?" · 阶段 "+task.stepsDone+"/"+task.stepsRequired:""),0,y+48,560,26,17,task.actor>=0?Teal:Muted);
                Label(list,CommandRules.TaskTimingText(State,task),0,y+84,790,26,17,CommandRules.TaskUrgent(State,task)&&task.actor<0?Red:Muted);
                Button("DistrictTask_"+task.id,list,"打开任务  ›",600,y+24,206,36,()=>OpenCommandTask(task.id));
                Rule(list,0,y+130,810);
            }
            Button("DistrictClose",p,"返回城区地图",28,height-80,276,48,()=>{modal="";Draw();});
            Button("DistrictAffairs",p,"地方事务  ›",596,height-80,276,48,()=>{modal="";commandTab=5;Draw();});
        }
    }
}
