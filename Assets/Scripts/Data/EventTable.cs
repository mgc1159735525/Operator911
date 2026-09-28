using System.Collections.Generic;
using UnityEngine;

namespace HeroDispatch
{
    // v2 版 24 个事件模板 + 6 区域数据
    public static class EventTable
    {
        public class EventRow
        {
            public string name, icon;
            public EventType type;
            public string[] needs;
            public string baseGrade;
            public string desc, fogDesc;
        }

        public static readonly EventRow[] Events =
        {
            new EventRow{ name="仓库火灾", icon="🔥", type=EventType.Disaster, needs=new[]{"fire_resist","endurance"}, baseGrade="C",
                desc="工业区仓库冒出浓烟，疑似电路老化引发火灾。", fogDesc="工业区发生不明火情，规模未知。" },
            new EventRow{ name="银行劫案", icon="💰", type=EventType.Crime, needs=new[]{"speed","strength"}, baseGrade="B",
                desc="武装匪徒闯入商业区银行，挟持人质。", fogDesc="商业区传来警报，疑似抢劫事件。" },
            new EventRow{ name="连环杀手出没", icon="🔪", type=EventType.Villain, needs=new[]{"perception","intelligence"}, baseGrade="A",
                desc="郊区发现第三名受害者，凶手仍在活动。", fogDesc="郊区多次出现可疑事件，警方束手无策。" },
            new EventRow{ name="高楼困人", icon="🏢", type=EventType.Rescue, needs=new[]{"flight","speed"}, baseGrade="C",
                desc="市中心大厦电梯故障，多人被困高层。", fogDesc="市中心高层建筑报告人员被困。" },
            new EventRow{ name="神秘信号", icon="📡", type=EventType.Mystery, needs=new[]{"intelligence","perception"}, baseGrade="B",
                desc="港口区域接收到不明电磁信号，来源不明。", fogDesc="港口检测到异常电磁波动。" },
            new EventRow{ name="地铁脱轨", icon="🚇", type=EventType.Disaster, needs=new[]{"strength","endurance"}, baseGrade="B",
                desc="住宅区地铁站发生列车脱轨事故。", fogDesc="地铁系统发生严重事故，有人员被困。" },
            new EventRow{ name="街头暴动", icon="💥", type=EventType.Crime, needs=new[]{"strength","intelligence"}, baseGrade="C",
                desc="商业区爆发大规模骚乱，商店遭打砸。", fogDesc="商业区出现群体性事件。" },
            new EventRow{ name="化学泄漏", icon="☣️", type=EventType.Disaster, needs=new[]{"fire_resist","intelligence"}, baseGrade="A",
                desc="工业区化学品储存设施泄漏，毒雾蔓延。", fogDesc="工业区传出刺鼻气味，居民报告不适。" },
            new EventRow{ name="超级反派现身", icon="🦹", type=EventType.Villain, needs=new[]{"strength","speed","intelligence"}, baseGrade="S",
                desc="不明超级反派在市中心展示毁灭性力量！", fogDesc="市中心出现极强能量波动！等级未知！" },
            new EventRow{ name="孩童失踪", icon="🔍", type=EventType.Rescue, needs=new[]{"perception","intelligence"}, baseGrade="C",
                desc="住宅区多名儿童失踪，家长焦急求助。", fogDesc="住宅区有人员失踪报告。" },
            new EventRow{ name="港口走私", icon="📦", type=EventType.Crime, needs=new[]{"perception","strength"}, baseGrade="B",
                desc="情报显示港口今晚有大规模走私交易。", fogDesc="港口有可疑活动迹象。" },
            new EventRow{ name="变电站爆炸", icon="⚡", type=EventType.Disaster, needs=new[]{"electronic","endurance"}, baseGrade="C",
                desc="住宅区变电站发生爆炸，大片停电。", fogDesc="住宅区发生爆炸，供电中断。" },
            new EventRow{ name="人质危机", icon="🚨", type=EventType.Crime, needs=new[]{"perception","speed"}, baseGrade="A",
                desc="歹徒占领市中心政府大楼，扣押数十名人质。", fogDesc="市中心政府大楼发生严重安全事件。" },
            new EventRow{ name="海难救援", icon="🌊", type=EventType.Rescue, needs=new[]{"flight","endurance"}, baseGrade="B",
                desc="港口外海域船只倾覆，多名船员落水。", fogDesc="港口附近海域发生紧急事件。" },
            new EventRow{ name="变异生物", icon="👾", type=EventType.Villain, needs=new[]{"strength","fire_resist"}, baseGrade="A",
                desc="工业区下水道发现巨型变异生物！", fogDesc="工业区地下传来异常震动。" },
            new EventRow{ name="网络攻击", icon="💻", type=EventType.Mystery, needs=new[]{"intelligence","electronic"}, baseGrade="B",
                desc="城市网络系统遭到大规模攻击，数据泄露。", fogDesc="城市网络出现严重异常。" },
            new EventRow{ name="地震余震", icon="🏚️", type=EventType.Disaster, needs=new[]{"endurance","strength"}, baseGrade="B",
                desc="城市遭受余震袭击，建筑受损。", fogDesc="检测到地震活动，影响范围不明。" },
            new EventRow{ name="邪教仪式", icon="🕯️", type=EventType.Mystery, needs=new[]{"perception","intelligence"}, baseGrade="A",
                desc="郊区废弃工厂发现可疑邪教活动。", fogDesc="郊区有异常能量波动。" },
            new EventRow{ name="交通事故", icon="🚗", type=EventType.Rescue, needs=new[]{"speed","endurance"}, baseGrade="D",
                desc="郊区高速公路多车连环碰撞。", fogDesc="郊区发生交通事故。" },
            new EventRow{ name="超级病毒", icon="🦠", type=EventType.Disaster, needs=new[]{"intelligence","healing"}, baseGrade="S",
                desc="未知病毒在住宅区快速传播，医院告急！", fogDesc="住宅区出现不明疾病爆发迹象！" },
            new EventRow{ name="机器人暴走", icon="🤖", type=EventType.Villain, needs=new[]{"electronic","strength"}, baseGrade="B",
                desc="工业区机器人工厂AI失控，机器人攻击工人。", fogDesc="工业区发生工业事故。" },
            new EventRow{ name="炸弹威胁", icon="💣", type=EventType.Crime, needs=new[]{"perception","intelligence"}, baseGrade="A",
                desc="市中心商业大厦被安放炸弹，倒计时中！", fogDesc="收到炸弹威胁，目标位置不确定。" },
            new EventRow{ name="难民潮", icon="⛺", type=EventType.Rescue, needs=new[]{"intelligence","endurance"}, baseGrade="C",
                desc="大量难民涌入郊区，急需安置。", fogDesc="郊区有大量人员聚集。" },
            new EventRow{ name="时间裂缝", icon="🌀", type=EventType.Mystery, needs=new[]{"intelligence","perception"}, baseGrade="S",
                desc="市中心上空出现时空裂缝！物理法则开始扭曲！", fogDesc="市中心检测到不可能的物理现象！" },
        };

