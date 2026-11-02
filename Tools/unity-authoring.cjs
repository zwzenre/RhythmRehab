const cp=require('child_process');
function call(group,name,params={}){
 const args=['command',group,name,'--caller','plugin','--skill','ui-ugui','--format','json'];
 for(const [key,value] of Object.entries(params))args.push('--'+key,typeof value==='string'?value:JSON.stringify(value));
 const r=JSON.parse(cp.execFileSync('C:/Program Files/Unity Hub/resources/unity.exe',args,{encoding:'utf8',timeout:120000,maxBuffer:16*1024*1024}));
 if(!r.success||r.data.result?.success===false)throw Error(JSON.stringify(r));return r.data.result;
}
module.exports={call};
