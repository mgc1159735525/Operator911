using System;
using System.Linq;
using UnityEngine;
namespace Minglu
{
    public sealed class CommandRecruitPreview
    {
        public Agent agent;
        public CommandPerson person;
    }
    public static partial class CommandRules
    {
        public const int RecruitmentCost=12;
        public static string RecruitError(RunState s)
        {
            if(s.ended)return "本局已结束";
            if(s.command.eventKind!=0)return "先处理待决事件";
            if(s.agents.Count>=s.command.config.capacity)return "组织人数已满";
            if(s.money-ReservedMoney(s)<RecruitmentCost)return "接纳需要12可用银两；可先撤回其他任务预留";
            return "";
        }
        // Preview the very next recruit on a private copy: looking, cancelling,
        // and reopening must not reroll candidates or consume action randomness.
        public static CommandRecruitPreview PreviewRecruit(RunState s)
        {
            if(s.agents.Count>=s.command.config.capacity)return null;
            var copy=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));
            AddPerson(copy);
            return new CommandRecruitPreview{agent=copy.agents.Last(),person=copy.command.people.Last()};
        }
        public static string Recruit(RunState s)
        {
            string error=RecruitError(s);if(error!="")return error;
            AddPerson(s);s.money-=RecruitmentCost;
            var a=s.agents.Last();var p=Person(s,a.id);
            Log(s,"接纳新成员："+a.name+" / "+p.profession+" / "+Employment(p)+"。支出12银两；每夜生活补助增加2银两。");
            return "";
        }
    }
}
