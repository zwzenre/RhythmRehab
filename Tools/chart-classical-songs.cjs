const fs=require('fs');
const songs=[['Beethoven_FurElise','FurElise.mp3','Für Elise','Ludwig van Beethoven'],['Pachelbel_CanonInD','CanonInD.mp3','Canon in D','Johann Pachelbel'],['Bach_PreludeInC','PreludeInC.mp3','Prelude in C','Johann Sebastian Bach'],['Mozart_SonataK545_Rondo','SonataK545_Rondo.mp3','Rondo K.545','Wolfgang Amadeus Mozart'],['Chopin_PreludeInEMinor','PreludeInEMinor.mp3','Prelude in E minor','Frédéric Chopin']];
const round=x=>Math.round(x*1e6)/1e6;
for(let songIndex=0;songIndex<songs.length;songIndex++){
 const [folder,mp3,title,artist]=songs[songIndex],dir='Assets/Music/Classical/'+folder;
 const midi=JSON.parse(fs.readFileSync('Temp/ClassicalAnalysis/'+folder+'.json')),audio=JSON.parse(fs.readFileSync('Temp/ClassicalAnalysis/'+folder+'-audio.json'));
 const flux=audio.energy.map((v,i)=>Math.max(0,Math.log1p(v*150)-Math.log1p((audio.energy[Math.max(0,i-1)])*150)));
 const peak=Math.max(...audio.energy),first=audio.energy.findIndex(e=>e>peak*.02)/audio.rate;
 const onset=[];for(const n of midi.notes){let g=onset.at(-1);if(!g||n.time-g.time>.018){g={time:n.time,beat:n.beat,pitch:n.pitch,velocity:n.velocity,duration:n.end-n.time};onset.push(g);}else{g.pitch=Math.max(g.pitch,n.pitch);g.velocity=Math.max(g.velocity,n.velocity);g.duration=Math.max(g.duration,n.end-n.time);}}
 const guess=first-midi.notes[0].time;
 const sample=t=>{const i=Math.round(t*audio.rate);let v=0;for(let j=-1;j<=1;j++)if(i+j>=0&&i+j<flux.length)v=Math.max(v,flux[i+j]);return v;};
 let fit={score:-1,offset:guess,scale:1};
 const anchors=onset.filter((n,i)=>i%2===0&&n.velocity>20);
 for(let scale=.998;scale<=1.002001;scale+=.0001)for(let offset=guess-.2;offset<=guess+.2001;offset+=.005){let score=0;for(const n of anchors)score+=sample(n.time*scale+offset)*(n.velocity/127);if(score>fit.score)fit={score,offset,scale};}
 let distance=0;for(const n of anchors){const time=n.time*fit.scale+fit.offset;let best=0,bestD=0;for(let d=-.08;d<=.0801;d+=.005){const value=sample(time+d)*Math.exp(-Math.abs(d)*8);if(value>best){best=value;bestD=d;}}distance+=Math.abs(bestD);}
 const pitchSorted=onset.map(n=>n.pitch).sort((a,b)=>a-b),low=pitchSorted[Math.floor(pitchSorted.length*.1)],high=pitchSorted[Math.floor(pitchSorted.length*.9)];
 const lead=4,bpm=Math.round(midi.bpm*10)/10;
 const edit=fs.existsSync(dir+'/AudioEdit.json')?JSON.parse(fs.readFileSync(dir+'/AudioEdit.json')):null;
 const noteEnd=edit?edit.sourceEndSeconds-edit.fadeOutSeconds:audio.duration-.5;
 const candidates=onset.map(n=>({...n,time:n.time*fit.scale+fit.offset})).filter(n=>n.time>=0&&n.time<Math.min(audio.duration-1.5,noteEnd-.9));
 const charts={};
 for(const [name,minGap,range,maxStep,holdBase] of [['Therapy',2.6,0,0,1.8],['Easy',2.3,.5,.25,1.7],['Medium',1.35,1,.375,1.25],['Hard',.85,1,.625,1]]){
  const chosen=[];let cursor=0,next=candidates[0].time,height=name==='Therapy'?.5:0;
  while(cursor<candidates.length){while(cursor<candidates.length&&candidates[cursor].time<next-.001)cursor++;if(cursor>=candidates.length)break;
   const firstCandidate=cursor;let best=cursor,priority=-Infinity;
   while(cursor<candidates.length&&candidates[cursor].time<=candidates[firstCandidate].time+.45){const n=candidates[cursor],grid=Math.abs(n.beat-Math.round(n.beat)),p=(grid<.06?2:grid<.27?1:0)+n.velocity/180-(n.time-candidates[firstCandidate].time)*1.1;if(p>priority){best=cursor;priority=p;}cursor++;}
   const n=candidates[best];cursor=best+1;
   if(name!=='Therapy'){const melodic=Math.max(0,Math.min(1,(n.pitch-low)/Math.max(1,high-low))),phrase=Math.floor(n.beat/8);const contour=[0,.25,.5,.75,1,.75,.5,.25][(phrase+songIndex*2)%8];let target=(melodic*.7+contour*.3)*range;target=Math.round(target/(name==='Hard'?.0625:.125))*(name==='Hard'?.0625:.125);height=Math.max(0,Math.min(range,Math.max(height-maxStep,Math.min(height+maxStep,target))));}
   const isHold=(chosen.length+songIndex*2)%8===3||n.duration>=1.6&&chosen.length%5===2;
   let hold=isHold?Math.min(holdBase+(n.duration>.9?.3:0),audio.duration-n.time-1.1,noteEnd-n.time-.35):0;if(hold<.9)hold=0;
   chosen.push({beat:round((n.time+lead)*bpm/60),lane:Math.round(height*2),useCustomHeight:name!=='Therapy',customHeight:round(height),holdBeats:round(hold*bpm/60)});
   next=n.time+Math.max(minGap,hold+.65);
  }
  charts[name]={description:`${title} — ${name}. Note timings follow the source MIDI tempo map aligned to this MP3. ${name==='Therapy'?'Height is replaced by the selected therapy level.':'Heights follow melodic contour and phrase shapes.'} Four-second audio lead-in.`,durationSeconds:round(lead+audio.duration),notes:chosen};
  fs.mkdirSync(dir+'/Charts',{recursive:true});fs.writeFileSync(dir+'/Charts/'+name+'.json',JSON.stringify(charts[name],null,2));
 }
 const metadata={folder,mp3,title,artist,bpm,leadIn:lead,audioDuration:audio.duration,shortened:!!edit,fadeOutSeconds:edit?edit.fadeOutSeconds:0,alignment:{offsetSeconds:round(fit.offset),scale:round(fit.scale),meanAttackDistanceMs:round(distance/anchors.length*1000)},charts:Object.fromEntries(Object.entries(charts).map(([k,v])=>[k,{notes:v.notes.length,holds:v.notes.filter(n=>n.holdBeats>0).length}]))};
 fs.writeFileSync(dir+'/ChartingNotes.json',JSON.stringify(metadata,null,2));console.log(JSON.stringify(metadata));
}
