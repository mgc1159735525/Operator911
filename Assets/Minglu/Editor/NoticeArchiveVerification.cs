using System;
using System.Linq;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class NoticeArchiveVerification
    {
        public static void Verify()
        {
            var s=RunLifecycle.Create(ConfigStore.Load(),1921,0,0,false);CommandRules.Initialize(s);
            uint random=s.random;
            CommandRules.Notice(s,new CommandNotice{title="任职完成",body="成员已在机构工作。",actor=0,tab=2});
            CommandRules.Notice(s,new CommandNotice{title="联络链中断",body="请安排替补。",tab=1});
            if(s.command.noticeArchive.Count!=2)throw new Exception("Both notices must be archived before dismissal");
            s.command.notices.Clear();CommandRules.Validate(s);
            if(s.command.noticeArchive.Count!=2||s.random!=random)throw new Exception("Dismissal and archive validation changed history or RNG");
            var loaded=JsonUtility.FromJson<RunState>(JsonUtility.ToJson(s));CommandRules.Validate(loaded);
            if(loaded.command.noticeArchive[0].actor!=0||loaded.command.noticeArchive[1].body!="请安排替补。")throw new Exception("Archive destinations must survive reload");
            s.command.noticeArchive=null;s.command.notices.Add(new CommandNotice{title="旧档未读",body="旧消息也保留",tab=4});
            CommandRules.EnsureNoticeArchive(s);string once=JsonUtility.ToJson(s.command);CommandRules.EnsureNoticeArchive(s);
            if(s.command.noticeArchive.Count!=1||JsonUtility.ToJson(s.command)!=once)throw new Exception("Old notice migration must be idempotent");
            s.command.notices.Clear();
            for(int i=0;i<90;i++){CommandRules.Notice(s,new CommandNotice{title="通知"+i,body="独立事项"+i});s.command.notices.Clear();}
            if(s.command.noticeArchive.Count!=CommandRules.NoticeArchiveLimit||s.command.noticeArchive.First().title!="通知10"||s.command.noticeArchive.Last().title!="通知89")throw new Exception("Archive must retain the latest bounded history");
            Debug.Log("MINGLU_NOTICE_ARCHIVE_VERIFY_SUCCESS: dismissal, reload, migration, RNG and 80-entry retention");
        }
    }
}
