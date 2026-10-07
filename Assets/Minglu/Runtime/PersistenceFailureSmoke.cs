using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Minglu
{
    public partial class CityGameUI
    {
        void RequirePersistenceWarning(string expected)
        {
            if(!PersistenceFailurePending||!toast.StartsWith("保存失败：")||!persistenceFailure.Contains(expected))
                throw new Exception("Persistence failure was replaced by success feedback: "+toast);
            if(!root.GetComponentsInChildren<Button>().Any(b=>b.name=="PersistenceRetry")||root.Find("PersistenceFailureShade")==null)
                throw new Exception("Persistence retry is not visible above the current screen");
        }
        IEnumerator PersistenceFailureSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Persistence smoke requires isolated saves");
            string runBlock=SavePath+".tmp",profileBlock=ProfilePath+".tmp";
            try
            {
                yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
                if(!PersistProfile())throw new Exception("Persistence fixture could not create its isolated profile");
                uint random=State.random;int night=State.turn;

                // A directory at the exact transaction-file path deterministically
                // denies writes without changing machine permissions or real saves.
                selectedAgent=State.agents.Last().id;modal="memberDetails";Draw();yield return null;
                string before=File.ReadAllText(SavePath);
                Directory.CreateDirectory(runBlock);
                yield return Click("LifePolicy_2");RequirePersistenceWarning("当前安排");
                if(File.ReadAllText(SavePath)!=before)throw new Exception("Failed policy save replaced the valid primary");
                yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"policy-save-failure.png"));
                yield return Click("PersistenceStay");
                if(PersistenceFailurePending||modal!="memberDetails")throw new Exception("Storage warning trapped the player or lost the member dossier");
                Directory.Delete(runBlock);
                Save();yield return null;
                if(retryRunSave||CommandRules.Person(PrototypeSave.Read(SavePath),selectedAgent).policy!=2)
                    throw new Exception("Retry did not save the original policy change");

                yield return Click("MemberDetailsClose");yield return SmokeOpenBusiness(1);
                var route=State.command.routes[0];route.source=0;route.liaison=1;route.courier=2;
                Draw();yield return null;Directory.CreateDirectory(runBlock);
                yield return Click("RouteEnable_0");RequirePersistenceWarning("当前安排");
                if(!route.enabled)throw new Exception("Route enable fixture did not reach the save failure");
                Directory.Delete(runBlock);yield return Click("PersistenceRetry");
                if(PersistenceFailurePending||!PrototypeSave.Read(SavePath).command.routes[0].enabled)
                    throw new Exception("Route retry failed to save the original enable decision");

                yield return SmokeReturnToMap();yield return Click("CommandMenu");yield return Click("CommandSettings");
                float volume=profile.volume;string oldProfile=File.ReadAllText(ProfilePath);
                Directory.CreateDirectory(profileBlock);yield return Click("VolumeDown");RequirePersistenceWarning("设置尚未");
                if(File.ReadAllText(ProfilePath)!=oldProfile)throw new Exception("Failed settings save changed the valid profile");
                yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"settings-save-failure.png"));
                Directory.Delete(profileBlock);yield return Click("PersistenceRetry");
                if(PersistenceFailurePending||Mathf.Abs(RunLifecycle.ReadProfile(ProfilePath).volume-(volume-.1f))>.001f)
                    throw new Exception("Settings failure did not recover from inside its game modal");

                yield return SmokeReturnToMap();
                Directory.CreateDirectory(runBlock);
                if(TryPrepareQuit()||quitApproved)throw new Exception("Window-close gate approved exit without a saved run");
                RequirePersistenceWarning("当前安排");
                yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"quit-save-failure.png"));
                Directory.Delete(runBlock);yield return Click("PersistenceRetry");
                if(State.random!=random||State.turn!=night)throw new Exception("Storage retries advanced the simulation");

                // Finishing is a fixture; returning and retrying use the actual
                // controls. A failed archive must not replace the saved ending.
                Directory.CreateDirectory(profileBlock);
                State.ended=true;State.won=false;State.ending="存档故障注入验收";modal="";Draw();yield return null;
                string runId=State.runId;
                RequirePersistenceWarning("本局战绩");yield return Click("PersistenceStay");
                yield return Click("CommandEndHome");RequirePersistenceWarning("本局战绩");
                if(atTitle||profile.records.Any(r=>r.id==runId))throw new Exception("Failed ending archive allowed leaving or mutated the profile");
                string endingSave=File.ReadAllText(SavePath);
                BeginConfiguredRun();
                if(State.runId!=runId||File.ReadAllText(SavePath)!=endingSave)throw new Exception("New run replaced an unarchived ending");
                yield return new WaitForEndOfFrame();Capture(Path.ChangeExtension(path,"ending-save-failure.png"));
                Directory.Delete(profileBlock);yield return Click("PersistenceRetry");
                if(profile.records.Count(r=>r.id==runId)!=1||RunLifecycle.ReadProfile(ProfilePath).records.Count(r=>r.id==runId)!=1)
                    throw new Exception("Archive retry lost or duplicated the run record");
                yield return Click("CommandEndHome");yield return Click("HomeContinue");
                if(profile.records.Count(r=>r.id==runId)!=1)throw new Exception("Reloading ending duplicated its archive record");

                Directory.CreateDirectory(profileBlock);
                if(TryPrepareQuit()||quitApproved)throw new Exception("Window-close gate ignored a failed settings write");
                RequirePersistenceWarning("设置尚未");Directory.Delete(profileBlock);yield return Click("PersistenceRetry");
                if(!TryPrepareQuit())throw new Exception("Recovered saves never allowed a normal close");
                var runStamp=File.GetLastWriteTimeUtc(SavePath);var profileStamp=File.GetLastWriteTimeUtc(ProfilePath);
                yield return new WaitForSeconds(.03f);
                if(!TryPrepareQuit())throw new Exception("An approved close was not idempotent");
                OnApplicationQuit();
                if(File.GetLastWriteTimeUtc(SavePath)!=runStamp||File.GetLastWriteTimeUtc(ProfilePath)!=profileStamp)
                    throw new Exception("Approved quit wrote twice and rotated the recovery backup");
                Debug.Log("MINGLU_PERSISTENCE_FAILURE_UI_SUCCESS: real policy/route/settings controls, injected write failure, visible retry, unchanged valid saves/RNG/night, window-close gate, ending archive gate, deduplicated retry and quit");
            }
            finally
            {
                quitApproved=false;
                foreach(string folder in new[]{runBlock,profileBlock})if(Directory.Exists(folder))Directory.Delete(folder);
                foreach(string file in new[]{SavePath,SavePath+".bak",ProfilePath,ProfilePath+".bak"})if(File.Exists(file))File.Delete(file);
            }
        }
    }
}
