using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        int taskFilter;
        bool resetCommandTaskScroll;
        string TaskOrderText(CommandTask task)
        {return task.actor<0?"待安排":CommandName(task.actor)+" · 已安排"+(task.reconnaissance?"调查":"执行");}
        void FocusCommandTask(int id)
        {
            var task=State.command.tasks.FirstOrDefault(t=>t.id==id&&t.status==0);
            if(task==null)return;
            if(modal!="missionPlan"&&modal!="compareCandidates")
            {
                taskReturnTab=commandTab;
                taskReturnModal=modal=="memberDetails"||modal=="institutionPicker"?"memberDetails":modal=="districtDetails"?"districtDetails":modal=="campaignDossier"?"campaignDossier":modal=="commandConfirm"?"commandConfirm":"";
                taskReturnAgent=taskReturnModal=="memberDetails"?selectedAgent:-1;
            }
            selectedDistrict=task.district;commandTask=id;commandTab=0;taskFilter=0;resetCommandTaskScroll=true;modal="missionPlan";
            if(task.actor>=0)selectedAgent=task.actor;
            else if(task.operation==4)selectedAgent=task.target;
            else if(CommandRules.TaskError(State,selectedAgent,task)!="")
            {
                var available=AssignmentCandidates(task).FirstOrDefault(a=>CommandRules.TaskError(State,a.id,task)=="");
                if(available!=null)selectedAgent=available.id;
            }
        }
        void DrawCommandMenu()
        {
            var p=CommandModal("设置与存档",630);
            Label(p,"第 "+State.turn+" 夜 · 当前安排保留\nF5 保存 / F9 读取 / Esc 返回；查看界面不会推进夜晚。",28,100,844,58,18,Muted);
            Button("Save",p,"保存当前进度",28,184,408,44,Save);
            Button("Load",p,"读取存档",456,184,416,44,Load,System.IO.File.Exists(SavePath)||System.IO.File.Exists(SavePath+".bak"));
            Button("CommandGuide",p,"玩法指南  ›",28,256,408,44,()=>{modal="commandHelp";Draw();});
            Button("CommandSettings",p,"声音与显示  ›",456,256,416,44,()=>{modal="commandSettings";Draw();});
            Rule(p,28,326,844);
            Button("CommandHome",p,"保存并返回主菜单",28,348,844,44,ReturnHome);
            Button("CommandMenuQuit",p,"保存并退出游戏",28,408,844,44,QuitGame);
            Label(p,toast,28,476,844,47,16,Muted);
            Button("CommandResume",p,"返回"+CommandPageName(commandTab),28,550,276,48,()=>{modal="";Draw();});
        }
        void DrawCommandGuide()
        {
            var p=CommandModal("指挥指南 / 首夜任务，次夜联络",750);
            Label(p,"第一夜安排一项任务并看结果；第二夜建立联络链，再逐步处理安全与成长。",28,94,844,28,17,Muted);
            var list=Scroll(p,"CommandGuideText",28,139,844,501,850);
            var guideText=Label(list,"1  地图是指挥桌面\n左侧按钮打开任务簿、联络网、人事、报告和地方事务弹窗。底部「当前重点」与对应按钮指出当前问题；返回或Esc关闭当前层；业务弹窗关闭后回地图。\n\n2  建立传递链\n内线须在敌方政务、税务、巡防或驻军机构任职；民间成员可在人事档案建立任职任务，由本人成功完成后取得资格。点击联络网岗位 → 选择内线、接头人、交通员 → 启用。只有机构内线产出，外送送达才增加库存，普通任务不奖励情报。暂停保留积压，但情报仍会老化。\n\n3  授权任务\n筛选待授权或即将截止事项 → 对比人选 → 查看方案 → 授权。成功率只是一次尝试的概率，不是承诺；多阶段任务需多次成功。也可先授权「调查目标」：用情报能力调查一夜，成功后该目标执行+1骰、暴露−3，可交给另一人接手；不产生库存情报，不能重复叠加，且必须留够后续执行夜数。\n\n4  理解机会成本\n一人一夜只做一项地下工作：任务优先于链条。可用资源等于总额减预留；撤回授权会释放预算。\n\n5  轮休与成长\n打开人事档案，从上方名册选人，再进入个人档案安排政策。生活优先会暂停地下工作；空闲成员处理生活、恢复状态并培养身份。留意暴露预警和救援期限。\n\n6  推进并复盘\n夜前检查岗位冲突与截止事项，确认后才掷骰执行。报告默认按夜展示结果与原因，完整骰面可展开；下一夜重新决定。地区经营提供盟友收益，同时追查会迫使据点撤离。\n\n7  三章委任与机构档案\n二十夜新局按种子生成一份地区委任。左下进入主线，选择已有机构档案的一夜核证，或不要求特定任职的两夜协调。创建目标不等于签发，每阶段仍需指派负责人。三章委任与组织门槛全部达成才算完整胜利；八夜短局和旧对局保留原目标。\n\n8  回看要报\n值夜报告中的「要报留档」保留已读的任务、事件与成员消息，可以重新打开相关档案。\n\n操作提示\n下划线或边框标记当前选项，朱红按钮提示当前主要操作；风险同时配有文字说明。设置中集中提供保存、读档、指南及返回主菜单。Esc 返回，F5 保存，F9 读取；滚动条可拖动。外观只影响形象，不影响能力。",0,0,812,850,21);
            float guideHeight=UnityEngine.Mathf.Max(850,guideText.preferredHeight+16);guideText.rectTransform.sizeDelta=new UnityEngine.Vector2(812,guideHeight);list.sizeDelta=new UnityEngine.Vector2(list.sizeDelta.x,guideHeight);
            Button("CommandGuideBack",p,"返回设置",28,669,276,48,()=>{modal="menu";Draw();});
        }
    }
}
