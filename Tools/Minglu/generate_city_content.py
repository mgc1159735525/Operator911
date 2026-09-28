from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
sites=[]
data=[
('tea','听雨茶楼',0,.13,.49,'Contact','证人口供','','跑堂少年昨夜失踪。老掌柜愿意说话，却怕牵连整条街。'),
('warehouse','永丰货栈',0,.29,.72,'Recon','私运账本','证人口供','茶客留下的地址指向货栈后门，账房似乎记录了陌生买家的来往。'),
('bookshop','青灯书坊',1,.14,.10,'Recon','印版印记','','被禁文告出自同一块旧印版。书坊中有人知道是谁借走了它。'),
('academy','槐序学社',1,.31,.37,'Propaganda','学社名录','印版印记','学生在此争论出路。印版上的暗记能证明你与传抄者的联系。'),
('registry','北衙案牍房',2,.46,.10,'Recon','转押时刻','','低阶书吏轮流抄写转押簿，片刻疏忽就可能留下新的线索。'),
('prison','旧狱夹巷',2,.61,.39,'Contact','牢门暗号','转押时刻','夹巷只在转押时短暂开放。狱卒家属需要确信你知道确切时刻。'),
('camp','东营辕门',3,.79,.15,'Contact','换防口令','','疲惫的守卒不愿为长官的私利送命，但不会轻信陌生人的承诺。'),
('bell','临河钟楼',3,.87,.43,'Recon','警钟构造','换防口令','知道换防口令后可以接近钟楼。绳索和传令制度都有薄弱之处。'),
('clinic','济生药铺',4,.43,.55,'Contact','伤患证词','','无钱求医的伤者越来越多。郎中需要帮助，也知道搜捕留下的伤口。'),
('refuge','古寺后院',4,.60,.80,'Propaganda','避难名册','伤患证词','凭伤者口述找到后院，住持愿意为被波及的人暂留一盏灯。'),
('ferry','芦苇渡口',5,.77,.57,'Recon','船期残页','','夜船未按约定靠岸。渡夫捡到一页被撕碎的船期，墨迹尚未干。'),
('depot','东埠盐仓',5,.86,.85,'Recon','盐仓钥记','船期残页','沿船期追到盐仓，一批军需混在食盐中。开仓必须避开巡夜。')]
for id,name,d,x,y,duty,evidence,requires,desc in data:sites.append(dict(id=id,name=name,district=d,x=x,y=y,duty=duty,evidence=evidence,requires=requires,description=desc))
cases=[]
def add(id,title,site,duty,start,end,group,requires,body,outcome,heat,support,reward,money=0,pre='',grants='',pressure=0):
 d=next(x['district'] for x in sites if x['id']==site)
 task=dict(id=id,title=title,body=body,duty=duty,district=d,start=start,deadline=end,priority=2,intelReward=1 if duty=='Recon' else 0,progressReward=reward,failurePressure=2,prerequisite=pre)
 mission=dict(task=task,pressureDelta=pressure,money=money,retaliationDelay=2 if heat>=12 else 0,retaliationPressure=4 if heat>=12 else 0,flag='')
 cases.append(dict(mission=mission,site=site,group=group,requires=requires,grants=grants,outcome=outcome,heat=heat,support=support))
