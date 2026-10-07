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
                AppearanceCatalogVerification.Verify();
                GroomingAgeVerification.Verify();
                PaintedPortraitVerification.Verify();
                InstitutionVerification.Verify();
                StrategyVerification.Verify();
                MissionVerification.Verify();
                CommandVerification.Verify();
                ExperienceVerification.Verify();
                RecruitmentVerification.Verify();
                PlanningVerification.Verify();
                PrototypeTools.BatchBuild();EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
