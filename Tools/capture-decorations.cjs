const {call}=require('./unity-authoring.cjs');const fs=require('fs');
fs.mkdirSync('Screenshots/Decorations',{recursive:true});
for(const name of ['MainMenu','SongSelection','Gameplay','PerformanceHistory']){
 call('script','eval',{code:'UnityEngine.SceneManagement.SceneManager.LoadScene('+JSON.stringify(name)+');return true;'});
 if(name==='Gameplay')call('script','eval',{code:'UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().enabled=false;return true;'});
 call('capture','game_view',{width:1920,height:1080,save_path:'Temp/Decorated-'+name+'.png',source:'screen'});
 const p='Assets/Temp/Decorated-'+name+'.png';fs.renameSync(p,'Screenshots/Decorations/'+name+'.png');if(fs.existsSync(p+'.meta'))fs.unlinkSync(p+'.meta');
 console.log('Captured '+name);
}
console.log(call('console','get',{level:'error',tail:4}));
