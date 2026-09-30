using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawInstitutionPicker()
        {
            var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,a.id);
            var p=CommandModal("建立机构任职 / "+a.name,710);
            Label(p,"选择目标只生成任务，须由本人执行成功。任职后仍要配置传递链，情报才会送达。",28,89,844,52,19,Muted);
            for(int i=1;i<CommandRules.Institutions.Length;i++)
            {
                int institution=i,y=158+(i-1)*105;string error=CommandRules.EmploymentError(State,a.id,i);
                Rule(p,28,y-6,844);
                Label(p,CommandRules.Institutions[i],28,y+4,545,29,23,Gold);
                Label(p,"情报范围："+CommandRules.InstitutionIntel[i],28,y+38,545,26,17,Muted);
                Label(p,error==""?"本人说服判定 · 需2成功 · 可选筹备提高成功率":error,28,y+68,810,25,16,error==""?Muted:Red);
                Button("InstitutionSelect_"+i,p,"生成任职任务",595,y+11,277,43,()=>{
                    toast=CommandRules.StartEmployment(State,a.id,institution);
                    if(toast==""){FocusCommandTask(State.command.tasks.Last().id);modal="";toast="任职任务已创建，请检查方案后签发给本人。";Save(false);}Draw();
                },error=="");
            }
            Button("InstitutionCancel",p,"返回 · 不建立任务",28,631,844,48,()=>{modal="";Draw();});
        }
        bool HasActiveInsider(){return State.command.people.Any(p=>CommandRules.HasInstitution(p)&&CommandRules.Agent(State,p.id).condition==Condition.Active);}
        void OpenEmployment()
        {
            var pending=State.command.tasks.FirstOrDefault(t=>t.operation==4&&t.target==selectedAgent&&t.status==0);
            if(pending!=null){FocusCommandTask(pending.id);modal="";}else modal="institutionPicker";
            Draw();
        }
    }
}
