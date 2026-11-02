const {call}=require('./unity-authoring.cjs');
const ev=code=>call('script','eval',{code}).result;
const run=call('script','run',{file:'Tools/UIInteractionFeedbackProbe.cs',entry:'UIInteractionFeedbackProbe.Main'});console.log(run.result);
const type=`System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name==${JSON.stringify(run.assemblyName)}).GetType("UIInteractionFeedbackProbe")`;
for(let i=0;i<15;i++){
 const status=ev(`return ${type}.GetMethod("Status").Invoke(null,null);`);
 if(status.startsWith('FAIL'))throw Error(status);
 if(status.startsWith('PASS')){console.log(status);break;}
 if(i===14)throw Error('Interaction probe did not finish');
}
console.log(JSON.stringify(call('console','get',{level:'error',tail:4})));
