using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        void DrawInstitutionPicker()
        {
            var a=CommandRules.Agent(State,selectedAgent);var member=CommandRules.Person(State,a.id);
            var p=CommandModal("建立机构任职 / "+a.name,710);
            Label(p,"本人任职需2个说服成功；选择机构后生成待签发任务。\n任职后仍需接入传递链，情报才会送达。",28,98,844,50,17,Muted);
            for(int i=1;i<CommandRules.Institutions.Length;i++)
            {
                int institution=i,y=174+(i-1)*104;string error=CommandRules.EmploymentError(State,a.id,i);
                Rule(p,28,y-8,844);
                Label(p,CommandRules.Institutions[i],28,y+8,548,28,21,Gold);
                Label(p,CommandRules.InstitutionIntel[i],28,y+49,548,24,17,Muted);
                if(error!="")Label(p,error,28,y+77,844,23,15,Red);
                Button("InstitutionSelect_"+i,p,"建立任职任务  ›",620,y+19,252,40,()=>{
                    toast=CommandRules.StartEmployment(State,a.id,institution);
                    if(toast==""){FocusCommandTask(State.command.tasks.Last().id);toast="任职任务已创建，请检查方案后签发给本人。";Save(false);}Draw();
                },error=="");
            }
            Button("InstitutionCancel",p,"返回个人档案",28,630,276,48,()=>{modal="memberDetails";Draw();});
        }
        bool HasActiveInsider(){return State.command.people.Any(p=>CommandRules.HasInstitution(p)&&CommandRules.Agent(State,p.id).condition==Condition.Active);}
        void OpenEmployment()
        {
            var pending=State.command.tasks.FirstOrDefault(t=>t.operation==4&&t.target==selectedAgent&&t.status==0);
            if(pending!=null){FocusCommandTask(pending.id);}else modal="institutionPicker";
            Draw();
        }
    }
}
