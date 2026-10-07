using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minglu
{
    [Serializable] public class CommandCampaignApproach
    {
        public string title,body;
        public int skill,required=1,steps=1,evidenceInstitution,reward=5,money,pressureRelief,traceRelief,heartsReward;
    }
    [Serializable] public class CommandCampaignChapter
    {
        public string title,briefing,successText;
        public CommandCampaignApproach[] approaches;
    }
    [Serializable] public class CommandCampaignScenario
    {
        public int id;
        public string title,premise,endingSuccess,endingPartial;
        public CommandCampaignChapter[] chapters;
    }
    [Serializable] public class CommandCampaignConfig
    {
        public int revision=1;
        public CommandCampaignScenario[] scenarios;
    }
    [Serializable] public class CommandCampaignEvidence
    {
        public int id,institution,source,route,receivedNight,verifiedChapter=-1;
        public string title,sourceName,summary;
        public bool verified;
    }
    [Serializable] public class CommandCampaignPacket
    {
        public int id,route,stage,institution,source=-1,producedNight;
        public string sourceName,title;
    }
    [Serializable] public class CommandCampaignState
    {
        public int revision=1,scenario,chapter,activeTask=-1,nextEvidence,nextPacket;
        public int[] chosenApproaches=new[]{-1,-1,-1};
        public CommandCampaignScenario scenarioData;
        public List<CommandCampaignEvidence> evidence=new List<CommandCampaignEvidence>();
        public List<CommandCampaignPacket> packets=new List<CommandCampaignPacket>();
        public List<string> history=new List<string>();
    }

    public static partial class CommandRules
    {
        public static bool HasCommandCampaign(RunState s)
        {return s!=null&&s.command!=null&&s.command.campaignEnabled&&s.command.campaign!=null;}

        // Only the new-run entry point calls this. Old saves retain their original win contract.
        public static void InitializeCommandCampaign(RunState s)
        {
            if(s.config.Value("turns")<=8||HasCommandCampaign(s))return;
            var asset=Resources.Load<TextAsset>("Minglu/Config/command_campaign");
            if(asset==null)throw new InvalidOperationException("缺少首脑战役配置");
            var content=JsonUtility.FromJson<CommandCampaignConfig>(asset.text);
            ValidateCommandCampaignConfig(content);
            int scenario=(int)(s.initialSeed%(uint)content.scenarios.Length);
            s.command.campaign=new CommandCampaignState{scenario=scenario,scenarioData=JsonUtility.FromJson<CommandCampaignScenario>(JsonUtility.ToJson(content.scenarios[scenario]))};
            s.command.campaignEnabled=true;
            Log(s,"主线委任「"+s.command.campaign.scenarioData.title+"」："+s.command.campaign.scenarioData.premise+" 完成三章委任与组织阶段目标，才能取得完整胜利。");
        }

        public static CommandCampaignChapter CampaignCurrentChapter(RunState s)
        {
            if(!HasCommandCampaign(s))return null;
            var c=s.command.campaign;
            return c.chapter<c.scenarioData.chapters.Length?c.scenarioData.chapters[c.chapter]:null;
        }

        public static bool CampaignReadyForVictory(RunState s)
        {return !HasCommandCampaign(s)||s.command.campaign.chapter>=s.command.campaign.scenarioData.chapters.Length;}

        public static string CampaignProgressText(RunState s)
        {return !HasCommandCampaign(s)?"原有行动目标":s.command.campaign.scenarioData.title+" · 已完成 "+s.command.campaign.chapter+" / "+s.command.campaign.scenarioData.chapters.Length+" 章";}

        public static string CampaignObjective(RunState s)
        {
            if(!HasCommandCampaign(s))return GateText(s);
            var c=s.command.campaign;var chapter=CampaignCurrentChapter(s);
            if(chapter==null)return "三章委任已完成；继续达成组织目标："+GateText(s);
            var active=s.command.tasks.FirstOrDefault(t=>t.id==c.activeTask&&t.status==0);
            return "第"+(c.chapter+1)+"章 · "+chapter.title+(active==null?"：在主线委任选择一种方法":"："+active.title+"（"+active.stepsDone+"/"+active.stepsRequired+"阶段）");
        }

        public static string CampaignApproachName(RunState s,int approach)
        {
            var chapter=CampaignCurrentChapter(s);
            return chapter==null||approach<0||approach>=chapter.approaches.Length?"委任已结束":chapter.approaches[approach].title;
        }

        public static string CampaignApproachDescription(RunState s,int approach)
        {
            var chapter=CampaignCurrentChapter(s);
            if(chapter==null||approach<0||approach>=chapter.approaches.Length)return "";
            var a=chapter.approaches[approach];
            string access=a.evidenceInstitution>0?"需已送达的"+Institutions[a.evidenceInstitution]+"档案；作为核证依据，不消耗库存情报。":"不要求机构任职或档案；通过成员协调完成。";
            string reward="成果+"+a.reward+(a.money>0?" / 银两+"+a.money:"")+(a.pressureRelief>0?" / 压力−"+a.pressureRelief:"")+(a.traceRelief>0?" / 追查−"+a.traceRelief:"")+(a.heartsReward>0?" / 民心+"+a.heartsReward:"");
            return a.body+"\n"+Skills[a.skill]+" · 每阶段需"+a.required+"成功 · 最少"+a.steps+"夜\n"+access+"\n完成后："+reward;
        }

        public static string CampaignApproachError(RunState s,int approach)
        {
            if(!HasCommandCampaign(s))return "本局沿用原有行动目标，没有主线委任";
            if(s.ended)return "本局已结束";
            if(s.command.eventKind!=0)return "先处理待决事件";
            var c=s.command.campaign;var chapter=CampaignCurrentChapter(s);
            if(chapter==null)return "三章委任已完成，继续达成组织阶段目标";
            if(approach<0||approach>=chapter.approaches.Length)return "未知行动方法";
            var active=s.command.tasks.FirstOrDefault(t=>t.id==c.activeTask&&t.status==0);
            if(active!=null)return "已选「"+active.title+"」，前往任务安排负责人";
            var a=chapter.approaches[approach];
            if(s.config.Value("turns")-s.turn+1<a.steps)return "行动窗口不足：此方法至少需要"+a.steps+"夜";
            if(a.evidenceInstitution>0&&!c.evidence.Any(e=>e.institution==a.evidenceInstitution))return "尚未收到"+Institutions[a.evidenceInstitution]+"档案；可选另一种方法，或等待该机构情报链送达";
            if(!s.agents.Any(member=>member.condition==Condition.Active))return "没有仍在组织的可行动成员";
            return "";
        }

        public static string StartCampaignApproach(RunState s,int approach)
        {
            string error=CampaignApproachError(s,approach);if(error!="")return error;
            var c=s.command.campaign;var chapter=CampaignCurrentChapter(s);var a=chapter.approaches[approach];
            // Main objectives never compete with the optional nine-task cap.
            var task=new CommandTask{id=s.command.nextTask++,title=a.title,body=a.body+"\n主线「"+c.scenarioData.title+"」第"+(c.chapter+1)+"章。"+(a.steps>1?"每完成一个阶段后，可重新选择负责人；失败保留既有阶段。":"完成后推进本章。"),category="主线委任",skill=a.skill,required=a.required,level=1,reward=a.reward,money=a.money,pressureRelief=a.pressureRelief,traceRelief=a.traceRelief,heartsReward=a.heartsReward,failExposure=4,origin="上级主线委任",source=-1,district=s.command.strategy.baseDistrict,stepsRequired=a.steps,deadline=s.config.Value("turns"),campaignKey=c.scenario+":"+c.chapter+":"+approach};
            s.command.tasks.Add(task);
            c.activeTask=task.id;c.chosenApproaches[c.chapter]=approach;
            Log(s,"为「"+chapter.title+"」选择「"+a.title+"」。至少"+a.steps+"夜，档案不消耗；请在任务中签发负责人。");
            return "";
        }

        public static string CancelCampaignApproachError(RunState s)
        {
            if(!HasCommandCampaign(s)||s.ended)return "当前没有可更换的方法";
            if(s.command.eventKind!=0)return "先处理待决事件";
            var c=s.command.campaign;var task=s.command.tasks.FirstOrDefault(t=>t.id==c.activeTask&&t.status==0);
            if(task==null)return "当前没有已选的方法";
            if(task.actor>=0)return "先在任务中撤回负责人授权，再更换方法";
            if(task.stepsDone>0||task.reconComplete||!string.IsNullOrEmpty(task.lastResult))return "本章已开始执行；保留现有进度，继续当前方法";
            return "";
        }
        public static string CancelCampaignApproach(RunState s)
        {
            string error=CancelCampaignApproachError(s);if(error!="")return error;
            var c=s.command.campaign;var task=s.command.tasks.First(t=>t.id==c.activeTask);
            task.status=2;task.lastResult="尚未执行，负责人改选其他方法；未消耗资源，也不产生到期惩罚。";
            c.activeTask=-1;c.chosenApproaches[c.chapter]=-1;
            Log(s,"撤回主线方法「"+task.title+"」：尚未执行，可重新选择。");return "";
        }

        static void MatchCampaignPacketCount(RunState s,int route,int stage,int count)
        {
            var c=s.command.campaign;
            var packets=c.packets.Where(p=>p.route==route&&p.stage==stage).ToList();
            while(packets.Count>count){c.packets.Remove(packets[0]);packets.RemoveAt(0);}
            // Synthetic/legacy inventory has unknown provenance. It may move,
            // but it can never grant access to an institutional story method.
            while(packets.Count<count)
            {
                var unknown=new CommandCampaignPacket{id=c.nextPacket++,route=route,stage=stage,producedNight=s.turn,source=-1,institution=0,sourceName="来源待核实",title="来源未记录的积压报告"};
                c.packets.Insert(0,unknown);packets.Insert(0,unknown);
            }
        }
        public static void ReconcileCampaignPackets(RunState s)
        {
            if(!HasCommandCampaign(s))return;
            var c=s.command.campaign;
            c.packets.RemoveAll(p=>!s.command.routes.Any(r=>r.id==p.route));
            foreach(var route in s.command.routes)
            {
                MatchCampaignPacketCount(s,route.id,0,route.waiting);
                MatchCampaignPacketCount(s,route.id,1,route.atRelay);
            }
        }
        // Counters have already advanced when the caller invokes each hook.
        public static void CampaignOnSourceProduced(RunState s,int sourceId,int routeId)
        {
            if(!HasCommandCampaign(s))return;
            var route=s.command.routes.FirstOrDefault(r=>r.id==routeId);
            var person=s.command.people.FirstOrDefault(p=>p.id==sourceId);
            var member=s.agents.FirstOrDefault(a=>a.id==sourceId);
            if(route==null||route.waiting<1||person==null||member==null||!HasInstitution(person)||route.source!=sourceId)return;
            MatchCampaignPacketCount(s,routeId,0,route.waiting-1);
            MatchCampaignPacketCount(s,routeId,1,route.atRelay);
            var c=s.command.campaign;
            c.packets.Add(new CommandCampaignPacket{id=c.nextPacket++,route=routeId,stage=0,institution=person.institution,source=sourceId,producedNight=s.turn,sourceName=member.name,title=InstitutionIntel[person.institution]});
        }
        public static void CampaignOnRelay(RunState s,int routeId)
        {
            if(!HasCommandCampaign(s))return;
            var route=s.command.routes.FirstOrDefault(r=>r.id==routeId);
            if(route==null||route.atRelay<1)return;
            MatchCampaignPacketCount(s,routeId,0,route.waiting+1);
            MatchCampaignPacketCount(s,routeId,1,route.atRelay-1);
            var c=s.command.campaign;var packet=c.packets.First(p=>p.route==routeId&&p.stage==0);
            c.packets.Remove(packet);packet.stage=1;c.packets.Add(packet);
        }
        // Called only after route delivery. Use the produced packet's source,
        // never the source member currently occupying the route after a handoff.
        public static void CampaignOnDelivery(RunState s,int sourceId,int routeId)
        {
            if(!HasCommandCampaign(s))return;
            var route=s.command.routes.FirstOrDefault(x=>x.id==routeId);
            if(route==null)return;
            MatchCampaignPacketCount(s,routeId,0,route.waiting);
            MatchCampaignPacketCount(s,routeId,1,route.atRelay+1);
            var c=s.command.campaign;var packet=c.packets.First(p=>p.route==routeId&&p.stage==1);c.packets.Remove(packet);
            if(packet.institution==0||packet.source<0){Log(s,"收到来源未记录的积压报告：保留正常送达收益，不作为机构核证依据。");return;}
            c.evidence.Add(new CommandCampaignEvidence{id=c.nextEvidence++,institution=packet.institution,source=packet.source,route=routeId,receivedNight=s.turn,sourceName=packet.sourceName,title=packet.title,summary="第"+packet.producedNight+"夜的岗位报告，经联络链"+(routeId+1)+"送达；原始来源保留，不随岗位交接改变。"});
            Log(s,"机构档案归卷："+packet.title+"。收到报告不等于已核实；可在主线委任查看用途。");
        }

        public static void CampaignOnTaskCompleted(RunState s,CommandTask task)
        {
            if(!HasCommandCampaign(s)||task==null||string.IsNullOrEmpty(task.campaignKey)||task.status!=1)return;
            var c=s.command.campaign;if(c.activeTask!=task.id||CampaignCurrentChapter(s)==null)return;
            int approach=c.chosenApproaches[c.chapter];
            if(approach<0||task.campaignKey!=c.scenario+":"+c.chapter+":"+approach)return;
            var chapter=CampaignCurrentChapter(s);var a=chapter.approaches[approach];
            if(a.evidenceInstitution>0)
            {
                var evidence=c.evidence.FirstOrDefault(e=>e.institution==a.evidenceInstitution);
                if(evidence==null)return;
                evidence.verified=true;evidence.verifiedChapter=c.chapter;
            }
            string consequence="第"+s.turn+"夜 · "+chapter.title+"：采用「"+a.title+"」。"+chapter.successText;
            c.history.Add(consequence);c.chapter++;c.activeTask=-1;
            Log(s,"主线推进："+consequence);
            Notice(s,new CommandNotice{title="主线推进 · "+chapter.title,body=chapter.successText+"\n选择的方法：「"+a.title+"」。\n"+(CampaignReadyForVictory(s)?"三章委任已完成；完整胜利还需达成组织目标："+GateText(s):"下一章："+CampaignCurrentChapter(s).title+"。在主线委任选择新的处理方法。"),tab=6});
        }

        public static string CampaignEvidenceSummary(RunState s)
        {
            if(!HasCommandCampaign(s))return "本局沿用原有行动目标。";
            var c=s.command.campaign;
            if(c.evidence.Count==0)return "尚无送达档案。只有敌方机构任职内线，经联络链送达的报告会归卷。主线仍可通过不依赖档案的方法推进。";
            return string.Join("\n\n",c.evidence.Select(e=>"第"+e.receivedNight+"夜 · "+Institutions[e.institution]+" · "+e.sourceName+"\n"+e.title+" / "+(e.verified?"已作为第"+(e.verifiedChapter+1)+"章核证依据":"已收到 · 未核证")+"\n"+e.summary));
        }
        public static string CampaignEvidenceRecords(RunState s){return CampaignEvidenceSummary(s);}

        public static string CampaignEnding(RunState s,bool won)
        {
            if(!HasCommandCampaign(s))return won?"灯火成网：组织完成本局考验。":s.ending;
            var c=s.command.campaign;int survivors=s.agents.Count(a=>a.condition==Condition.Active),lost=s.agents.Count-survivors;
            string outcome=won?c.scenarioData.endingSuccess:c.chapter==c.scenarioData.chapters.Length?"主线委任已完成，但组织维持能力未达到上级要求。":c.scenarioData.endingPartial;
            string routes=string.Join("、",c.chosenApproaches.Select((a,i)=>a>=0&&i<c.chapter?c.scenarioData.chapters[i].approaches[a].title:null).Where(x=>x!=null));
            return (won?"委任完成 · ":"行动复盘 · ")+c.scenarioData.title+"\n"+outcome+"\n已完成"+c.chapter+"/"+c.scenarioData.chapters.Length+"章；送达"+s.command.delivered+"份报告，成果"+s.progress+"。"+survivors+"名成员仍在组织，"+lost+"名被捕或离队。"+(routes.Length>0?"\n本局选择："+routes+"。":"")+(!won?"\n未完成的要求："+(c.chapter<c.scenarioData.chapters.Length?"主线第"+(c.chapter+1)+"章；":"")+GateText(s):"");
        }

        public static void ValidateCommandCampaignConfig(CommandCampaignConfig c)
        {
            if(c==null||c.revision!=1||c.scenarios==null||c.scenarios.Length!=3)throw new Exception("主线配置须含三个剧本");
            var titles=new HashSet<string>();
            for(int i=0;i<c.scenarios.Length;i++)
            {
                var scenario=c.scenarios[i];
                if(scenario==null||scenario.id!=i||string.IsNullOrWhiteSpace(scenario.title)||string.IsNullOrWhiteSpace(scenario.premise)||string.IsNullOrWhiteSpace(scenario.endingSuccess)||string.IsNullOrWhiteSpace(scenario.endingPartial)||scenario.chapters==null||scenario.chapters.Length!=3)throw new Exception("主线剧本结构无效");
                foreach(var chapter in scenario.chapters)
                {
                    if(chapter==null||string.IsNullOrWhiteSpace(chapter.title)||string.IsNullOrWhiteSpace(chapter.briefing)||string.IsNullOrWhiteSpace(chapter.successText)||chapter.approaches==null||chapter.approaches.Length!=2)throw new Exception("每章需有叙事与两种处理方法");
                    for(int k=0;k<2;k++)
                    {
                        var a=chapter.approaches[k];
                        if(a==null||string.IsNullOrWhiteSpace(a.title)||!titles.Add(a.title)||string.IsNullOrWhiteSpace(a.body)||a.skill<0||a.skill>3||a.required<1||a.required>3||a.steps!=(k==0?1:2)||a.evidenceInstitution<0||a.evidenceInstitution>=Institutions.Length||(k==0&&a.evidenceInstitution==0)||(k==1&&a.evidenceInstitution!=0)||a.reward<0||a.reward>15||a.money<0||a.money>20||a.pressureRelief<0||a.pressureRelief>20||a.traceRelief<0||a.traceRelief>20||a.heartsReward<0||a.heartsReward>20)throw new Exception("主线方法数值无效，须保留两夜无档案替代路径");
                    }
                }
            }
        }

        public static void ValidateCommandCampaign(RunState s)
        {
            if(s!=null&&s.command!=null&&s.command.campaignEnabled&&s.command.campaign==null)throw new Exception("已启用的主线存档缺少内容");
            if(!HasCommandCampaign(s))return;
            var c=s.command.campaign;
            if(c.revision!=1||c.scenario<0||c.scenario>2||c.scenarioData==null||c.scenarioData.id!=c.scenario||c.scenarioData.chapters==null||c.scenarioData.chapters.Length!=3||c.chapter<0||c.chapter>3||c.chosenApproaches==null||c.chosenApproaches.Length!=3||c.chosenApproaches.Any(x=>x< -1||x>1)||c.evidence==null||c.evidence.Any(e=>e==null)||c.packets==null||c.packets.Any(p=>p==null)||c.history==null||c.history.Count!=c.chapter||c.nextEvidence<0||c.nextPacket<0||c.evidence.Select(e=>e.id).Distinct().Count()!=c.evidence.Count||c.packets.Select(p=>p.id).Distinct().Count()!=c.packets.Count)throw new Exception("主线存档结构无效");
            for(int i=0;i<c.scenarioData.chapters.Length;i++)
            {
                var chapter=c.scenarioData.chapters[i];
                if(chapter==null||string.IsNullOrWhiteSpace(chapter.title)||string.IsNullOrWhiteSpace(chapter.briefing)||string.IsNullOrWhiteSpace(chapter.successText)||chapter.approaches==null||chapter.approaches.Length!=2||i<c.chapter&&c.chosenApproaches[i]<0)throw new Exception("主线章节记录无效");
                for(int k=0;k<2;k++)
                {
                    var a=chapter.approaches[k];
                    if(a==null||string.IsNullOrWhiteSpace(a.title)||string.IsNullOrWhiteSpace(a.body)||a.skill<0||a.skill>3||a.required<1||a.required>3||a.steps!=(k==0?1:2)||a.evidenceInstitution<0||a.evidenceInstitution>=Institutions.Length||(k==0&&a.evidenceInstitution==0)||(k==1&&a.evidenceInstitution!=0)||a.reward<0||a.reward>15||a.money<0||a.money>20||a.pressureRelief<0||a.pressureRelief>20||a.traceRelief<0||a.traceRelief>20||a.heartsReward<0||a.heartsReward>20)throw new Exception("主线方法存档无效");
                }
            }
            foreach(var e in c.evidence)
                if(e==null||e.id<0||e.id>=c.nextEvidence||e.institution<1||e.institution>=Institutions.Length||e.receivedNight<1||e.receivedNight>s.turn||e.verifiedChapter< -1||e.verifiedChapter>=c.chapter||e.verified!=(e.verifiedChapter>=0)||!s.command.people.Any(p=>p.id==e.source)||!s.command.routes.Any(r=>r.id==e.route))throw new Exception("主线机构档案无效");
            foreach(var packet in c.packets)
                if(packet.id<0||packet.id>=c.nextPacket||packet.stage<0||packet.stage>1||packet.institution<0||packet.institution>=Institutions.Length||packet.producedNight<1||packet.producedNight>s.turn||!s.command.routes.Any(r=>r.id==packet.route)||packet.institution==0&&packet.source!=-1||packet.institution>0&&!s.command.people.Any(p=>p.id==packet.source))throw new Exception("在途档案来源无效");
            if(c.activeTask>=0)
            {
                var task=s.command.tasks.FirstOrDefault(t=>t.id==c.activeTask);
                if(c.chapter>=3||task==null||c.chosenApproaches[c.chapter]<0||task.campaignKey!=c.scenario+":"+c.chapter+":"+c.chosenApproaches[c.chapter])throw new Exception("主线授权记录无效");
            }
        }
    }
}
