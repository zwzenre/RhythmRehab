const {call}=require('./unity-authoring.cjs');
const ev=code=>call('script','eval',{code}).result;
const run=call('script','run',{file:'Tools/UIScreenAudioProbe.cs',entry:'UIScreenAudioProbe.Main'});console.log(run.result);
const type=`System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==${JSON.stringify(run.assemblyName)}).GetType("UIScreenAudioProbe")`;
for(let i=0;i<20;i++){
 const status=ev(`return ${type}.GetMethod("Status").Invoke(null,null);`);
 if(status.startsWith('FAIL'))throw Error(status);
 if(status.startsWith('PASS')){console.log(status);break;}
 if(i===19)throw Error('Audio probe did not finish');
}
console.log(JSON.stringify(call('console','get',{level:'error',tail:3})));
