const fs=require('fs'),path=require('path'),{pathToFileURL}=require('url');
(async()=>{
 const lame=await import(pathToFileURL(path.resolve('Temp/AudioEncoder/package/dist/lamejs.js')).href);
 for(const folder of ['Beethoven_FurElise','Pachelbel_CanonInD']){
  const meta=JSON.parse(fs.readFileSync('Temp/ShortClassical/'+folder+'.json'));
  const pcm=fs.readFileSync('Temp/ShortClassical/'+folder+'.pcm'),samples=new Int16Array(pcm.buffer,pcm.byteOffset,pcm.length/2);
  const encoder=new lame.Mp3Encoder(meta.channels,meta.frequency,192),chunks=[];
  for(let offset=0;offset<meta.frames;offset+=1152){const count=Math.min(1152,meta.frames-offset),left=new Int16Array(count),right=new Int16Array(count);for(let i=0;i<count;i++){left[i]=samples[(offset+i)*meta.channels];right[i]=samples[(offset+i)*meta.channels+1];}const data=encoder.encodeBuffer(left,right);if(data.length)chunks.push(Buffer.from(data));}
  const tail=encoder.flush();if(tail.length)chunks.push(Buffer.from(tail));
  const output='Temp/ShortClassical/'+folder+'.mp3';fs.writeFileSync(output,Buffer.concat(chunks));console.log(folder+': encoded '+meta.sourceEndSeconds+' seconds, '+fs.statSync(output).size+' bytes, 192 kbps MP3.');
 }
})().catch(e=>{console.error(e);process.exit(1);});
