using System.Collections.Generic;
using System.Linq;

namespace Minglu
{
    public static partial class CommandRules
    {
        public const int NoticeArchiveLimit=80;
        public static void EnsureNoticeArchive(RunState s)
        {
            if(s.command.noticeArchive==null)s.command.noticeArchive=new List<CommandNotice>();
            if(s.command.notices==null)s.command.notices=new List<CommandNotice>();
            if(s.command.noticeArchive.Any(n=>n==null)||s.command.notices.Any(n=>n==null))throw new System.InvalidOperationException("要报记录无效");
            foreach(var notice in s.command.notices)
            {
                if(notice.night<=0)notice.night=s.turn;
                ArchiveNotice(s,notice);
            }
        }
        static void ArchiveNotice(RunState s,CommandNotice notice)
        {
            var archive=s.command.noticeArchive;
            if(!archive.Any(n=>n.night==notice.night&&n.title==notice.title&&n.body==notice.body&&n.tab==notice.tab&&n.actor==notice.actor&&n.task==notice.task))
                archive.Add(new CommandNotice{night=notice.night,title=notice.title,body=notice.body,tab=notice.tab,actor=notice.actor,task=notice.task});
            if(archive.Count>NoticeArchiveLimit)archive.RemoveRange(0,archive.Count-NoticeArchiveLimit);
        }
    }
}
