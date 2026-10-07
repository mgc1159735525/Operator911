using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Minglu.EditorTools
{
    public static class SaveRecoveryVerification
    {
        static int checks;
        static void Check(bool condition,string label)
        {
            if(!condition)throw new Exception("Save recovery: "+label);
            checks++;
        }
        static void Reject(Action action,string label)
        {
            bool rejected=false;
            try{action();}catch(Exception){rejected=true;}
            Check(rejected,label);
        }
        static RunState Fresh()
        {
            var config=CampaignRules.BuildConfig(ConfigStore.Load(),CampaignRules.Load());
            var s=RunLifecycle.Create(config,421,1,0,false);
            CommandRules.Initialize(s);
            return s;
        }
        static void VerifyRun(string path)
        {
            var s=Fresh();PrototypeSave.Write(s,path);int originalMoney=s.money;
            s.money+=7;PrototypeSave.Write(s,path);
            string primary=File.ReadAllText(path),backup=File.ReadAllText(path+".bak"),message;
            var read=PrototypeSave.ReadWithRecovery(path,out message);
            Check(read.money==originalMoney+7&&message=="","A valid primary wins over an older valid backup");
            Check(File.ReadAllText(path)==primary&&File.ReadAllText(path+".bak")==backup,"Successful reads do not rewrite either file");

            File.WriteAllText(path,"{ damaged primary");
            read=PrototypeSave.ReadWithRecovery(path,out message);
            Check(read.money==originalMoney&&message.Contains("备份"),"Damaged primary recovers the complete prior command run with a clear notice");
            Check(File.ReadAllText(path)=="{ damaged primary"&&File.ReadAllText(path+".bak")==backup,"Recovery does not silently repair or overwrite user files");
            read.money+=3;PrototypeSave.Write(read,path);
            Check(PrototypeSave.Read(path).money==originalMoney+3&&File.ReadAllText(path+".bak")==backup,"Saving after recovery preserves the only valid backup");
            var damaged=Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileName(path)+".corrupt-*");
            Check(damaged.Length==1&&File.ReadAllText(damaged[0])=="{ damaged primary","The damaged primary is retained byte-for-byte under its own exact filename");

            primary=File.ReadAllText(path);backup=File.ReadAllText(path+".bak");
            var invalid=Fresh();invalid.version=99;
            Reject(()=>PrototypeSave.Write(invalid,path),"An invalid serialized run is rejected before replacing a usable save");
            Check(primary==File.ReadAllText(path)&&backup==File.ReadAllText(path+".bak")&&!File.Exists(path+".tmp"),"Failed validation leaves both valid files untouched and removes its transaction file");
            invalid=Fresh();invalid.runId="";
            Reject(()=>PrototypeSave.Write(invalid,path),"Command saves also validate their run identity");

            File.Delete(path);
            read=PrototypeSave.ReadWithRecovery(path,out message);
            Check(read.money==originalMoney&&message.Contains("备份")&&!File.Exists(path),"A missing primary can recover without recreating it during browsing");
            File.WriteAllText(path,"bad primary");File.WriteAllText(path+".bak","bad backup");
            Reject(()=>PrototypeSave.Read(path),"Two damaged files report failure instead of inventing a fresh run");
            Check(File.ReadAllText(path)=="bad primary"&&File.ReadAllText(path+".bak")=="bad backup","Unrecoverable saves stay available for diagnosis");
        }
        static void VerifyProfile(string path)
        {
            string message;
            var profile=RunLifecycle.ReadProfileWithRecovery(path,out message);
            Check(profile.records.Count==0&&message==""&&!File.Exists(path),"First launch defaults require no files or recovery warning");
            profile.volume=.35f;profile.chiefLook=AppearanceRules.Randomize(843,1);profile.lookSlots[0]=AppearanceRules.Copy(profile.chiefLook);
            profile.records.Add(new RunRecord{id="preserved-result",seed=911,turn=20,progress=45,won=true,doctrine="均衡联络"});
            RunLifecycle.WriteProfile(profile,path);profile.volume=.65f;RunLifecycle.WriteProfile(profile,path);
            string backup=File.ReadAllText(path+".bak");
            Check(RunLifecycle.ReadProfileWithRecovery(path,out message).volume==.65f&&message=="","A valid profile uses its latest settings");
            File.WriteAllText(path,"broken profile");
            var read=RunLifecycle.ReadProfileWithRecovery(path,out message);
            Check(read.volume==.35f&&read.records.Single().id=="preserved-result"&&read.lookSlots[0].sex==1&&message.Contains("备份"),"Profile recovery preserves settings, history, and appearance presets together");
            read.volume=.8f;RunLifecycle.WriteProfile(read,path);
            Check(RunLifecycle.ReadProfile(path).volume==.8f&&File.ReadAllText(path+".bak")==backup,"Recovered-profile save cannot replace the good backup with corrupt data");
            string primary=File.ReadAllText(path);read.version=33;
            Reject(()=>RunLifecycle.WriteProfile(read,path),"Invalid profiles are rejected before replacement");
            Check(File.ReadAllText(path)==primary&&File.ReadAllText(path+".bak")==backup,"Rejected profile write preserves both existing snapshots");
            File.Delete(path);read=RunLifecycle.ReadProfileWithRecovery(path,out message);
            Check(read.records.Count==1&&message.Contains("备份"),"An orphan profile backup is recovered instead of replaced by defaults");
            File.WriteAllText(path,"bad profile");File.WriteAllText(path+".bak","bad profile backup");
            Reject(()=>RunLifecycle.ReadProfile(path),"Two corrupt profiles remain an explicit error");
            Check(File.ReadAllText(path)=="bad profile"&&File.ReadAllText(path+".bak")=="bad profile backup","Failed profile recovery preserves original bytes");
        }
        static void VerifyOldRun(string path)
        {
            var s=Fresh();s.command.experienceRevision=0;s.command.missionRevision=1;s.command.variant=2;
            s.command.config.templates.RemoveAll(t=>t.sourceInstitution>0);
            uint random=s.random;
            // Use the actual legacy serialized form rather than writing a new
            // schema first, so this exercises recovery plus migration together.
            File.WriteAllText(path+".bak",JsonUtility.ToJson(s,true));File.WriteAllText(path,"interrupted legacy save");
            string backup=File.ReadAllText(path+".bak"),message;
            var loaded=PrototypeSave.ReadWithRecovery(path,out message);
            Check(loaded.command.legacySourceSchedule&&loaded.command.experienceRevision==1&&loaded.random==random,"Old command backup migrates without rerolling its future");
            PrototypeSave.Write(loaded,path);var roundtrip=PrototypeSave.Read(path);
            Check(File.ReadAllText(path+".bak")==backup&&JsonUtility.ToJson(loaded.command)==JsonUtility.ToJson(roundtrip.command),"Recovered legacy run roundtrips while retaining its original backup");
            CommandRules.Resolve(loaded);CommandRules.Resolve(roundtrip);
            Check(loaded.random==roundtrip.random&&JsonUtility.ToJson(loaded.command)==JsonUtility.ToJson(roundtrip.command),"Recovery and resave preserve the next command turn exactly");
        }
        static void VerifyFirstWrite(string parent)
        {
            string folder=Path.Combine(parent,"new-player"),path=Path.Combine(folder,"profile.json");
            Check(!Directory.Exists(folder),"First-install fixture begins without a save directory");
            try
            {
                RunLifecycle.WriteProfile(new PlayerProfile(),path);
                Check(File.Exists(path)&&RunLifecycle.ReadProfile(path).version==1,"A fresh installation creates the stable save directory on its first write");
            }
            finally
            {
                if(File.Exists(path))File.Delete(path);
                if(File.Exists(path+".tmp"))File.Delete(path+".tmp");
                if(Directory.Exists(folder))Directory.Delete(folder);
            }
        }
        [MenuItem("Minglu/验证存档恢复与覆盖保护")]
        public static void Verify()
        {
            checks=0;
            string folder=Path.Combine(Application.temporaryCachePath,"minglu-save-recovery-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                VerifyRun(Path.Combine(folder,"run.json"));
                VerifyProfile(Path.Combine(folder,"profile.json"));
                VerifyOldRun(Path.Combine(folder,"legacy.json"));
                VerifyFirstWrite(folder);
            }
            finally
            {
                // The GUID directory was created by this test. No actual player
                // save path is enumerated or written by the verification.
                foreach(string file in Directory.GetFiles(folder))File.Delete(file);
                Directory.Delete(folder);
            }
            Debug.Log("MINGLU_SAVE_RECOVERY_VERIFY_SUCCESS: "+checks+" checks; isolated files only");
        }
    }
}
