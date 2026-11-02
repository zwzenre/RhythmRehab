const {call}=require('./unity-authoring.cjs');const cp=require('child_process');
const ev=code=>call('script','eval',{code}).result;
const original=ev('return System.Convert.ToBase64String(System.IO.File.ReadAllBytes(RhythmRehab.LocalSettingsStore.Default.FilePath));');
try{
 console.log(JSON.stringify(ev('var slider=UnityEngine.GameObject.Find("/Canvas/TimingControls/HitSoundTiming").GetComponent<UnityEngine.UI.Slider>();slider.value=-100;var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>();c.Save();c.TestHitSound();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var clip=(UnityEngine.AudioClip)typeof(RhythmRehab.RhythmCalibrationController).GetField("previewClip",flags).GetValue(c);var source=(UnityEngine.AudioSource)typeof(RhythmRehab.RhythmCalibrationController).GetField("soundPreview",flags).GetValue(c);if(RhythmRehab.LocalSettingsStore.Default.Load(out var error).hitSoundOffsetMs!=-100||error!=null)throw new System.Exception("Slider not saved");if(System.Math.Abs(clip.length-(source.clip.length-.1f))>.001f)throw new System.Exception("Preview did not trim lead-in");return new {savedOffset=-100,previewSeconds=clip.length,originalSeconds=source.clip.length};')));
 console.log(JSON.stringify(call('capture','game_view',{width:1920,height:1080,save_path:'Temp/Hit-Sound-Timing-Calibration.png',source:'screen'})));
 ev('UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");return true;');
 console.log(JSON.stringify(ev('var game=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();game.enabled=false;var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var clip=(UnityEngine.AudioClip)GameplayEditorAccess.Read(game,"adjustedHitClip");var source=(UnityEngine.AudioSource)GameplayEditorAccess.Read(game,"hitSource");if(!clip||System.Math.Abs(clip.length-(source.clip.length-.1f))>.001f)throw new System.Exception("Gameplay timing not applied");return "Saved -100 ms applied to gameplay hit sound";')));
}finally{
 call('editor.playmode','stop');
 cp.execFileSync('C:/Program Files/Unity Hub/resources/unity.exe',['status','--until-ready','--timeout','60','--format','json'],{encoding:'utf8'});
 ev(`System.IO.File.WriteAllBytes(RhythmRehab.LocalSettingsStore.Default.FilePath,System.Convert.FromBase64String("${original}"));return true;`);
 call('editor.playmode','set_run_in_background',{enable:false});
 call('scene','open',{path:'Assets/Scenes/RhythmCalibration.unity'});
}
console.log(JSON.stringify(call('console','get',{level:'error',tail:4})));
