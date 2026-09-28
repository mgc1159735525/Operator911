import fs from 'node:fs/promises';
import path from 'node:path';
import {FileBlob, SpreadsheetFile} from '@oai/artifact-tool';
const out=path.resolve('outputs/minglu-v03');
await fs.mkdir(path.join(out,'csv'),{recursive:true});
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load('outputs/minglu-v02/明路地下_游戏配置.xlsx'));
const names=['Settings','Agents','Districts','Actions','Events','Assets'];
const ranges=['A1:C19','A1:H6','A1:J8','A1:L7','A1:L5','A1:D4'];
const snapshots=names.map((n,i)=>JSON.stringify(wb.worksheets.getItem(n).getRange(ranges[i]).values));
const rows=[
 ['id','title','body','duty','prerequisite','district','start','deadline','priority','intelReward','progressReward','failurePressure'],
 ['#任务编号','标题','线报内容','Recon或Sabotage','前置任务ID；可空','区域ID','开放夜','截止夜；含当夜','优先级1—3','额外情报','额外成果','逾期压力'],
 ['ledger','书坊暗账','书坊伙计发现异常购纸记录，两夜内可查明去向。','Recon','',1,1,2,2,1,1,5],
 ['cargo','码头换防','货栈换防尚未结束，今夜可扰乱一批军需。','Sabotage','',5,1,1,3,0,2,4],
 ['trail','顺藤摸瓜','循暗账寻找城南接头人，建立后续线索。','Recon','ledger',4,3,4,2,2,1,4],
 ['patrol','巡哨名册','西市出现陌生巡哨，查清路线以免连累住户。','Recon','',0,3,4,3,1,1,6],
 ['supply','断其粮道','从接头人处得知军需缺口，东营行动窗口已打开。','Sabotage','trail',3,5,6,3,0,3,6],
 ['archive','名册将焚','北衙将销毁一批秘密文书，须抢在火起前搜集线索。','Recon','',2,5,6,2,2,1,5],
 ['signal','最后的信号','行动窗口将闭，西市仍有一处可扰乱的部署。','Sabotage','',0,7,8,3,0,3,7]
];
const sh=wb.worksheets.add('Missions');sh.showGridLines=false;sh.freezePanes.freezeRows(2);
sh.getRange('A1:L9').values=rows;
sh.getRange('A1:L9').format={font:{name:'Microsoft YaHei',size:11,color:'#253B3C'},rowHeight:58,columnWidth:18,verticalAlignment:'center',wrapText:true};
sh.getRange('A1:L1').format={fill:'#203E42',font:{name:'Microsoft YaHei',size:11,color:'#FFFFFF',bold:true},rowHeight:32};
sh.getRange('A2:L2').format={fill:'#ECE7D9',font:{name:'Microsoft YaHei',size:10,color:'#606D68'},rowHeight:42,wrapText:true};
sh.getRange('A3:L9').format.fill='#FFFCF2';sh.getRange('A1:A9').format.columnWidth=23;sh.getRange('C1:C9').format.columnWidth=58;sh.getRange('F3:L9').setNumberFormat('0');
sh.getRange('I3:I9').dataValidation={rule:{type:'whole',operator:'between',formula1:1,formula2:3}};
const guide=wb.worksheets.getItem('使用说明');guide.getRange('A1').values=[['明路·地下 游戏配置 v0.3']];
guide.getRange('B8').values=[['也可选择csv目录；六张基础表必须齐全，Missions为新增任务表。']];
guide.getRange('A16:B18').values=[['限时任务','Missions：截止夜仍可行动；奖励叠加常规行动奖励；前置必须先于后续任务开放夜截止。'],['核实与轮休','核实固定消耗1情报并为该任务加1骰；连续出勤两夜后减1骰，休整清除。这两项暂为代码规则。'],['设计来源','任务文案与数值为本项目原创设计初值；参考911 Operator的分流和This Is the Police的管理取舍。']];
guide.getRange('A16:B18').format={font:{name:'Microsoft YaHei',size:11,color:'#253B3C'},rowHeight:48,wrapText:true};
wb.recalculate();
for(let i=0;i<names.length;i++) if(JSON.stringify(wb.worksheets.getItem(names[i]).getRange(ranges[i]).values)!==snapshots[i]) throw new Error('Preservation failure '+names[i]);
for(let i=0;i<names.length;i++){
 const vals=wb.worksheets.getItem(names[i]).getRange(ranges[i]).values;
 await fs.writeFile(path.join(out,'csv',names[i]+'.csv'),'\uFEFF'+vals.map(r=>r.map(v=>'"'+String(v??'').replaceAll('"','""')+'"').join(',')).join('\r\n')+'\r\n');
}
await fs.writeFile(path.join(out,'csv/Missions.csv'),'\uFEFF'+rows.map(r=>r.map(v=>'"'+String(v).replaceAll('"','""')+'"').join(',')).join('\r\n')+'\r\n');
console.log((await wb.inspect({kind:'table',range:'Missions!A1:L9',include:'values,formulas',tableMaxRows:9,tableMaxCols:12,maxChars:5000})).ndjson);
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:20},maxChars:1000})).ndjson);
await (await SpreadsheetFile.exportXlsx(wb)).save(path.join(out,'明路地下_游戏配置.xlsx'));
for(const [name,range] of [['Missions','A1:L9'],['使用说明','A1:B18']]){
 const blob=await wb.render({sheetName:name,range,scale:1,format:'png'});
 await fs.writeFile(path.join(out,name+'-preview.png'),new Uint8Array(await blob.arrayBuffer()));
}
console.log('DISPATCH_WORKBOOK_READY; six original configuration sheets preserved');
