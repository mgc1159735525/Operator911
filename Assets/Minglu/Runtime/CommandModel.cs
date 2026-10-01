using System;
using System.Collections.Generic;
namespace Minglu
{
    [Serializable] public class CommandPerson
    {
        public int institution;
        public int id,level=1,xp,loyalty=75,neglect,leaveUntil,policy,age;
        public string profession,code,life,temper,last="正常生活";
        public Appearance originalLook;
        public int warningTurn=-1,capturedTurn=-1,resisted;
    }
    [Serializable] public class CommandTask
    {
        public int id,skill,required=1,level=1,district,deadline,actor=-1,reward=5,money=6,status,source=-1;
        public string title,body,origin,profession="",category="委托",followup="";
        public int institution;
        public int plan,intelReward,pressureRelief,failExposure=6;
        public int traceRelief,heartsReward;
        public bool intelSupport,followupSpawned;
        public bool reconnaissance,reconComplete;
        public string lastResult="";
        public int operation,target=-1,stepsRequired=1,stepsDone;
    }
    [Serializable] public class CommandRoute
    {
        public int id,source=-1,liaison=-1,courier=-1,waiting,atRelay,age,delivered;
        public bool enabled;
        public int compromisedSlots;
        public string last="未配置：选择三名不同成员并启用。";
    }
    [Serializable] public class CommandTemplate
    {
        public string title,body,profession="",category="委托",followup="";
        public int intelReward,pressureRelief,failExposure=6;
        public int sourceInstitution,traceRelief,heartsReward;
        public bool followupOnly;
        public int skill,required=1,level=1,reward=5,money=6;
    }
    [Serializable] public class CommandConfig
    {
        public int startingCrew=6,capacity=8,income=12,taskInterval=3,eventChance=28,coverXP=3;
        public List<CommandTemplate> templates=new List<CommandTemplate>();
    }
    [Serializable] public class CommandNotice
    {
        public string title,body;
        public int tab=4,task=-1,actor=-1;
    }
    [Serializable] public class CommandState
    {
        public int missionRevision,institutionRevision;
        public int experienceRevision,goalDeliveries,goalScore,goalLevel,secondGateDeliveries;
        public bool legacySourceSchedule;
        public CommandStrategy strategy;
        public int version=1,nextTask,delivered,completed,gate,eventKind,eventActor=-1,pauseUntil,variant;
        public string eventText="";
        public List<CommandNotice> notices=new List<CommandNotice>();
        public CommandConfig config;
        public List<CommandPerson> people=new List<CommandPerson>();
        public List<CommandTask> tasks=new List<CommandTask>();
        public List<CommandRoute> routes=new List<CommandRoute>();
    }
}
