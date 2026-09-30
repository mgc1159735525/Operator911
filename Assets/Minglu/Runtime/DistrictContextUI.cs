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
            var p=Box("DistrictContext",root,1146,122,422,376,Ink);
            Label(p,"已选城区",20,0,379,26,15,Muted);
            Label(p,district.name,20,42,379,44,32,Gold);
            Label(p,district.subtitle,20,103,379,29,18,Muted);
            Label(p,"待签发 "+tasks.Count(t=>t.actor<0)+" 项 · 已签发 "+tasks.Count(t=>t.actor>=0)+" 项",20,153,379,29,20,Teal);
            int urgent=tasks.Count(t=>t.actor<0&&t.deadline<=State.turn);
            Label(p,urgent>0?urgent+" 项任务今夜截止":"点击地点，展开当地任务与局势。",20,201,379,48,18,urgent>0?Red:Muted);
            Button("DistrictOpen",p,"打开城区档案  ›",20,272,379,40,()=>{modal="districtDetails";Draw();},true,true);
            Button("DistrictRoutes",p,"检查联络网  ›",20,329,379,33,()=>{commandTab=1;Draw();});
        }
        void DrawDistrictDetails()
        {
            var district=State.config.District(selectedDistrict);var strategy=State.command.strategy;
            var tasks=State.command.tasks.Where(t=>t.status==0&&t.district==district.id).OrderBy(t=>t.actor>=0?1:0).ThenBy(t=>t.deadline).ToList();
            var p=CommandModal("城区档案 / "+district.name,710);
            Label(p,district.subtitle+(strategy.baseDistrict==district.id?" · 当前据点":"")+" · 本区行动暴露 +"+Mathf.Clamp(district.risk,0,6),28,88,844,29,19,Muted);
            Label(p,"待签发 "+tasks.Count(t=>t.actor<0)+" 项 / 已签发 "+tasks.Count(t=>t.actor>=0)+" 项",28,137,844,29,21,Teal);
            var list=Scroll(p,"DistrictTasks_"+district.id,28,186,844,385,Mathf.Max(385,tasks.Count*102));
            if(tasks.Count==0)Label(list,"此区暂无待办。\n联络链与成员生活仍会按安排进行。\n需要新的工作时，可到地方事务发起委托。",0,38,810,160,23,Muted);
            int row=0;
            foreach(var task in tasks)
            {
                int y=row++*102;
                Label(list,task.title,0,y+5,556,32,24,Gold);
                Label(list,(task.deadline<=State.turn?"今夜截止":"第 "+task.deadline+" 夜截止")+" · "+(task.actor<0?"待签发":CommandName(task.actor)+"执行"),0,y+46,556,25,17,task.deadline<=State.turn&&task.actor<0?Red:Muted);
                Button("DistrictTask_"+task.id,list,"打开任务  ›",600,y+17,206,43,()=>OpenCommandTask(task.id));
                Rule(list,0,y+91,806);
            }
            Button("DistrictClose",p,"返回城区地图",28,625,404,48,()=>{modal="";Draw();});
            Button("DistrictAffairs",p,"查看地方事务  ›",450,625,422,48,()=>{modal="";commandTab=5;Draw();});
        }
    }
}
