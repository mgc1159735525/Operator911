from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
content=dict(id='xinjing',title='新京二十夜',description='在四个阶段中重建地下联络，争取民众与守军，发动最终起义。',finalMission='uprising',turns=20,goal=70,income=12,startingMoney=48,
 stages=[dict(id=i,name=n,start=a,end=b,pressure=p,description=d) for i,n,a,b,p,d in [
 ('hidden','蛰伏',1,4,1,'建立安全联络，寻找可以信任的人。'),('active','活跃',5,10,2,'联络街坊与工匠，扩大支持网络。'),('pressure','高压',11,16,3,'敌方搜捕升级，争取守军并保护群众。'),('final','决战',17,20,4,'行动窗口即将关闭，完成最终起义。')]],
 characters=[dict(id=i,role=r,cover=c,persuade=p,organize=o,bio=b) for i,r,c,p,o,b in [
 (0,'地下联络员','军官',3,3,'沈默曾在城防营任职，熟悉岗哨调度。破坏行动获得1枚身份骰。'),(1,'书坊伙计','教师',3,4,'顾青借抄书传授识字，熟悉学社人脉。散布思想获得1枚身份骰。'),(2,'往来行商','商人',4,2,'陈砚以货物账本掩护联络，熟悉码头脚夫。秘密接头获得1枚身份骰。'),(3,'城南郎中','医生',2,3,'林叔借行医走街串巷，为同伴维持最后一处落脚点。医生专属治疗尚未开放。')]],missions=[],events=[])
rows=[
 ('shelter','古寺落脚','Contact',4,1,4,'',3,4,-3,8,'接头人只在暮鼓后等候。取得住持信任，为后续联络留下安全地址。'),
 ('roster','更夫名册','Recon',0,1,3,'',2,2,0,0,'西市更夫知道巡夜换岗的时辰。记录班次，避开第一次搜查。'),
 ('school','书院灯火','Propaganda',1,2,4,'',2,4,-2,0,'学生愿意传抄识字小册，但需要一个值得信任的组织者。'),
 ('goods','药材暗格','Contact',5,2,4,'',1,3,-2,12,'药材船停泊两夜。取得船主支持，把物资送进城南。'),
 ('network','重接旧线','Contact',0,5,10,'shelter',3,6,-5,8,'凭古寺约定的暗号重接街坊旧线。这是争取城防营的必要前置。'),
 ('print','夜印文告','Propaganda',1,5,8,'school',2,6,0,0,'书院愿意提供刻版。把组织的主张传到工坊与码头。'),
 ('supply','截断军饷','Sabotage',5,6,9,'roster',2,7,2,16,'沿更夫提供的空隙截走运饷账册。两夜后敌方将扩大搜查。'),
 ('workers','工坊盟约','Contact',0,7,10,'',2,5,-5,8,'工匠不愿卷入无望的争斗。说服他们为城中变化留下一份力量。'),
 ('witness','失踪证人','Recon',2,8,10,'',1,4,-2,0,'北衙书吏藏着被捕名单。设法获取名单，而不是惊动整座衙门。'),
 ('garrison','守军密约','Contact',3,11,16,'network',3,10,-6,0,'城防营中有人愿意响应，但只相信已经建立街坊联络的组织。此约决定起义能否发动。'),
 ('refuge','护送街坊','Contact',4,11,14,'',2,5,-4,0,'搜捕波及无辜家庭。联系古寺安排躲避之所，为百姓留下退路。'),
 ('prison','转押路线','Recon',2,12,15,'witness',2,5,-2,0,'被捕名单对应一条新的转押路线。拿到线索后营救行动仍需单独安排。'),
 ('signal','破坏警钟','Sabotage',3,13,16,'',2,8,0,0,'敌人用警钟调动巡队。破坏钟楼机关，但要准备承受两夜后的报复。'),
 ('solidarity','连夜互助','Propaganda',0,14,16,'workers',1,6,-6,0,'让工坊中的互助成为公开的承诺，减轻街坊对搜查的恐惧。'),
 ('uprising','最终起义','Propaganda',0,17,20,'garrison',3,12,-10,0,'守军已承诺响应。汇合街坊、发出约定信号；还需达到本局成果目标才能成功。'),
 ('harbor','封锁退路','Sabotage',5,17,19,'',2,7,0,0,'敌方准备从东埠撤走物资。切断这条运输线，为起义争取时间。'),
 ('lanterns','千家灯火','Propaganda',1,18,20,'print',2,8,-8,0,'让早先传抄的文告成为起义信号。窗前灯火亮起，街巷终于互相看见。'),
 ('lastboat','最后渡船','Contact',4,18,20,'',1,4,-5,8,'为尚未撤离的人安排最后的渡船。即使行动失败，也要保住后来者。')]
for id,title,duty,area,start,end,pre,priority,reward,pressure,money,body in rows:
 retaliation=id in ('supply','signal','harbor')
 content['missions'].append(dict(task=dict(id=id,title=title,body=body,duty=duty,district=area,start=start,deadline=end,prerequisite=pre,priority=priority,intelReward=1 if duty=='Recon' else 0,progressReward=reward,failurePressure=2 if priority==3 else 1),pressureDelta=pressure,money=money,retaliationDelay=2 if retaliation else 0,retaliationPressure=4 if retaliation else 0,flag='shelter_people' if id=='refuge' else ''))
events=[(2,'curfew','暮鼓后的盘查','西市开始检查夜行人的路引。旧线可能已被盯上。','花1情报绕开岗哨','暂缓联络',-1,0,-6,2,0,0),(4,'courier','驿使来信','城外同伴送来一页名单，接信会留下踪迹。','收下完整名单','只记暗号',2,1,5,0,0,0),(5,'awakening','城中暗流','几家工坊同时询问组织的主张。活跃阶段开始。','公开互助主张','谨慎传话',0,1,3,-2,4,0),(8,'fund','商路邀约','行商愿意递送消息，希望组织表明承诺。','提供2份线报','暂保密',-2,0,-5,2,5,0),(11,'hunt','封城搜捕','北衙增设岗哨。高压阶段，休整与身份维持更为重要。','用1情报调整联络','保持原计划',-1,0,-7,3,0,2),(13,'shelter_choice','百姓的门','搜捕队逐户追查。一户陌生人请求借用组织的藏身处。','花2情报安排庇护','保留安全屋',-2,0,3,-3,2,0),(16,'dawn','城防营的答复','决战将至。守军是否响应，取决于此前的密约任务。','再送1份联络情报','等待时机',-1,0,-5,0,3,0),(17,'signalnight','决战之夜','城门将于四夜后关闭。完成最终起义并达到成果目标，才能迎来黎明。','提前发动街坊','保持隐蔽',0,0,5,-4,4,0),(19,'lastlight','将熄的灯','最后的行动窗口只剩两夜。所有人都在等待决定。','用2情报协调行动','保存火种',-2,0,-5,0,5,0)]
for t,id,title,body,a,b,ia,ib,pa,pb,ga,gb in events:content['events'].append(dict(id=id,title=title,body=body,turn=t,optionA=a,optionB=b,intelA=ia,intelB=ib,pressureA=pa,pressureB=pb,progressA=ga,progressB=gb))
(root/'Assets/Minglu/Resources/Minglu/Config/campaign_xinjing.json').write_text(json.dumps(content,ensure_ascii=False,indent=2),encoding='utf-8')

