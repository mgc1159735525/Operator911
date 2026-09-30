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
                PaintedPortraitVerification.Verify();
                InstitutionVerification.Verify();
                StrategyVerification.Verify();
                MissionVerification.Verify();
                CommandVerification.Verify();
                PrototypeTools.BatchBuild();EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
