const {call}=require('./unity-authoring.cjs');
const ev=code=>call('script','eval',{code}).result;
const entry=process.argv[2]==='settings'?'Settings':'Main';
const run=call('script','run',{file:'Tools/TextSlideInProbe.cs',entry:'TextSlideInProbe.'+entry});
console.log(run.result);
const type=`System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==${JSON.stringify(run.assemblyName)}).GetType("TextSlideInProbe")`;
for(let i=0;i<12;i++){
 const status=ev(`return ${type}.GetMethod("Status").Invoke(null,null);`);
 if(status.startsWith('FAIL'))throw Error(status);
 if(status.startsWith('PASS')){console.log(status);break;}
 if(i===11)throw Error('Probe did not finish');
}
console.log(JSON.stringify(call('capture','game_view',{width:1920,height:1080,save_path:'Temp/Text-Slide-Performance.png',source:'screen'})));
console.log(JSON.stringify(call('console','get',{level:'error',tail:3})));
