import fs from 'node:fs/promises';
import path from 'node:path';
import { Workbook, SpreadsheetFile } from '@oai/artifact-tool';

const out = path.resolve('outputs/minglu-v02');
const tables = {
Settings: [
['id','value','description'],
['#参数键','整数值','规则说明'],
['turns',8,'单局回合上限；2—40'],['goal',24,'胜利所需行动成果'],['ap',3,'全组织每回合行动点；1—8'],
['startingIntel',1,'开局情报份数'],['startingPressure',15,'开局敌方压力'],['pressurePerTurn',4,'敌方每回合压力增长'],
['patrolThreshold',50,'达到此压力后全员增加暴露'],['patrolExposure',5,'高压巡查暴露增加量'],['captureThreshold',70,'敌方结算时被捕的暴露阈值'],
['leakTurn',2,'被捕后第几次审讯泄密'],['lostTurn',4,'第几次审讯后永久失联；必须大于泄密时间'],['leakExposure',40,'直接下线被供出后增加的暴露'],
['restStamina',25,'休整体力恢复'],['restExposure',15,'休整降低暴露'],['cutExposure',25,'断线降低暴露'],['rescuedStamina',50,'获救成员体力'],['rescuedExposure',25,'获救成员暴露']
],
Agents: [
['id','name','role','bio','intel','stealth','handler','portrait'],
['#唯一编号','姓名','表身份','人物小传','情报骰数1—8','潜行骰数1—8','上线ID；-1为领导者','旧档兼容编号0—3；不再对应头像图集'],
[0,'沈默','联络员','昔日衙门书吏，熟悉城中暗巷。他将每封密信背熟，再焚于灯下。顾青是他唯一的下线。',3,4,-1,0],
[1,'顾青','书坊伙计','替人抄书谋生，记得每一位客人的口音。她相信文字可以改变这座城，也正在学会保守秘密。',4,3,0,1],
[2,'陈砚','行商','凭一杆秤走遍南北，惯于把真话藏在闲谈里。曾受林叔救命之恩，愿替他挡一阵风。',3,3,-1,2],
[3,'林叔','郎中','行医三十年，门前从不问来者姓名。袖中藏着药方，也藏着尚未送出的家书。',2,4,2,3]
],
Districts: [
['id','name','subtitle','description','x','y','risk','reconBonus','sabotageBonus','difficulty'],
['#唯一编号','地图名称','区域标签','行动提示','横坐标0—1','纵坐标0—1；从顶部','额外暴露','侦察情报加成','破坏成果加成','额外需求成功数'],
[0,'西市','商贾 / 人流','商铺与茶摊连成一片，人群能掩护行踪。这里适合稳妥地建立第一条情报线。',.20,.56,0,0,0,0],
[1,'文津书院','士林 / 消息','讲席后的闲谈往往比公文更早。侦察额外取得1份情报，代价是更容易留下踪迹。',.22,.20,5,1,0,0],
[2,'北衙','政令 / 牢狱','高墙内保存着要紧名册。戒备森严，任务额外需要1个成功；破坏成果额外增加4。',.51,.19,12,0,4,1],
[3,'东营','驻军 / 巡防','巡哨轮换有固定间隙。深入此地格外危险，但破坏军需能获得额外3点成果。',.80,.24,10,0,3,1],
[4,'城南古寺','香客 / 隐蔽','钟声掩住低语，廊下适合接头。没有额外风险，适合疲惫成员执行普通行动。',.50,.66,0,0,0,0],
[5,'东埠码头','漕运 / 仓储','粮船与货栈昼夜不息。破坏额外获得2点成果，暴露增加8。',.81,.66,8,0,2,0]
],
Actions: [
['id','name','description','ap','intelCost','required','staminaCost','exposure','failExposure','intelReward','progressReward','pressure'],
['#固定行为ID','显示名','任务说明','AP消耗','情报消耗','成功数需求','体力消耗','暴露增加','失败额外暴露','成功情报奖励','成功成果奖励','成功压力增加'],
['Rest','隐匿休整','不外出行动，恢复体力并淡去行踪。恢复量由Settings控制。',0,0,0,0,0,0,0,0,0],
['Recon','探听消息','从街巷与往来书信中寻找线索。取得的情报下回合才能用于行动。',1,0,1,20,10,15,2,2,0],
['Sabotage','破坏部署','利用已有情报破坏敌方部署。成功推进目标，同时引来敌方反应。',1,1,2,20,20,15,0,8,8],
['Rescue','秘密营救','营救所选被捕成员。本回合行动先于审讯；失败也消耗情报。',1,1,2,20,20,15,0,0,0],
['CutContact','断线转移','更换联络地址，脱离旧上线，改由领导者直接联系；旧上线不能沿旧地址追踪此人。',1,0,0,0,0,0,0,0,0]
],
Events: [
['id','title','body','optionA','optionB','turn','intelA','pressureA','progressA','intelB','pressureB','progressB'],
['#唯一事件ID','密报标题','密报正文','选择A','选择B','出现回合','A情报变化','A压力变化','A成果变化','B情报变化','B压力变化','B成果变化'],
['curfew','暮鼓之后','城门提前落锁，北衙正在核对路引。茶馆掌柜愿用一份情报替你们疏通今夜的巡查。','交出线索，疏通巡查','保留情报，静候风声',2,-1,-8,0,0,4,0],
['courier','无名来信','一封未署名的信指出军需账目的破绽。冒险接头可以获得更多线索，也可能落入敌人的视线。','赴约接头','只收下信件',4,2,10,0,1,0,0],
['lantern','长夜中的灯','城南百姓愿为你们掩护一次行动。公开响应可以推进组织影响，低调接纳则能让搜捕暂缓。','回应呼声','隐藏火种',6,0,10,4,0,-10,0]
],
Assets: [
['id','resource','kind','description'],
['#资源ID','Resources相对路径，不带扩展名','资源类型','用途'],
['city','Minglu/Art/city-v02','Texture2D','六区域工笔城市底图；节点由Districts坐标定位']
]
};
await fs.mkdir(out, {recursive:true});
await fs.mkdir(path.join(out,'csv'), {recursive:true});
const wb=Workbook.create();
const guide=wb.worksheets.add('使用说明');
guide.showGridLines=false;
const instructions=[['明路·地下 游戏配置 v0.2',''],['步骤','操作'],['1 编辑','修改各表第3行以后的数据；第1行为程序字段，第2行为说明。'],['2 保存','保存xlsx；数值列只填整数或小数，不能填公式。'],['3 校验','Unity菜单 Minglu / 配置表工具，选择本工作簿，点击预览与校验。'],['4 导入','校验通过后点击重新读取并导入。错误不会覆盖当前运行配置。'],['5 生效','新开局加载新配置；读档继续使用存档里的配置快照。'],['CSV入口','也可选择csv目录批量导入，六张表必须齐全。'],['主键','ID不可重复；角色上线需存在且不能成环。'],['地图','坐标为0—1，从左上角计算；修改节点坐标不改变底图建筑。'],['角色','支持2—8人；portrait为旧档兼容编号0—3。新人物使用完整头部与独立五官目录，不再加载四列头像图集。'],['行动','五种行为ID固定。Rest免费；CutContact不消耗情报。'],['事件','每回合最多一条；从第2回合开始；资源不足的选项会禁用。'],['范围','本版为单城八回合体验；参数为设计初值，不是已验证的平衡结果。'],['来源','依据用户系统策划和原型规则整合；区域与密报为本轮新增设计。']];
guide.getRange(`A1:B${instructions.length}`).values=instructions;
guide.getRange(`A1:B${instructions.length}`).format.font={name:'Microsoft YaHei',size:11,color:'#253B3C'};
guide.getRange('A1:B1').format.font={name:'Microsoft YaHei',size:16,bold:true,color:'#203E42'};
guide.getRange('A1:A15').format.columnWidth=27; guide.getRange('B1:B15').format.columnWidth=100;
guide.getRange('A1:B15').format.rowHeight=30;
guide.getRange('A2:B2').format.fill='#203E42';guide.getRange('A2:B2').format.font.color='#FFFFFF';
for(const [name,rows] of Object.entries(tables)){
 const sh=wb.worksheets.add(name); sh.showGridLines=false; sh.freezePanes.freezeRows(2);
 const n=rows[0].length;
 const range=sh.getRangeByIndexes(0,0,rows.length,n);range.values=rows;
 range.format.font={name:'Microsoft YaHei',size:11,color:'#253B3C'};range.format.rowHeight=30;range.format.columnWidth=18;range.format.verticalAlignment='center';
 sh.getRangeByIndexes(0,0,1,n).format={fill:'#203E42',font:{name:'Microsoft YaHei',size:11,color:'#FFFFFF',bold:true},rowHeight:32};
 sh.getRangeByIndexes(1,0,1,n).format={fill:'#ECE7D9',font:{name:'Microsoft YaHei',size:10,color:'#606D68'},rowHeight:42,wrapText:true};
 sh.getRangeByIndexes(2,0,rows.length-2,n).format.fill='#FFFCF2';
 for(let j=0;j<n;j++){
   const numeric=typeof rows[2][j]==='number';
   if(numeric){ sh.getRangeByIndexes(2,j,rows.length-2,1).setNumberFormat(['x','y'].includes(rows[0][j])?'0.00':'0'); }
   const field=rows[0][j];
   const width=['body','description','bio'].includes(field)?58:['resource'].includes(field)?48:['optionA','optionB'].includes(field)?27:['id'].includes(field)?23:18;
   sh.getRangeByIndexes(0,j,rows.length,1).format.columnWidth=width;
   if(['body','description','bio'].includes(field)){sh.getRangeByIndexes(2,j,rows.length-2,1).format.wrapText=true;sh.getRangeByIndexes(2,0,rows.length-2,n).format.rowHeight=58;}
 }
 if(name==='Agents'){sh.getRange('E3:F6').dataValidation={rule:{type:'whole',operator:'between',formula1:1,formula2:8}};sh.getRange('H3:H6').dataValidation={rule:{type:'whole',operator:'between',formula1:0,formula2:3}};}
 const csv=rows.map(row=>row.map(v=>'"'+String(v).replaceAll('"','""')+'"').join(',')).join('\r\n')+'\r\n';
 await fs.writeFile(path.join(out,'csv',name+'.csv'),'\uFEFF'+csv);
}
wb.recalculate();
console.log((await wb.inspect({kind:'table',range:'Settings!A1:C7',include:'values,formulas',tableMaxRows:7,tableMaxCols:3,maxChars:1800})).ndjson);
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A',options:{useRegex:true,maxResults:10},maxChars:1000})).ndjson);
await (await SpreadsheetFile.exportXlsx(wb)).save(path.join(out,'明路地下_游戏配置.xlsx'));
for(const name of ['使用说明',...Object.keys(tables)]){
 const blob=await wb.render({sheetName:name,range:name==='使用说明'?'A1:B15': name==='Settings'?'A1:C19':name==='Agents'?'A1:H6':name==='Districts'?'A1:J8':name==='Actions'?'A1:L7':name==='Events'?'A1:L5':'A1:D3',scale:1,format:'png'});
 await fs.writeFile(path.join(out,name+'-preview.png'),new Uint8Array(await blob.arrayBuffer()));
}
console.log('WORKBOOK_READY '+out);
