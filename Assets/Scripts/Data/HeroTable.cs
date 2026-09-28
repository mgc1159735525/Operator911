using System.Collections.Generic;
using UnityEngine;

namespace HeroDispatch
{
    // v2 版 20 英雄 / 24 事件 / 6 区域 / 关系 完整数据表
    public static class HeroTable
    {
        public class HeroRow
        {
            public string id, name, codename, emoji, color, power, quirk, quirkDesc, bio;
            public float str, spd, intl, end, per;
            public int salary;
            public HeroArchetype arch;
            public (string n, string d)[] skills; // 被动/主动/终极 名称+描述
        }

        public static readonly HeroRow[] Heroes =
        {
            new HeroRow{ id="kael", name="凯尔", codename="焰心", emoji="🔥", color="#8B5CF6", arch=HeroArchetype.Mage,
                power="炎狱魔焰", str=5,spd=6,intl=10,end=5,per=8, salary=850,
                quirk="魔力反噬", quirkDesc="每次释放大招后会短暂失去视觉",
                skills=new[]{ ("焰心不灭","生命低于30%时，自动释放火焰护盾吸收伤害"),("七焰连珠","向目标连续发射七枚不同属性的焰弹，每枚造成递增伤害"),("炎狱天降","召唤巨大的火焰法阵覆盖全场，持续灼烧所有敌人5秒") },
                bio="新上海废墟中长大的孤儿，在地下遗迹中发现了上古炎魔的法典，将古典魔法与赛博神经接口融合，成为城中唯一能同时操控七焰的魔导师。" },
            new HeroRow{ id="lyra", name="莉拉", codename="霜弦", emoji="❄️", color="#8B5CF6", arch=HeroArchetype.Frost,
                power="极寒弦音", str=4,spd=7,intl=9,end=6,per=9, salary=800,
                quirk="旋律执念", quirkDesc="施法时必须哼唱，沉默状态会崩溃",
                skills=new[]{ ("霜之领域","周围3米内敌人移速降低20%"),("冰弦震荡","演奏冰晶小提琴释放锥形冰霜波，冻结命中目标2秒"),("永冬乐章","演奏终极乐章，全场降下冰晶风暴，所有敌人进入冻结状态3秒") },
                bio="曾是新东京最负盛名的全息音乐会首席演奏家，一场量子事故让她的声波与冰霜魔法永久绑定，用冰晶小提琴奏出致命的寒冬旋律。" },
            new HeroRow{ id="zephyr", name="泽菲尔", codename="雷纹", emoji="⚡", color="#8B5CF6", arch=HeroArchetype.Rune,
                power="天雷符文", str=5,spd=8,intl=9,end=5,per=8, salary=820,
                quirk="静电体质", quirkDesc="情绪激动时周围电子设备会短路",
                skills=new[]{ ("雷电亲和","受到的电系伤害降低50%，并转化为能量"),("雷纹爆裂","在地面绘制雷纹法阵，引爆后对范围内敌人造成雷电伤害并眩晕"),("万雷天牢","在空中展开巨型雷纹阵，召唤连续雷击覆盖全场，持续4秒") },
                bio="出生于悬浮城邦阿瓦隆的符文世家，将古老雷电符文刻入机械义肢，成为第一位「符文机械师」，被队友戏称为「行走的EMP」。" },
            new HeroRow{ id="nova", name="诺瓦", codename="星核", emoji="🔧", color="#21E6C1", arch=HeroArchetype.Tech,
                power="纳米重组", str=7,spd=7,intl=8,end=7,per=6, salary=880,
                quirk="机械共情", quirkDesc="能与机器对话，但会被噪音信息淹没",
                skills=new[]{ ("纳米修复","每秒恢复2%最大生命值"),("纳米刃臂","双臂变形为高频纳米刀刃，连续斩击前方敌人"),("纳米风暴","释放体内全部纳米虫群，形成覆盖全场的金属风暴") },
                bio="前量子动力实验室首席工程师，纳米实验事故后数十亿纳米机器人融入血液，可随时重组自身机械部分变成任何武器或工具。" },
            new HeroRow{ id="cipher", name="赛弗", codename="零号", emoji="💻", color="#21E6C1", arch=HeroArchetype.Hacker,
                power="数据入侵", str=4,spd=6,intl=10,end=5,per=10, salary=900,
                quirk="现实解离", quirkDesc="长时间联网会分不清现实与虚拟",
                skills=new[]{ ("数据幽灵","有20%概率闪避所有攻击（虚体化）"),("神经入侵","入侵目标神经系统，使其混乱失控攻击队友3秒"),("全域接管","接管全场所有机械单位，使其倒戈攻击己方阵营持续6秒") },
                bio="暗网传说中的「零号协议」，第一个成功入侵军方量子防火墙的黑客，将意识上传至神经云端，肉体只剩下空壳般的存在。" },
            new HeroRow{ id="atlas", name="阿特拉斯", codename="铁壁", emoji="🛡️", color="#21E6C1", arch=HeroArchetype.Tank,
                power="能量壁垒", str=8,spd=4,intl=7,end=10,per=6, salary=750,
                quirk="过载恐惧", quirkDesc="护盾破碎后会陷入短暂恐慌无法行动",
                skills=new[]{ ("钢铁意志","生命值越低，防御力越高（最多+40%）"),("能量壁垒","展开巨型能量护盾，为全体队友吸收大量伤害持续4秒"),("铁壁要塞","变形为固定炮台形态，获得无限护盾并持续嘲讽所有敌人") },
                bio="前城市防御军的活体盾牌，全身70%已替换为军用级合金装甲，曾独自抵挡整支机械军团的进攻，退役后被联盟召回成为最可靠的防线。" },
            new HeroRow{ id="aurora", name="奥萝拉", codename="曙光", emoji="✨", color="#FFE66D", arch=HeroArchetype.Holy,
                power="神圣光辉", str=6,spd=6,intl=8,end=7,per=8, salary=870,
                quirk="悲悯之锁", quirkDesc="无法对求救的敌人下手，会犹豫3秒",
                skills=new[]{ ("圣光庇佑","队友阵亡时有30%概率以1HP复活"),("曙光之矛","凝聚圣光为矛投向敌人，造成神圣伤害并治疗自身"),("天界审判","召唤天界之光柱笼罩全场，对邪恶造成毁灭伤害并治愈全体队友") },
                bio="天界遗族最后的血脉，自幼被圣光环绕，在赛博城市底层贫民中被发现时正用治愈之光拯救垂死的流浪者，被视为对抗黑暗势力的最后希望。" },
            new HeroRow{ id="thorin", name="索林", codename="天锤", emoji="🔨", color="#FFE66D", arch=HeroArchetype.Warrior,
                power="神罚之锤", str=10,spd=5,intl=6,end=8,per=6, salary=800,
                quirk="战神血脉", quirkDesc="战斗越久越狂暴，可能误伤队友",
                skills=new[]{ ("战意昂扬","每击杀一个敌人，攻击力提升5%，可叠加"),("天锤落击","跃起后以巨锤砸地，造成范围冲击波并击飞敌人"),("诸神黄昏","解放全部神力，化身雷神形态，每次攻击附带连锁闪电") },
                bio="据说是远古战神转世，觉醒于北欧废墟区，右臂铭刻神族文字，性格豪迈如雷霆，战斗时仿佛一尊移动的战神降世。" },
            new HeroRow{ id="raven", name="瑞文", codename="暗蚀", emoji="🗡️", color="#FF3CAC", arch=HeroArchetype.AntiHero,
                power="暗影吞噬", str=7,spd=9,intl=7,end=5,per=7, salary=850,
                quirk="暗影饥渴", quirkDesc="每隔一段时间必须吞噬灵魂否则衰弱",
                skills=new[]{ ("噬魂回复","击杀敌人时恢复15%生命值并获得临时护盾"),("暗蚀之爪","双手化为暗影利爪撕裂敌人，每次命中偷取生命值"),("深渊之门","打开脚下的小型深渊裂隙，释放暗影触手抓取所有敌人") },
                bio="曾是普通佣兵，被背叛致死后灵魂被深渊领主收为契约仆从，以半恶魔形态复活，只猎杀恶人来喂养体内的恶魔。" },
            new HeroRow{ id="lilith", name="莉莉丝", codename="血蔷", emoji="🌹", color="#FF3CAC", arch=HeroArchetype.Demon,
                power="血焰咒术", str=5,spd=7,intl=9,end=6,per=8, salary=830,
                quirk="血脉共鸣", quirkDesc="受伤越重力量越强，但理性会逐步丧失",
                skills=new[]{ ("血之狂怒","每损失10%生命值，伤害提升8%"),("血焰绽放","以自身血液点燃玫瑰形火阵，对范围内敌人造成持续灼烧"),("血蔷薇园","全场盛放地狱血蔷薇，每朵花爆炸造成大范围血焰伤害") },
                bio="纯血恶魔与人类的混血后裔，在地下黑市中被恶魔商人抚养长大，学会用自身血液点燃地狱之火，以暴制暴以血还血。" },
            new HeroRow{ id="morrigan", name="莫丽甘", codename="命织", emoji="🕸️", color="#9D4EDD", arch=HeroArchetype.Fate,
                power="命运丝线", str=4,spd=6,intl=10,end=5,per=10, salary=920,
                quirk="命运窥视", quirkDesc="能预见未来但会被无数可能性逼疯",
                skills=new[]{ ("命运预判","有15%概率完全闪避攻击（预见了攻击轨迹）"),("丝线操控","射出命运丝线连接敌人，操控其移动方向并暴露弱点"),("命运终焉","剪断全场所有敌人的命运丝线，使其陷入混乱并受到致命伤害") },
                bio="出生时便被古老死亡之神标记的预言者，双眼能看见所有生物的命运丝线，每次大规模预知都会让她离疯狂更近一步。" },
            new HeroRow{ id="hollow", name="霍洛", codename="虚壳", emoji="💀", color="#9D4EDD", arch=HeroArchetype.Undead,
                power="亡者号令", str=7,spd=5,intl=8,end=8,per=7, salary=780,
                quirk="亡者低语", quirkDesc="能听到周围亡灵的抱怨，战斗时尤其嘈杂",
                skills=new[]{ ("不死之躯","首次阵亡后以50%HP复活，冷却120秒"),("亡者召令","从地面召唤骷髅战士协助战斗，持续10秒"),("亡灵天灾","唤醒战场下所有亡者，形成亡灵军团冲锋席卷全场") },
                bio="本该在灾难中死去，但死亡之神的诅咒让他成为「活着的死人」，身体缓慢腐烂却不会真正死亡，同时获得了指挥亡灵大军的能力。" },
            new HeroRow{ id="fang", name="方恩", codename="兽牙", emoji="🐺", color="#7BF1A8", arch=HeroArchetype.Beast,
                power="野性觉醒", str=9,spd=8,intl=5,end=7,per=6, salary=780,
                quirk="兽化冲动", quirkDesc="月圆之夜会不受控制地完全兽化",
                skills=new[]{ ("猎食者本能","对生命值低于30%的目标造成双倍伤害"),("兽化突袭","进入半兽化形态，获得极速移动和连续爪击能力"),("完全兽化","变身十二基因融合巨兽，获得所有掠食者能力的终极形态") },
                bio="基因改造计划的唯一幸存者，体内融合了十二种顶级掠食者DNA，可在人形和兽化形态之间切换，发誓摧毁所有非法基因实验设施。" },
            new HeroRow{ id="spore", name="斯波尔", codename="孢子", emoji="🍄", color="#7BF1A8", arch=HeroArchetype.Fungal,
                power="菌域控制", str=4,spd=5,intl=9,end=7,per=10, salary=800,
                quirk="共生依赖", quirkDesc="必须定期与菌类共生否则免疫系统崩溃",
                skills=new[]{ ("菌丝感知","能感知隐形单位，并降低受到的毒素伤害50%"),("毒孢爆散","释放一片毒孢子云，持续对区域内敌人造成中毒伤害"),("菌域降临","全场生长巨型毒菌，释放毁灭性的孢子风暴覆盖整个战场") },
                bio="原生态研究所实习生，真菌泄漏事故后身体成为超级菌群宿主，能控制一切菌类生长，通过菌丝网络感知方圆数公里内一切生物活动。" },
            new HeroRow{ id="psyche", name="赛琪", codename="心魇", emoji="🧠", color="#7BF1A8", arch=HeroArchetype.Psychic,
                power="心灵风暴", str=3,spd=6,intl=10,end=5,per=10, salary=900,
                quirk="共情过载", quirkDesc="会不受控地吸收周围人的情绪导致头痛",
                skills=new[]{ ("心灵屏障","免疫所有精神控制类技能"),("念力风暴","释放强大的心灵冲击波，击退并眩晕范围内所有敌人"),("心灵深渊","将所有敌人拉入精神世界，在其最深层恐惧中困住5秒") },
                bio="六岁觉醒心灵感应能力的天才少女，能读取并操控他人思想，加入联盟是为了找到控制能力的方案，同时用念力守护被世界排斥的人。" },
            new HeroRow{ id="orion", name="奥利安", codename="星陨", emoji="🌟", color="#4DA8FF", arch=HeroArchetype.Alien,
                power="星辰之力", str=7,spd=7,intl=8,end=6,per=7, salary=850,
                quirk="星际乡愁", quirkDesc="夜晚会凝望星空陷入深深的思乡情绪",
                skills=new[]{ ("星能再生","在夜间战斗时，所有属性提升10%"),("星陨冲击","从空中召唤小型陨石砸向目标区域造成爆炸伤害"),("星河倾泻","打开星际传送门，让星河能量倾泻而出覆盖全场造成毁灭伤害") },
                bio="来自仙女座星系的高维生命体，因飞船坠毁流落地球，身体由压缩的星辰能量构成，正在寻找回到母星的方法同时保护这个新世界。" },
            new HeroRow{ id="vega", name="薇珈", codename="织光", emoji="💎", color="#4DA8FF", arch=HeroArchetype.Light,
                power="光子编织", str=5,spd=8,intl=9,end=6,per=7, salary=830,
                quirk="光敏失调", quirkDesc="在完全黑暗的环境中能力会大幅衰减",
                skills=new[]{ ("光速反应","首次受到的攻击必定闪避"),("光子剑舞","凝聚光子为多把光剑进行高速连击"),("光之织网","在全场编织光子网络，所有被光线触及的敌人被切割并致盲") },
                bio="来自织女星系的光子生命体，能以光速移动并操控一切光能，在地球上化身时尚设计师，当威胁降临时将光子化为利刃与护盾。" },
            new HeroRow{ id="blaze", name="布雷兹", codename="烈拳", emoji="👊", color="#FF8C42", arch=HeroArchetype.Fighter,
                power="气焰拳法", str=10,spd=7,intl=5,end=8,per=5, salary=700,
                quirk="武痴成性", quirkDesc="遇到强者会不顾一切要求切磋",
                skills=new[]{ ("拳意贯通","连续攻击同一目标时伤害递增，最高+30%"),("烈拳连打","以超高速打出十二连拳，最后一拳产生冲击波"),("灭世一拳","蓄力后打出一拳，产生毁灭性的直线冲击波贯穿全场") },
                bio="新上海地下格斗场传奇冠军，将传统气功与赛博增强肌肉完美融合，拳头能打出音爆冲击波，参加联盟只为遇到最强对手。" },
            new HeroRow{ id="shadow", name="影", codename="疾风", emoji="🌙", color="#FF8C42", arch=HeroArchetype.Ninja,
                power="影步杀法", str=7,spd=10,intl=6,end=5,per=7, salary=850,
                quirk="影之依存", quirkDesc="在无光环境中会变得沉默寡言且攻击性增强",
                skills=new[]{ ("影遁术","站立不动3秒后自动进入隐身状态"),("影步连斩","在敌人影子间瞬移并进行连续背刺攻击"),("影界降临","将全场拖入影之维度，在无尽的影子中无限连击所有敌人") },
                bio="已灭亡的影忍一族最后传人，将影术与现代体术融合，能在影子中自由穿梭，是联盟中速度最快的存在，没人见过她的真面目。" },
            new HeroRow{ id="titan", name="泰坦", codename="金刚", emoji="💪", color="#FF8C42", arch=HeroArchetype.Brute,
                power="极限肉体", str=9,spd=5,intl=5,end=10,per=6, salary=650,
                quirk="痛觉屏蔽", quirkDesc="感觉不到疼痛，经常战斗后才发现伤势严重",
                skills=new[]{ ("不灭体质","受到致命伤害时有50%概率以1HP存活"),("泰坦重击","以超级力量进行地面砸击，造成范围震荡并降低敌人防御"),("绝对领域","释放全部肉体能量，进入不可阻挡状态，免疫控制并持续对周围造成伤害") },
                bio="经过极端肉体强化训练的超级战士，身体密度是常人十倍，普通子弹对他如同橡皮弹，纯粹用肉体极限力量战斗，是联盟精神支柱。" },
        };

