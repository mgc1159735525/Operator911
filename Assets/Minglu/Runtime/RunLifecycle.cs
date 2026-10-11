using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    [Serializable] public class RunRecord { public string id, date, doctrine; public uint seed; public int difficulty, progress, turn; public bool won; }
    [Serializable] public class PlayerProfile
    {
        public Appearance chiefLook;
        public Appearance[] lookSlots=new Appearance[3];
        public int lookSlotRevision,lookSlotMask;
        public int version=1;
        public bool sound=true, fullscreen;
        public float volume=.7f;
        public List<RunRecord> records=new List<RunRecord>();
    }
    public static class RunLifecycle
    {
        public static RunState Create(GameConfig source, uint seed, int difficulty, int doctrine, bool crisis)
        {
            if(seed==0||difficulty<0||difficulty>2||doctrine<0||doctrine>2)throw new ArgumentException("新局参数无效。");
            var c=JsonUtility.FromJson<GameConfig>(JsonUtility.ToJson(source));
            c.settings.First(x=>x.id=="goal").value+=difficulty==0?-4:difficulty==2?8:0;
            c.settings.First(x=>x.id=="startingPressure").value=Math.Max(0,Math.Min(100,c.Value("startingPressure")+(difficulty-1)*5));
            // World randomization uses its own stream, so opening menus never consumes action rolls.
            uint world=seed;
            foreach(var d in c.districts)
            {
                world=world*1664525u+1013904223u;
                d.risk=Math.Max(0,Math.Min(30,d.risk+(int)(world%7)-3));
                d.description="本局巡查已变化：额外暴露+"+d.risk+"，成功需求+"+d.difficulty+"。\n侦察情报加成+"+d.reconBonus+"，破坏成果加成+"+d.sabotageBonus+"。";
            }
            if(doctrine==1)c.settings.First(x=>x.id=="startingIntel").value+=2;
            if(doctrine==2)c.settings.First(x=>x.id=="startingPressure").value=Math.Max(0,c.Value("startingPressure")-8);
            ConfigStore.Validate(c);
            var s=PrototypeRules.NewRun(crisis,seed,c,true,true);
            s.runId=Guid.NewGuid().ToString("N");s.initialSeed=seed;s.difficulty=difficulty;s.doctrine=doctrine;s.roguelikeRun=true;
            OrganizationRules.Initialize(s);
            if(doctrine==1)foreach(var a in s.agents.Where(a=>a.condition==Condition.Active))a.exposure=10;
            if(doctrine==2)foreach(var a in s.agents.Where(a=>a.condition==Condition.Active))a.stamina=85;
            s.journal.Add("[1] 新局种子 "+seed+"；"+DifficultyName(difficulty)+"；"+DoctrineName(doctrine)+"。各区暴露修正已由种子生成，以地图数值为准。");
            return s;
        }
        public static string DifficultyName(int n){return new[]{"初探","标准","险境"}[n];}
        public static string DoctrineName(int n){return new[]{"均衡联络","情报先行","隐蔽入城"}[n];}
        public static PlayerProfile ReadProfile(string path)
        {
            string recoveryMessage;
            return ReadProfileWithRecovery(path,out recoveryMessage);
        }
        public static PlayerProfile ReadProfileWithRecovery(string path,out string recoveryMessage)
        {
            recoveryMessage="";
            if(!File.Exists(path)&&!File.Exists(path+".bak"))return new PlayerProfile();
            return SaveFileRecovery.Read(path,ReadProfileStrict,"设置与人物预设",out recoveryMessage);
        }
        static PlayerProfile ReadProfileStrict(string path)
        {
            var p=JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path));
            if(p==null||p.version!=1||p.records==null||float.IsNaN(p.volume)||p.volume<0||p.volume>1||p.records.Any(r=>r==null||string.IsNullOrEmpty(r.id)||r.seed==0)||p.records.Select(r=>r.id).Distinct().Count()!=p.records.Count)throw new InvalidDataException("档案格式无效。");
            AppearanceRules.Validate(p.chiefLook);
            if(p.lookSlots==null)p.lookSlots=new Appearance[3];
            if(p.lookSlots.Length!=3)throw new InvalidDataException("外观预设数量无效。");
            foreach(var look in p.lookSlots)AppearanceRules.Validate(look);
            if(p.lookSlotRevision<0||p.lookSlotRevision>1||p.lookSlotMask<0||p.lookSlotMask>7)throw new InvalidDataException("外观预设占用数据无效。");
            for(int i=0;i<3;i++)
                if(p.lookSlotRevision==1?(p.lookSlotMask&(1<<i))==0:IsEmptyLookSlot(p.lookSlots[i]))p.lookSlots[i]=null;
            return p;
        }
        public static bool IsEmptyLookSlot(Appearance a)
        {
            // JsonUtility materializes null class entries as an all-default
            // object. Only this exact untouched legacy placeholder is empty.
            return a==null||(!a.enabled&&a.portraitVersion==0&&a.style==0&&a.sex==0&&a.ears==0&&!a.featureEditing&&!a.groomingEditing&&a.parts!=null&&a.parts.All(n=>n==0));
        }
        public static PlayerProfile CopyProfile(PlayerProfile source)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            // Never serialize the live profile as a copying mechanism:
            // Unity's inline class serialization can materialize null slots.
            // Also normalize placeholders left by an earlier serialization.
            return new PlayerProfile
            {
                version=source.version,sound=source.sound,fullscreen=source.fullscreen,volume=source.volume,
                lookSlotRevision=source.lookSlotRevision,lookSlotMask=source.lookSlotMask,
                chiefLook=source.chiefLook==null?null:AppearanceRules.Copy(source.chiefLook),
                lookSlots=source.lookSlots==null?new Appearance[3]:source.lookSlots.Select(look=>IsEmptyLookSlot(look)?null:AppearanceRules.Copy(look)).ToArray(),
                records=source.records==null?null:source.records.Select(record=>record==null?null:new RunRecord
                {id=record.id,date=record.date,doctrine=record.doctrine,seed=record.seed,difficulty=record.difficulty,progress=record.progress,turn=record.turn,won=record.won}).ToList()
            };
        }
        public static void WriteProfile(PlayerProfile p,string path)
        {
            var snapshot=CopyProfile(p);snapshot.lookSlotRevision=1;snapshot.lookSlotMask=0;
            for(int i=0;i<snapshot.lookSlots.Length;i++)if(snapshot.lookSlots[i]!=null)snapshot.lookSlotMask|=1<<i;
            SaveFileRecovery.Write(snapshot,path,ReadProfileStrict);
        }
        public static bool Record(PlayerProfile p,RunState s)
        {
            if(!s.ended||!s.roguelikeRun||TutorialRules.IsTutorial(s)||string.IsNullOrEmpty(s.runId)||p.records.Any(r=>r.id==s.runId))return false;
            p.records.Add(new RunRecord{id=s.runId,date=DateTime.Now.ToString("yyyy-MM-dd HH:mm"),seed=s.initialSeed,difficulty=s.difficulty,doctrine=DoctrineName(s.doctrine),turn=s.turn,progress=s.progress,won=s.won});
            return true;
        }
    }
}
