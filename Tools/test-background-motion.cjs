const {call}=require('./unity-authoring.cjs');
const ev=code=>call('script','eval',{code}).result;
const run=call('script','run',{file:'Tools/BackgroundMotionProbe.cs',entry:'BackgroundMotionProbe.Main'});console.log(run.result);
const type=`System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==${JSON.stringify(run.assemblyName)}).GetType("BackgroundMotionProbe")`;
for(let i=0;i<12;i++){
 const status=ev(`return ${type}.GetMethod("Status").Invoke(null,null);`);
 if(status.startsWith('FAIL'))throw Error(status);
 if(status.startsWith('PASS')){console.log(status);break;}
 if(i===11)throw Error('Background motion probe did not finish');
}
console.log(JSON.stringify(call('console','get',{level:'error',tail:3})));
