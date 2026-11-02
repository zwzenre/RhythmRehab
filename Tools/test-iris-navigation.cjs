const {call}=require('./unity-authoring.cjs');const fs=require('fs'),path=require('path');
const ev=code=>call('script','eval',{code}).result;let checks=0;
function scene(name){ev('if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!='+JSON.stringify(name)+'||RhythmRehab.IrisSceneTransition.IsBusy)throw new System.Exception("Navigation not settled");if(UnityEngine.Object.FindObjectsByType<RhythmRehab.IrisSceneTransition>().Length!=1)throw new System.Exception("Duplicate overlay");return true;');checks++;console.log('PASS '+name);}
function click(p,to){ev('UnityEngine.GameObject.Find('+JSON.stringify(p)+').GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return true;');if(to)scene(to);}
const file=path.resolve('Temp/Iris-Navigation-'+Date.now()+'.json');
try{
 click('/Canvas/BackButton','MainMenu');click('/MenuCanvas/MenuButtons/ProfileButton','PerformanceHistory');click('/Canvas/HomeButton','MainMenu');
 click('/MenuCanvas/MenuButtons/StartButton','WellbeingAssessment');click('/Canvas/SkipButton','SongSelection');click('/Canvas/ContinueButton','ModeSelection');click('/Canvas/TherapyCard','ModeSelection');click('/Canvas/ContinueButton','ExerciseSelection');click('/Canvas/BackButton','ModeSelection');
 click('/Canvas/FreeCard','ModeSelection');click('/Canvas/ContinueButton','DifficultySelection');click('/Canvas/Choice1');click('/Canvas/ContinueButton','Gameplay');
 ev('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().Restart();return true;');scene('Gameplay');
 ev('typeof(RhythmRehab.SessionHistoryStore).GetField("defaultStore",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,new RhythmRehab.SessionHistoryStore('+JSON.stringify(file)+'));UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().CompleteSession();return true;');scene('Results');
 click('/Canvas/ReplayButton','Gameplay');ev('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().Home();return true;');scene('MainMenu');
 console.log(checks+' actual menu, therapy/free branch, restart, results, replay and Home navigation checks passed.');
}finally{
 ev('typeof(RhythmRehab.SessionHistoryStore).GetField("defaultStore",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,null);RhythmRehab.SessionSummary.Last=null;return true;');
 if(fs.existsSync(file))fs.unlinkSync(file);if(fs.existsSync(file+'.tmp'))fs.unlinkSync(file+'.tmp');
}
