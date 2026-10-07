using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    public static class PrototypeSave
    {
        public static string PathName { get { return Path.Combine(MingluStorage.Directory, "minglu-city-v3.json"); } }
        public static void Write(RunState state, string path)
        {
            SaveFileRecovery.Write(state,path,ReadStrict);
        }
        public static RunState Read(string path)
        {
            string recoveryMessage;
            return ReadWithRecovery(path,out recoveryMessage);
        }
        public static RunState ReadWithRecovery(string path,out string recoveryMessage)
        {
            return SaveFileRecovery.Read(path,ReadStrict,"对局存档",out recoveryMessage);
        }
        static RunState ReadStrict(string path)
        {
            var s = JsonUtility.FromJson<RunState>(File.ReadAllText(path));
            if (s == null || s.version != 3 || s.config == null) throw new InvalidDataException("存档格式版本不匹配，当前对局未被替换。");
            // Command runs retain departed members in historical dossiers.
            // Their occupied capacity is checked by CommandRules.Validate.
            ConfigStore.Validate(s.config,s.command!=null);
            if(s.roguelikeRun&&(string.IsNullOrEmpty(s.runId)||s.initialSeed==0||s.difficulty<0||s.difficulty>2||s.doctrine<0||s.doctrine>2))throw new InvalidDataException("新局初始化信息无效。");
            if(s.agents==null||s.agents.Count==0||s.agents.Any(a=>a==null)||s.journal==null||s.intelligence<0||s.progress<0||s.pressure<0||s.pressure>100||s.agents.Select(a=>a.id).Distinct().Count()!=s.agents.Count)throw new InvalidDataException("存档中的组织或资源数据无效。");
            if(s.command!=null){CommandRules.Validate(s);return s;}
            if (s.turn < 1 || s.turn > s.config.Value("turns") || s.random == 0 ||
                s.agents == null || s.agents.Count != s.config.agents.Count || s.journal == null || s.intelligence < 0 || s.progress < 0 || s.pressure < 0 || s.pressure > 100 || s.completedEvents == null ||
                s.agents.Any(a => a == null || !s.config.agents.Any(x => x.id == a.id) || !s.config.districts.Any(x => x.id == a.district) || a.stamina < 0 || a.stamina > 100 || a.exposure < 0 || a.exposure > 100 || a.interrogation < 0 || a.consecutiveShifts < 0 || a.consecutiveShifts > s.config.Value("turns") || !Enum.IsDefined(typeof(Condition), a.condition) || !Enum.IsDefined(typeof(Duty), a.order)) ||
                s.agents.Select(a => a.id).Distinct().Count() != s.agents.Count)
                throw new InvalidDataException("存档版本或内容无效，当前对局未被替换。");
            if (PrototypeRules.UsedAP(s) > s.config.Value("ap") || PrototypeRules.ReservedIntel(s) > s.intelligence ||
                s.agents.Any(a => a.handler != -1 && !s.agents.Any(b => b.id == a.handler && b.id != a.id)) ||
                s.agents.Any(a => a.order == Duty.Rescue && !s.agents.Any(b => b.id == a.target && b.condition == Condition.Captured)))
                throw new InvalidDataException("存档行动或联系数据不合法。");
            if ((!string.IsNullOrEmpty(s.pendingEvent) && !s.config.events.Any(e => e.id == s.pendingEvent)) || s.completedEvents.Any(id => !s.config.events.Any(e => e.id == id))) throw new InvalidDataException("存档事件引用不存在。");
            DispatchRules.ValidateSave(s);
            OrganizationRules.ValidateSave(s);
            CampaignRules.ValidateSave(s);
            CityContentRules.ValidateSave(s);
            CourierRules.ValidateSave(s);
            AppearanceRules.Validate(s.chiefLook);
            foreach(var a in s.agents)AppearanceRules.Validate(a.appearance);
            return s;
        }
    }

    // Both the run and the player profile use the same recovery contract. A read
    // never repairs files in place; only an explicit save replaces the primary.
    internal static class SaveFileRecovery
    {
        internal static T Read<T>(string path,Func<string,T> readStrict,string label,out string recoveryMessage)
        {
            recoveryMessage="";
            Exception primaryError;
            try{return readStrict(path);}
            catch(Exception e){primaryError=e;}
            string backup=path+".bak";
            try
            {
                T value=readStrict(backup);
                recoveryMessage=label+"无法读取，已恢复上一份有效备份；进度可能略早，原文件仍保留。下次保存时会保留损坏原件。";
                return value;
            }
            catch(Exception backupError)
            {
                throw new InvalidDataException(label+"及其备份均无法读取，文件未被改动。主档："+primaryError.Message+"；备份："+(File.Exists(backup)?backupError.Message:"尚无备份。"),primaryError);
            }
        }

        internal static void Write<T>(T value,string path,Func<string,T> readStrict)
        {
            string temp=path+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(temp,JsonUtility.ToJson(value,true));
                // Reject invalid snapshots before touching either known save.
                readStrict(temp);
                if(!File.Exists(path)){File.Move(temp,path);return;}
                bool primaryValid;
                try{readStrict(path);primaryValid=true;}
                catch(Exception){primaryValid=false;}
                if(primaryValid)File.Replace(temp,path,path+".bak");
                else
                {
                    // Never rotate a damaged primary over the only good backup.
                    // Replace atomically and retain the damaged bytes separately.
                    string damaged=path+".corrupt-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
                    File.Replace(temp,path,damaged);
                }
            }
            finally
            {
                // The exact transaction file belongs to this write; do not scan
                // or delete any other save, backup, or recovered original.
                try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}
            }
        }
    }
}
