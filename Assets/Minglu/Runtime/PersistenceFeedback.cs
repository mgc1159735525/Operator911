using System;
using UnityEngine;

namespace Minglu
{
    public partial class CityGameUI
    {
        bool retryRunSave,retryProfileSave,retryEndingSave,showPersistenceFailure;
        bool quitGuardInstalled,quitApproved;
        string persistenceFailure="",persistenceDetail="";
        bool PersistenceFailurePending { get { return showPersistenceFailure; } }

        void InstallQuitGuard()
        {
            if(quitGuardInstalled)return;
            Application.wantsToQuit+=HandleQuitRequest;quitGuardInstalled=true;
        }
        void RemoveQuitGuard()
        {
            if(!quitGuardInstalled)return;
            Application.wantsToQuit-=HandleQuitRequest;quitGuardInstalled=false;
        }
        bool HandleQuitRequest()
        {
            // Automated captures use their isolated files and must be able to
            // terminate even when deliberately injecting a storage failure.
            return smokeSave!=null||TryPrepareQuit();
        }
        bool TryPrepareQuit()
        {
            if(quitApproved)return true;
            if(((atTitle&&homePage=="appearance")||modal=="appearance")&&LookDirty())
            {
                lookPopup="discard";lookNotice="外观尚未应用。请先应用或放弃修改，再关闭游戏。";
                Draw();return false;
            }
            if(State!=null&&State.roguelikeRun&&!SaveForExit())return false;
            if(!PersistProfile()){Draw();return false;}
            quitApproved=true;return true;
        }
        void ReportPersistenceFailure(string message,Exception error)
        {
            persistenceFailure=message;persistenceDetail=error.Message;
            toast=homeNotice="保存失败："+message;
            showPersistenceFailure=true;
            Debug.LogWarning("MINGLU_SAVE_RETRY_REQUIRED: "+message+" "+error.Message);
        }
        void ClearResolvedPersistenceFailure()
        {
            if(retryRunSave||retryProfileSave||retryEndingSave)return;
            if(!string.IsNullOrEmpty(persistenceFailure))
            {
                toast="保存已恢复，当前安排和待存内容已写入磁盘。";
                if(homeNotice.StartsWith("保存失败："))homeNotice="";
            }
            persistenceFailure="";persistenceDetail="";showPersistenceFailure=false;
        }
        void RetryPersistence()
        {
            if(retryRunSave)Save(false);
            if(retryEndingSave)RecordEnding(true);
            if(retryProfileSave)PersistProfile();
            ClearResolvedPersistenceFailure();Draw();
        }
        void DrawPersistenceFailure()
        {
            if(!showPersistenceFailure)return;
            var shade=Box("PersistenceFailureShade",root,0,0,1600,900,new Color(0,0,0,.86f));
            var panel=Box("PersistenceFailurePanel",shade,340,220,920,460,Paper);
            SurfaceTheme(panel,true);
            Label(panel,"保存未完成",32,25,856,50,31,PaperInk);
            Label(panel,persistenceFailure,32,101,856,80,22,PaperInk);
            Label(panel,"请检查磁盘空间或文件占用，再重试。重试不会推进夜晚、重新掷骰或重复归档。",32,192,856,62,18,PaperMuted);
            var detail=Scroll(panel,"PersistenceDetail",32,268,856,59,59);
            var error=Label(detail,persistenceDetail,0,0,820,59,14,PaperMuted);
            float height=Mathf.Max(59,error.preferredHeight+8);error.rectTransform.sizeDelta=new Vector2(820,height);detail.sizeDelta=new Vector2(detail.sizeDelta.x,height);
            Button("PersistenceStay",panel,"留在当前界面",32,375,310,52,()=>{showPersistenceFailure=false;Draw();});
            Button("PersistenceRetry",panel,"重试保存",366,375,522,52,RetryPersistence,true,true);
        }
    }
}
