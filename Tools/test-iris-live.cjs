const {call}=require('./unity-authoring.cjs');const fs=require('fs');
const ev=code=>call('script','eval',{code}).result;
const run=call('script','run',{file:'Tools/IrisTransitionProbe.cs',entry:'IrisTransitionProbe.Main'});
console.log(run.result);
const type='System.AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=='+JSON.stringify(run.assemblyName)+').GetType("IrisTransitionProbe")';
function method(name){return ev('return '+type+'.GetMethod('+JSON.stringify(name)+').Invoke(null,null);');}
let status=method('Status');if(!status.startsWith('PASS'))throw Error(status);console.log(status);
console.log(method('ToGameplay'));
status=method('Status');if(!status.startsWith('PASS'))throw Error(status);console.log(status);
ev('var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();g.enabled=false;var t=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.IrisSceneTransition>();typeof(RhythmRehab.IrisSceneTransition).GetField("closeDuration",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(t,4f);RhythmRehab.IrisSceneTransition.Load("Settings");return true;');
call('capture','game_view',{width:1920,height:1080,save_path:'Temp/Iris-Transition.png',source:'screen'});
fs.renameSync('Assets/Temp/Iris-Transition.png','Screenshots/Iris-Transition.png');if(fs.existsSync('Assets/Temp/Iris-Transition.png.meta'))fs.unlinkSync('Assets/Temp/Iris-Transition.png.meta');
console.log(ev('var t=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.IrisSceneTransition>();typeof(RhythmRehab.IrisSceneTransition).GetField("closeDuration",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(t,.22f);return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,phase=t.Phase,instances=UnityEngine.Object.FindObjectsByType<RhythmRehab.IrisSceneTransition>().Length};'));
