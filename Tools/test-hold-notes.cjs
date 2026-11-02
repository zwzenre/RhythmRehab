const {call}=require('./unity-authoring.cjs');
const run=call('script','run',{file:'Tools/HoldNotesProbe.cs',entry:'HoldNotesProbe.Main'});console.log(run.result);
if(run.result.startsWith('FAIL'))throw Error(run.result);
const type=`System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==${JSON.stringify(run.assemblyName)}).GetType("HoldNotesProbe")`;
for(let i=0;i<12;i++){
 const status=call('script','eval',{code:`return ${type}.GetMethod("Status").Invoke(null,null);`}).result;
 if(status.startsWith('FAIL'))throw Error(status);
 if(status.startsWith('PASS')){console.log(status);break;}
 if(i===11)throw Error('Hold checks did not finish');
}
console.log(JSON.stringify(call('console','get',{level:'error',tail:2})));
