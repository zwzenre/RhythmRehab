const {call}=require('./unity-authoring.cjs');const fs=require('fs');
for(const scene of ['ModeSelection','ExerciseSelection','DifficultySelection']){
 call('script','eval',{code:'UnityEngine.SceneManagement.SceneManager.LoadScene('+JSON.stringify(scene)+');return true;'});
 const check=call('script','eval',{code:'UnityEngine.Canvas.ForceUpdateCanvases();var labels=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Where(t=>t.transform.parent.name=="HeightFeature"||t.transform.parent.name.EndsWith("Feature"));foreach(var t in labels)t.ForceMeshUpdate();var truncated=labels.Where(t=>t.isTextTruncated).Select(t=>t.text).ToArray();if(truncated.Length>0)throw new System.Exception("Truncated descriptions: "+string.Join(" / ",truncated));return "Description text fits";'});
 console.log(scene+': '+check.result);
 call('capture','game_view',{width:1920,height:1080,save_path:'Temp/Designed-'+scene+'.png',source:'screen'});
 const p='Assets/Temp/Designed-'+scene+'.png';fs.renameSync(p,'Screenshots/Designed-'+scene+'.png');if(fs.existsSync(p+'.meta'))fs.unlinkSync(p+'.meta');
}
