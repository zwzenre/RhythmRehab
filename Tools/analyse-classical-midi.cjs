const fs=require('fs');
const path=require('path');
const folders=['Beethoven_FurElise','Pachelbel_CanonInD','Bach_PreludeInC','Mozart_SonataK545_Rondo','Chopin_PreludeInEMinor'];
function parse(file){
 const b=fs.readFileSync(file);let p=0;const u16=()=>{const v=b.readUInt16BE(p);p+=2;return v;},u32=()=>{const v=b.readUInt32BE(p);p+=4;return v;};
 if(b.toString('ascii',0,4)!=='MThd')throw Error('Not MIDI');p=4;const header=u32(),format=u16(),tracks=u16(),division=u16();p=8+header;if(division&0x8000)throw Error('SMPTE not supported');
 const tempos=[{tick:0,us:500000}],notes=[];let endTick=0;
 function vlq(){let value=0,byte;do{byte=b[p++];value=value*128+(byte&127);}while(byte&128);return value;}
 for(let tr=0;tr<tracks;tr++){
  if(b.toString('ascii',p,p+4)!=='MTrk')throw Error('Missing MIDI track');p+=4;const size=u32(),end=p+size;let tick=0,status=0;const active=new Map();
  while(p<end){tick+=vlq();let command=b[p++];if(command<128){p--;command=status;}else if(command<240)status=command;
   if(command===255){const meta=b[p++],len=vlq();if(meta===81)tempos.push({tick,us:b.readUIntBE(p,3)});p+=len;}
   else if(command===240||command===247){p+=vlq();}
   else{const type=command>>4,ch=command&15,a=b[p++],v=type===12||type===13?0:b[p++];const key=ch+':'+a;
    if(type===9&&v>0){if(ch!==9){const n={tick,endTick:tick,pitch:a,velocity:v,track:tr};notes.push(n);active.set(key,n);}}
    else if(type===8||type===9&&v===0){const n=active.get(key);if(n){n.endTick=tick;active.delete(key);}}
   }
  }
  endTick=Math.max(endTick,tick);p=end;
 }
 tempos.sort((a,b)=>a.tick-b.tick);const unique=[];for(const t of tempos){if(unique.length&&unique.at(-1).tick===t.tick)unique[unique.length-1]=t;else unique.push(t);}
 let seconds=0;for(let i=0;i<unique.length;i++){if(i)seconds+=(unique[i].tick-unique[i-1].tick)/division*unique[i-1].us/1e6;unique[i].time=seconds;}
 function time(tick){let t=unique[0];for(const next of unique){if(next.tick>tick)break;t=next;}return t.time+(tick-t.tick)/division*t.us/1e6;}
 notes.sort((a,b)=>a.tick-b.tick||a.pitch-b.pitch);for(const n of notes){n.time=time(n.tick);n.end=time(n.endTick);n.beat=n.tick/division;}
 const durations=new Map();for(let i=0;i<unique.length;i++){const t=unique[i],duration=time(i+1<unique.length?unique[i+1].tick:endTick)-t.time;durations.set(t.us,(durations.get(t.us)||0)+duration);}
 const us=[...durations].sort((a,b)=>b[1]-a[1])[0][0];
 return {format,division,bpm:60000000/us,midiDuration:time(endTick),tempos:unique,notes};
}
fs.mkdirSync('Temp/ClassicalAnalysis',{recursive:true});
for(const folder of folders){const result=parse(path.join('Assets/Music/Classical',folder,'SourceTiming.mid'));fs.writeFileSync('Temp/ClassicalAnalysis/'+folder+'.json',JSON.stringify(result));console.log(JSON.stringify({song:folder,notes:result.notes.length,bpm:result.bpm,midiDuration:result.midiDuration,tempoChanges:result.tempos.length,first:result.notes[0],last:result.notes.at(-1)}));}
