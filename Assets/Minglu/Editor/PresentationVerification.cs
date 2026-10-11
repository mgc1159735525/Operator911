using System;
using UnityEditor;
using UnityEngine;
namespace Minglu.EditorTools
{
    public static class PresentationVerification
    {
        public static void BatchDeliver()
        {
            try
            {
                AppearanceVerification.Verify();
                AppearanceCatalogVerification.Verify();
                GroomingAgeVerification.Verify();
                ClassicPortraitVerification.Verify();
                InstitutionVerification.Verify();
                StrategyVerification.Verify();
                MissionVerification.Verify();
                CommandVerification.Verify();
                TutorialVerification.Verify();
                ExperienceVerification.Verify();
                RecruitmentVerification.Verify();
                PlanningVerification.Verify();
                CommandWorkOverviewVerification.Verify();
                CommandOutcomeVerification.Verify();
                CampaignCommandVerification.Verify();
                NoticeArchiveVerification.Verify();
                SaveRecoveryVerification.Verify();
                CampaignBalanceVerification.Verify();
                PrototypeTools.BatchBuild();EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
