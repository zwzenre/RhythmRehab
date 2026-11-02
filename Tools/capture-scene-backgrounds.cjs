const {call}=require('./unity-authoring.cjs');
const fs=require('fs');
fs.mkdirSync('Screenshots/Backgrounds',{recursive:true});
const scenes=['MainMenu','SongSelection','ModeSelection','ExerciseSelection','DifficultySelection','Gameplay','Settings','Results','PerformanceHistory'];
for(const name of scenes){
 call('script','eval',{code:'UnityEngine.SceneManagement.SceneManager.LoadScene('+JSON.stringify(name)+');return true;'});
 if(name==='Gameplay')call('script','eval',{code:'var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();g.enabled=false;return true;'});
 const check=call('script','eval',{code:'var bg=UnityEngine.Object.FindAnyObjectByType<UnityEngine.Canvas>().transform.Find("Background");return new {scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,frame=UnityEngine.Time.frameCount,backdropCount=bg.GetComponentsInChildren<UnityEngine.UI.RawImage>().Length};'});
 console.log(check.result);
 call('capture','game_view',{width:1920,height:1080,save_path:'Temp/Background-'+name+'.png',source:'screen'});
 const source='Assets/Temp/Background-'+name+'.png';
 fs.renameSync(source,'Screenshots/Backgrounds/'+name+'.png');if(fs.existsSync(source+'.meta'))fs.unlinkSync(source+'.meta');
}
console.log(call('console','get',{level:'error',tail:5}));
