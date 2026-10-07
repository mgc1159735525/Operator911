using System.Linq;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawNoticeArchive()
        {
            var p=CommandModal("要报留档",790);
            Label(p,"保留最近80则要报。已读消息仍可回看，查看不会推进时间。",28,99,844,35,17,Muted);
            var archive=State.command.noticeArchive;
            var content=Scroll(p,"NoticeArchiveList",28,151,844,530,530);float y=0;
            if(archive==null||archive.Count==0)Label(content,"尚无要报。成员执行任务、联络链送达和事件决定后，会在此留下记录。",0,12,808,90,21,Muted);
            else foreach(var n in archive.AsEnumerable().Reverse())
            {
                Label(content,"第 "+n.night+" 夜  /  "+n.title,0,y,808,34,21,Gold);
                var body=Label(content,n.body,0,y+43,808,66,18,TextColor);
                float height=Mathf.Max(66,body.preferredHeight+8);body.rectTransform.sizeDelta=new Vector2(808,height);
                Button("ArchivedNotice_"+archive.IndexOf(n),content,"查看相关档案  ›",0,y+51+height,258,40,()=>OpenArchivedNotice(n));
                y+=height+114;Rule(content,0,y,808);y+=20;
            }
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(530,y));
            Button("NoticeArchiveClose",p,"返回值夜报告",28,714,276,48,()=>{modal="";commandTab=4;Draw();});
        }
        void OpenArchivedNotice(CommandNotice notice)
        {
            modal="";
            var task=State.command.tasks.FirstOrDefault(t=>t.id==notice.task&&t.status==0);
            if(task!=null){commandTab=4;OpenCommandTask(task.id,true);return;}
            if(notice.actor>=0&&State.agents.Any(a=>a.id==notice.actor))
            {selectedAgent=notice.actor;commandTab=4;modal="memberDetails";}
            else if(notice.tab==6&&CommandRules.HasCommandCampaign(State)){commandTab=3;campaignPage=0;modal="campaignDossier";}
            else if(notice.tab==1||notice.tab==5)commandTab=notice.tab;
            else{commandTab=4;reportNight=notice.night;}
            Draw();
        }
    }
}