        public class DistrictRow
        {
            public string id, name, emoji, color;
            public Vector2 pos, size;
        }

        public static readonly DistrictRow[] Districts =
        {
            new DistrictRow{ id="commercial",  name="商业区", emoji="🏢", color="#FFE66D", pos=new Vector2(300,100), size=new Vector2(120,80) },
            new DistrictRow{ id="residential", name="住宅区", emoji="🏠", color="#7BF1A8", pos=new Vector2(130,180), size=new Vector2(120,80) },
            new DistrictRow{ id="industrial",  name="工业区", emoji="🏭", color="#FF8C42", pos=new Vector2(470,180), size=new Vector2(120,80) },
            new DistrictRow{ id="harbor",      name="港口",   emoji="🚢", color="#4DA8FF", pos=new Vector2(470,330), size=new Vector2(120,80) },
            new DistrictRow{ id="downtown",    name="市中心", emoji="🏛️", color="#C77DFF", pos=new Vector2(300,250), size=new Vector2(130,90) },
            new DistrictRow{ id="suburb",      name="郊区",   emoji="🌲", color="#21E6C1", pos=new Vector2(130,340), size=new Vector2(120,80) },
        };

        // SVG 地 600x430 画布 → 归一化 + 拉伸铺满（画布偏小且集中左上，直接映射会右半屏空黑）
        public static Vector2 ToNormalized(Vector2 svgPos)
        {
            float x = svgPos.x / 600f;
            float y = svgPos.y / 430f;
            x = Mathf.Clamp(0.5f + (x - 0.5f) * 1.4f, 0.06f, 0.94f);
            y = Mathf.Clamp(0.5f + (y - 0.5f) * 1.2f, 0.10f, 0.90f);
            return new Vector2(x, y);
        }
    }
}
