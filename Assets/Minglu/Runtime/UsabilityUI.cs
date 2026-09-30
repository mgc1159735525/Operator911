using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        int taskFilter;
        bool resetCommandTaskScroll;
        void FocusCommandTask(int id)
        {
            var task=State.command.tasks.FirstOrDefault(t=>t.id==id&&t.status==0);
            if(task==null)return;
            if(modal!="missionPlan"&&modal!="compareCandidates")
            {
                taskReturnTab=commandTab;
                taskReturnModal=modal=="memberDetails"||modal=="institutionPicker"?"memberDetails":"";
            }
            commandTask=id;commandTab=0;taskFilter=0;resetCommandTaskScroll=true;modal="missionPlan";
            if(task.actor>=0)selectedAgent=task.actor;
            else if(task.operation==4)selectedAgent=task.target;
        }
        void DrawCommandMenu()
        {
            var p=CommandModal("行动菜单 / 时间只在推进一夜时流动",560);
            Label(p,"第 "+State.turn+" 夜 · 当前安排保留。\nF5 保存 / F9 读取 / Esc 返回；长列表可滚轮或拖动右侧滚动条。",28,100,844,85,21,Muted);
            Button("CommandResume",p,"返回指挥",28,210,844,50,()=>{modal="";Draw();},true,true);
            Button("CommandGuide",p,"玩法指南 · 从目标到执行",28,280,410,50,()=>{modal="commandHelp";Draw();});
            Button("CommandSettings",p,"声音与显示设置",458,280,414,50,OpenSettings);
            Button("CommandMenuHome",p,"保存并返回主菜单",28,355,844,50,ReturnHome);
            Button("CommandMenuQuit",p,"保存并退出游戏",28,430,844,50,QuitGame);
        }
        void DrawCommandGuide()
        {
            var p=CommandModal("指挥指南 / 先建立循环，再处理取舍",750);
            var list=Scroll(p,"CommandGuideText",28,90,844,550,850);
            var guideText=Label(list,"1  看目标\n左侧简报显示关卡要求；指挥提示和「前往处理」指向当前问题。\n\n2  建立传递链\n内线须在敌方政务、税务、巡防或驻军机构任职；民间成员可在人事档案建立任职任务，由本人成功完成后取得资格。点击联络网岗位 → 选择内线、接头人、交通员 → 启用。只有机构内线产出，外送送达才增加库存，普通任务不奖励情报。暂停保留积压，但情报仍会老化。\n\n3  授权任务\n筛选待授权或即将截止事项 → 对比人选 → 查看方案 → 授权。成功率只是一次尝试的概率，不是承诺；多阶段任务需多次成功。\n\n4  理解机会成本\n一人一夜只做一项地下工作：任务优先于链条。可用资源等于总额减预留；撤回授权会释放预算。\n\n5  轮休与成长\n在人事生活页安排政策。生活优先会暂停地下工作；空闲成员处理生活、恢复状态并培养身份。留意暴露预警和救援期限。\n\n6  推进并复盘\n夜前检查岗位冲突与截止事项，确认后才掷骰执行。报告默认按夜展示结果与原因，完整骰面可展开；下一夜重新决定。地区经营提供盟友收益，同时追查会迫使据点撤离。\n\n操作提示\n浅纸色表示当前选项，朱红按钮表示提交操作；风险同时配有文字说明。Esc 返回，F5 保存，F9 读取；滚动条可拖动。外观只影响形象，不影响能力。",0,0,812,850,21);
            float guideHeight=UnityEngine.Mathf.Max(850,guideText.preferredHeight+16);guideText.rectTransform.sizeDelta=new UnityEngine.Vector2(812,guideHeight);list.sizeDelta=new UnityEngine.Vector2(list.sizeDelta.x,guideHeight);
            Button("CommandGuideBack",p,"返回指挥",28,669,844,48,()=>{modal="";Draw();});
        }
    }
}