        // 英雄×英雄 关系（源自 character_relations.json，映射为 Partner/Rival/Conflict）
        public static readonly (string a, string b, RelationType rel, string desc)[] Relations =
        {
            ("kael","lyra",RelationType.Partner,"魔法系的双子星，冰火联合技默契配合"),
            ("kael","zephyr",RelationType.Partner,"同为符文学派出身，火系与雷系师兄弟"),
            ("lyra","zephyr",RelationType.Partner,"音乐与符文的美学共鸣"),
            ("nova","cipher",RelationType.Partner,"科技系双核，硬件与软件"),
            ("nova","atlas",RelationType.Partner,"机械军团围攻中并肩作战的战友"),
            ("cipher","atlas",RelationType.Partner,"智力与耐力的完美防御体系"),
            ("aurora","thorin",RelationType.Partner,"神灵系双壁，信仰同盟"),
            ("raven","lilith",RelationType.Partner,"恶魔系反英雄，黑暗中互相理解"),
            ("fang","shadow",RelationType.Partner,"速度型搭档，爆发配合隐匿"),
            ("spore","psyche",RelationType.Partner,"两个被世界排斥的存在，互为知己"),
            ("orion","vega",RelationType.Partner,"来自星际的同乡，互相依靠"),
            ("lyra","aurora",RelationType.Partner,"冰霜旋律与神圣光辉，如姐妹亲密"),
            ("psyche","morrigan",RelationType.Partner,"心灵感应与命运预知互相增强"),
            ("morrigan","hollow",RelationType.Partner,"命运丝线与亡灵之力互相呼应"),
            ("fang","blaze",RelationType.Rival,"野性之力与肉体极限的竞争"),
            ("blaze","titan",RelationType.Conflict,"矛与盾的永恒宿敌较量"),
            ("lilith","kael",RelationType.Conflict,"血焰与圣焰的对立吸引"),
        };

        // 能力特长映射（heroMatchesNeed 的 powerMap，对应 v2 特殊 need）
        public static readonly (string heroId, string[] powers)[] PowerMap =
        {
            ("kael", new[]{"fire_resist"}),
            ("lyra", new[]{"ice","fire_resist"}),
            ("lilith", new[]{"fire_resist"}),
            ("aurora", new[]{"healing"}),
            ("spore", new[]{"healing"}),
            ("vega", new[]{"flight"}),
            ("orion", new[]{"flight"}),
            ("zephyr", new[]{"electronic"}),
            ("nova", new[]{"electronic"}),
            ("cipher", new[]{"electronic","intelligence"}),
            ("psyche", new[]{"perception","intelligence"}),
            ("morrigan", new[]{"perception","intelligence"}),
        };

        public static readonly (string a, string b)[] ConflictsEvents =
        {
            ("lilith","kael"), ("blaze","titan"), ("fang","blaze")
        };
    }
}
