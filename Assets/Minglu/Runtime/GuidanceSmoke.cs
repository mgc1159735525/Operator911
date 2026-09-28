using System;
using System.Collections;
using System.IO;
using System.Linq;
namespace Minglu
{
    public partial class CityGameUI
    {
        IEnumerator GuidanceSmoke(string path)
        {
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            Capture(Path.ChangeExtension(path,"first-night.png"));
            yield return Click("GuideAssign");
            if(!State.agents.Any(a=>a.organizationDuty==OrganizationDuty.ReceivePacket))throw new Exception("Guide failed to assign receipt");
            yield return Click("EndTurn");Capture(Path.ChangeExtension(path,"confirmation.png"));yield return Click("ExecuteTurn");yield return Click("ContinueReport");
            if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            yield return Click("Tab_6");Capture(Path.ChangeExtension(path,"send-next.png"));yield return Click("GuideAssign");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(CourierRules.Delivered(State)!=1)throw new Exception("Guide did not deliver");
            State.security.trace=78;yield return Click("Tab_6");Capture(Path.ChangeExtension(path,"security-warning.png"));yield return Click("GuideMove");
            yield return Click("Save");yield return Click("Load");
            if(!State.agents.Any(a=>a.organizationDuty==OrganizationDuty.RelocateStation))throw new Exception("Move order not restored");
            yield return Click("EndTurn");yield return Click("ExecuteTurn");yield return Click("ContinueReport");if(!string.IsNullOrEmpty(State.pendingEvent))yield return Click("EventB");
            if(State.security.generation!=2||State.security.trace>33)throw new Exception("Move failed");
            yield return Click("Tab_6");Capture(Path.ChangeExtension(path,"station-moved.png"));yield return Click("GuideMain");Capture(Path.ChangeExtension(path,"main-goal.png"));
            foreach(string f in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(f))File.Delete(f);
            UnityEngine.Debug.Log("MINGLU_GUIDANCE_UI_SUCCESS");
        }
    }
}
