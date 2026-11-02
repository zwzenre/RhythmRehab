const {call}=require('./unity-authoring.cjs');
const run=call('script','run',{file:'Tools/VerifyHoldInputLoop.cs',entry:'VerifyHoldInputLoop.Main'});console.log(run.result);
const type=`System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==${JSON.stringify(run.assemblyName)}).GetType("VerifyHoldInputLoop")`;
for(let i=0;i<8;i++){
 const status=call('script','eval',{code:`return ${type}.GetMethod("Status").Invoke(null,null);`}).result;
 if(status.startsWith('FAIL'))throw Error(status);
 if(status.startsWith('PASS')){console.log(status);break;}
 if(i===7)throw Error('Input checks did not finish');
}
