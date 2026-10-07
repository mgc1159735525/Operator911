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
        void RequireCampaignLayout()
        {
            Canvas.ForceUpdateCanvases();
            var panel=TaskLayoutRect("CommandModal");
            if(Mathf.Abs(panel.rect.width-1120)>1||panel.rect.height>760)throw new Exception("Campaign dossier lost its bounded two-method layout");
            foreach(var label in panel.GetComponentsInChildren<Text>())
            {
                if(string.IsNullOrWhiteSpace(label.text))continue;
                if(label.preferredHeight>label.rectTransform.rect.height+3)
                    throw new Exception("Campaign text is clipped: "+label.name+" / "+label.text+" / "+label.preferredHeight+" > "+label.rectTransform.rect.height);
            }
            var close=ProducerButton("CampaignClose");
            if(close==null||close.GetComponentInParent<ScrollRect>()!=null)throw new Exception("Campaign return action must remain outside scrolling text");
        }

        IEnumerator CampaignExperienceSmoke(string path)
        {
            if(string.IsNullOrEmpty(smokeSave))throw new Exception("Campaign smoke requires isolated saves");
            yield return Click("HomeNew");seedText="911";yield return Click("BeginRun");
            if(!CommandRules.HasCommandCampaign(State)||commandTab!=3||modal!="")throw new Exception("New 20-night campaign did not begin on the map");
            State.command.config.eventChance=0;
            string opening=JsonUtility.ToJson(State);int turn=State.turn,money=State.money;uint random=State.random;
            Capture(Path.ChangeExtension(path,"campaign-map.png"));
            yield return Click("CommandOverview");yield return RequireMapDeskBlocked();RequireCampaignLayout();
            if(ProducerButton("CampaignApproach_0").interactable||!ProducerButton("CampaignApproach_1").interactable)
                throw new Exception("First chapter must explain the missing evidence and expose its accessible alternative");
            Capture(Path.ChangeExtension(path,"campaign-no-evidence.png"));
            yield return Click("CampaignPage_1");RequireCampaignLayout();
            if(!root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("尚无送达档案")))throw new Exception("Empty evidence archive lacks actionable guidance");
            Capture(Path.ChangeExtension(path,"campaign-empty-archive.png"));
            yield return Click("CampaignPage_2");RequireCampaignLayout();
            if(!root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("完整胜利")))throw new Exception("Campaign hides its organizational win requirements");
            yield return Click("CampaignOrganization");yield return Click("OverviewClose");
            if(modal!=""||commandTab!=3)throw new Exception("Organization overview did not return to the map");
            yield return Click("CommandOverview");
            if(State.turn!=turn||State.money!=money||State.random!=random)throw new Exception("Reading campaign pages changed time, money or random rolls");
            yield return Click("CampaignApproach_1");
            var task=State.command.tasks.Single(t=>t.id==State.command.campaign.activeTask);
            if(modal!="missionPlan"||task.stepsRequired!=2||task.actor>=0||State.money!=money||State.turn!=turn)
                throw new Exception("Choosing an alternative should create an unassigned two-stage task without spending time or money");
            Capture(Path.ChangeExtension(path,"campaign-alternative-task.png"));
            yield return Click("MissionBack");yield return Click("CommandOverview");
            uint beforeCancel=State.random;
            yield return Click("CampaignChangeApproach");
            if(State.command.campaign.activeTask>=0||ProducerButton("CampaignApproach_1")==null||State.random!=beforeCancel||State.money!=money||State.turn!=turn)
                throw new Exception("Changing an unstarted campaign method did not restore selection without spending time, resources or random rolls");
            yield return Click("CampaignApproach_1");
            task=State.command.tasks.Single(t=>t.id==State.command.campaign.activeTask);
            yield return Click("CommandAuthorize");
            if(task.actor<0)throw new Exception("Campaign task could not be signed through the existing task dossier");
            yield return SmokeSaveLoad();
            if(State.command.campaign.activeTask!=task.id||State.command.tasks.First(t=>t.id==task.id).actor<0)
                throw new Exception("Campaign choice or task assignment was lost when loading");
            yield return Click("CommandOverview");RequireCampaignLayout();
            if(ProducerButton("CampaignTask")==null)throw new Exception("Campaign dossier lost the active task continuation");
            if(ProducerButton("CampaignChangeApproach").interactable)throw new Exception("Signed campaign task can be cancelled without withdrawing authorization");
            Capture(Path.ChangeExtension(path,"campaign-active-task.png"));
            yield return Click("CampaignTask");yield return Click("MissionBack");
            if(commandTab!=3||modal!="")throw new Exception("Campaign task did not return to its map context");

            // Build the evidence fixture through the same source -> relay -> delivery hooks as real execution.
            State=JsonUtility.FromJson<RunState>(opening);
            int institution=CommandRules.CampaignCurrentChapter(State).approaches[0].evidenceInstitution;
            var route=State.command.routes[0];route.source=0;
            CommandRules.Person(State,0).institution=institution;
            route.waiting++;CommandRules.CampaignOnSourceProduced(State,0,route.id);
            route.waiting--;route.atRelay++;CommandRules.CampaignOnRelay(State,route.id);
            route.atRelay--;route.delivered++;State.command.delivered++;State.intelligence++;CommandRules.CampaignOnDelivery(State,0,route.id);
            if(State.command.campaign.evidence.Count!=1)throw new Exception("A delivered institution packet did not become an archived report");
            Draw();yield return null;yield return Click("CommandOverview");yield return Click("CampaignPage_1");RequireCampaignLayout();
            if(!root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(State.agents[0].name)&&t.text.Contains("未核证")))throw new Exception("Evidence archive omitted its actual source or verification status");
            Capture(Path.ChangeExtension(path,"campaign-evidence.png"));
            yield return Click("CampaignPage_0");
            if(!ProducerButton("CampaignApproach_0").interactable)throw new Exception("Matching delivered report did not unlock verification");
            int inventory=State.intelligence;
            yield return Click("CampaignApproach_0");
            task=State.command.tasks.First(t=>t.id==State.command.campaign.activeTask);
            if(task.stepsRequired!=1||State.intelligence!=inventory||State.command.campaign.evidence.Count!=1)
                throw new Exception("Evidence method must save a stage without consuming inventory or the permanent archive");
            yield return Click("MissionBack");yield return SmokeSaveLoad();
            if(State.command.campaign.evidence.Count!=1||State.command.campaign.chosenApproaches[0]!=0)
                throw new Exception("Evidence and verification choice did not survive saving");
            yield return Click("CommandOverview");yield return Click("CampaignClose");RequireMapDesk();

            State.ended=true;State.won=false;State.ending=CommandRules.CampaignEnding(State,false);Draw();yield return null;
            Capture(Path.ChangeExtension(path,"campaign-partial-ending.png"));
            yield return Click("CommandEndReview");yield return Click("EndingReviewBack");
            if(ProducerButton("CommandEndHome")==null)throw new Exception("Campaign ending review did not return to its ending");
            // Every authored method must expose its time, rewards and access
            // without a nested card scrollbar, including the longest copy.
            var catalog=JsonUtility.FromJson<CommandCampaignConfig>(Resources.Load<TextAsset>("Minglu/Config/command_campaign").text);
            foreach(var scenario in catalog.scenarios)for(int chapter=0;chapter<3;chapter++)
            {
                State=JsonUtility.FromJson<RunState>(opening);
                State.command.campaign.scenario=scenario.id;
                State.command.campaign.scenarioData=JsonUtility.FromJson<CommandCampaignScenario>(JsonUtility.ToJson(scenario));
                State.command.campaign.chapter=chapter;
                commandTab=3;modal="campaignDossier";campaignPage=0;Draw();yield return null;yield return new WaitForEndOfFrame();
                RequireCampaignLayout();
                if(root.GetComponentsInChildren<ScrollRect>().Any(r=>r.name.StartsWith("CampaignMethodDescription")))throw new Exception("A campaign route hides its tradeoffs inside a nested scrollbar");
                Capture(Path.ChangeExtension(path,"scenario-"+scenario.id+"-chapter-"+chapter+".png"));
            }
            foreach(string file in new[]{ProfilePath,ProfilePath+".bak",ProfilePath+".tmp",smokeSave,smokeSave+".bak",smokeSave+".tmp"})if(File.Exists(file))File.Delete(file);
            Debug.Log("MINGLU_CAMPAIGN_EXPERIENCE_UI_SUCCESS: map, accessible and evidence methods, disabled reason, archive/history, physical signoff, save/load, ending and review");
        }
    }
}
