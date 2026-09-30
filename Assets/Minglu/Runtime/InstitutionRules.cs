using System;
using System.Linq;
namespace Minglu
{
    // Fictional employers. Profession is a skill background; employment is access.
    public static partial class CommandRules
    {
        public static readonly string[] Institutions={"民间任职","敌方政务署","敌方税务署","敌方巡防局","敌方驻军司令部"};
        public static readonly string[] InstitutionIntel={"不产出机构情报","行政命令与人事动向","征收与物资账目","巡防调动与拘押通告","军需与驻军调令"};
        public static bool HasInstitution(CommandPerson p){return p!=null&&p.institution>0&&p.institution<Institutions.Length;}
        public static string Employment(CommandPerson p){return Institutions[p!=null&&HasInstitution(p)?p.institution:0];}
        public static string SourceError(RunState s,int id)
        {
            var p=s.command.people.FirstOrDefault(x=>x.id==id);
            return HasInstitution(p)?"":"内线须在敌方机构任职；到人事页建立任职身份";
        }
        public static string EmploymentError(RunState s,int id,int institution)
        {
            string error=Availability(s,id);if(error!="")return error;
            if(institution<1||institution>=Institutions.Length)return "请选择敌方机构";
            var p=Person(s,id);if(HasInstitution(p))return "已有敌方任职身份，可直接在联络网安排为内线";
            if(s.command.tasks.Any(t=>t.status==0&&t.operation==4&&t.target==id))return "已有任职任务，请到任务簿安排执行";
            if(s.command.tasks.Count(t=>t.status==0)>=9)return "待办已满9项，请先处理现有任务";
            if(s.config.Value("turns")-s.turn+1<4)return "不足四夜：任职加首次传递已无完整窗口";
            return "";
        }
        public static string StartEmployment(RunState s,int id,int institution)
        {
            string error=EmploymentError(s,id,institution);if(error!="")return error;
            var a=Agent(s,id);
            CreateTask(s,new CommandTemplate{title=a.name+" · "+Institutions[institution].Replace("敌方","")+"任职",body="指定本人任职，需2个说服成功。成功后配置联络网，不直接获得情报。",category="机构任职",skill=2,required=2,reward=0,money=0,failExposure=4},"人事安排",id,s.command.strategy.baseDistrict);
            var t=s.command.tasks.Last();t.operation=4;t.target=id;t.institution=institution;
            Log(s,"已建立「"+t.title+"」；需在任务簿签发，尚未取得任职资格。");return "";
        }
        static void UpgradeInstitutions(RunState s)
        {
            if(s.command.institutionRevision>=1)return;
            foreach(var t in s.command.tasks)t.intelReward=0;
            foreach(var t in s.command.config.templates)t.intelReward=0;
            foreach(var r in s.command.routes.Where(r=>r.source>=0&&SourceError(s,r.source)!=""))
            {r.enabled=false;r.waiting=0;r.atRelay=0;r.age=0;r.last="旧链缺少敌方任职来源，已暂停；建立内线身份后重新配置。";}
            s.command.institutionRevision=1;
            Notice(s,new CommandNotice{title="情报来源规则已更新",body="内线必须在敌方机构任职。原有未核实来源的链条已暂停并清除待传积压；已送达情报和战绩保留。\n到人事档案，为一名成员建立任职身份，成功后重新启用链条。普通任务和日常生活不再直接产出情报。",tab=2});
        }
    }
}