add('tea_protect','失踪跑堂 · 护送证人','tea','Contact',1,4,'missing_boy','证人口供','掌柜承认证人藏在阁楼。先送他离开，还是留下他引出幕后买家？护送会失去追踪机会。','少年平安出城，街坊开始相信组织的承诺。',-8,12,3,grants='证人获救')
add('tea_bait','失踪跑堂 · 布置诱局','tea','Recon',1,4,'missing_boy','证人口供','让证人继续赴约，以此追查买家。组织可能得到账册，但茶楼将暴露在搜捕视线中。','买家留下账册，茶楼掌柜却不再愿意接待陌生人。',14,-8,7,8,grants='买家账册')
add('ink_publish','禁书印版 · 公布真相','bookshop','Propaganda',3,7,'printing','印版印记','印版关联一名受冤的老师。公开文告可以争取学生，但搜查会集中到书坊。','学生开始传抄文告，书坊被列入重点搜查。',15,15,7)
add('ink_hide','禁书印版 · 转移印版','bookshop','Contact',3,7,'printing','印版印记','先转移印版与刻工，放弃这次公开发声。留下的工具可能在决战时发挥作用。','刻工与印版藏进学社，留下可以再次使用的联络工具。',-10,5,3,grants='备用印版')
add('prison_list','转押疑云 · 保存名单','registry','Recon',7,12,'prison_order','转押时刻','密探准备焚毁名册。先抄下所有姓名，能让更多家庭知道亲人的去向。','名单流入街坊，失踪者不再只是无名的数字。',6,12,5,grants='完整名册')
add('prison_bribe','转押疑云 · 打通书吏','registry','Contact',7,12,'prison_order','转押时刻','书吏愿意提供内线消息，但要求把自己的名字从抄录名单中抹去。','内线开始传递消息，部分家属仍得不到答复。',-8,-4,7,10,grants='书吏内线')
add('camp_oath','守卒家书 · 援助家属','camp','Contact',11,16,'soldier','换防口令','守卒提出先安顿家属，再考虑响应。公开承诺能换取信任，也让组织承担照应的责任。','守卒家属得到庇护，辕门内有人愿意为组织说话。',-6,14,5,grants='守卒信任')
add('camp_threat','守卒家书 · 施压夺令','camp','Sabotage',11,16,'soldier','换防口令','直接夺取调令可以扰乱敌营。短期收益更高，但营中支持者会重新审视组织。','调令落入组织手中，东营开始严查内部人员。',20,-12,10,12)
add('clinic_aid','药铺断供 · 赈济街坊','clinic','Contact',5,10,'medicine','伤患证词','仅剩的一车药材无法兼顾所有人。先救治街坊，会放弃一笔可观的筹款收入。','药铺重新开门，伤者的家人愿意传递消息。',-5,18,4,grants='义诊口碑')
add('clinic_sell','药铺断供 · 出售存药','clinic','Contact',5,10,'medicine','伤患证词','卖药所得能支持组织周转，但等待药材的街坊将记住这个决定。','资金到账，药铺门前却留下空手而归的人。',4,-15,5,24)
add('cargo_return','失踪夜船 · 归还粮食','ferry','Contact',9,14,'night_ship','船期残页','夜船混装了军需和百姓的粮食。把粮食交还渡夫，组织只能带走少量文件。','渡夫分发粮食，码头工人自发帮忙掩护。',-10,16,5,grants='渡夫盟约')
add('cargo_take','失踪夜船 · 扣留货物','ferry','Sabotage',9,14,'night_ship','船期残页','扣下整船货物，能迅速增加组织的行动资源，但也会影响依靠这船粮食的人。','物资转入组织仓库，码头开始出现不满和搜查。',18,-10,9,18)
add('account_truth','账册背后 · 商路证言','warehouse','Recon',5,8,'','私运账本','货栈账册与获救少年的证词相互印证。最后一页指向城内军需买家。','证词和账册完成交叉印证，线报可信度提高。',-4,6,6,pre='tea_protect',grants='军需买家')
add('secret_print','藏版重启 · 夜印号外','academy','Propaganda',8,12,'','学社名录','先前保下的印版可以再次使用。让学社分批传抄，减少单一地点暴露。','文告从多处流入街巷，学社没有成为唯一靶子。',4,10,7,pre='ink_hide')
add('families','姓名之后 · 寻访家属','refuge','Contact',13,17,'','避难名册','把转押名单与避难名册比对，让失散的人知道彼此仍然活着。','几户失散家人重新取得联系，古寺成了真正的避风处。',-8,14,6,pre='prison_list',grants='家属网络')
add('bell_signal','约定钟声 · 留一条退路','bell','Recon',17,20,'','警钟构造','得到守卒信任后，钟楼可以传出约定的撤离信号。为最后的起义准备退路。','撤离信号约定完成，守卒知道何时该放下兵器。',-12,10,8,pre='camp_oath')
content=dict(sites=sites,cases=cases)
path=root/'Assets/Minglu/Resources/Minglu/Config/city_content.json'
path.write_text(json.dumps(content,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{len(sites)} sites, {len(cases)} case approaches')
