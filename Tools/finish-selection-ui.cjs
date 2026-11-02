const {call}=require('./unity-authoring.cjs');
const cp=require('child_process');
function wire(){const r=call('script','run',{file:'Tools/WireSelectionUI.cs',entry:'WireSelectionUI.Main'});console.log(r.result);}
wire();
for(const name of ['ModeSelection','ExerciseSelection','DifficultySelection']){
 cp.execFileSync(process.execPath,['Tools/build-selection-ui.cjs',name],{stdio:'inherit'});wire();
}
for(const name of ['Gameplay','Results']){call('scene','open',{path:'Assets/Scenes/'+name+'.unity'});wire();}
for(const [file,entry] of [['Tools/VerifyRhythmPrototype.cs','VerifyRhythmPrototype.Main'],['Tools/VerifySelectionFlow.cs','VerifySelectionFlow.Main']])console.log(call('script','run',{file,entry}).result);
