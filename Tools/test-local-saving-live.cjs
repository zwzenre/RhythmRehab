const cp=require('child_process');
const {call}=require('./unity-authoring.cjs');
const ev=code=>call('script','eval',{code}).result;
const ready=()=>cp.execFileSync('C:/Program Files/Unity Hub/resources/unity.exe',['status','--until-ready','--timeout','60','--format','json'],{encoding:'utf8'});
const check=(code,label)=>{if(!ev(code))throw Error(label);console.log('PASS '+label);};
if(ev('return UnityEditor.EditorApplication.isPlaying;'))throw Error('Stop Play mode before running this test.');
const originalScene=ev('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;');
const oldBackground=ev('return UnityEngine.Application.runInBackground;');
// Initialize/migrate once, then preserve the exact user settings for the test.
const original=ev('var s=RhythmRehab.LocalSettingsStore.Default; s.Load(out var error); if(error!=null)throw new System.Exception(error); return System.Convert.ToBase64String(System.IO.File.ReadAllBytes(s.FilePath));');
try{
 call('scene','open',{path:'Assets/Scenes/Settings.unity'});
 call('editor','set_auto_tick',{enable:true,interval_ms:16,persist:false});
 call('editor.playmode','set_run_in_background',{enable:true});
 call('editor.playmode','start');ready();
 const frame=ev('return UnityEngine.Time.frameCount;');
 check(`return UnityEngine.Time.frameCount>${frame};`,'Player frames advance');
 ev('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeSettings>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic; ((UnityEngine.UI.Slider)typeof(RhythmRehab.PrototypeSettings).GetField("beatVolume",flags).GetValue(c)).value=.31f; ((UnityEngine.UI.Slider)typeof(RhythmRehab.PrototypeSettings).GetField("sfxVolume",flags).GetValue(c)).value=.62f;return true;');
 check('var s=RhythmRehab.LocalSettingsStore.Default.Load(out var error);return error==null&&s.beatVolume==.31f&&s.sfxVolume==.62f;','Actual audio sliders save JSON');
 ev('UnityEngine.SceneManagement.SceneManager.LoadScene("RhythmCalibration");return true;');
 ev('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;((UnityEngine.UI.Slider)typeof(RhythmRehab.RhythmCalibrationController).GetField("offsetSlider",f).GetValue(c)).value=85;((UnityEngine.UI.Slider)typeof(RhythmRehab.RhythmCalibrationController).GetField("speedSlider",f).GetValue(c)).value=1.25f;c.Save();return true;');
 check('var s=RhythmRehab.LocalSettingsStore.Default.Load(out var error);return error==null&&s.judgementOffsetMs==85&&s.noteSpeed==1.25f&&s.beatVolume==.31f;','Calibration Save persists rhythm and preserves audio');
 call('editor.playmode','stop');ready();
 call('scene','open',{path:'Assets/Scenes/RhythmCalibration.unity'});
 call('editor.playmode','start');ready();
 check('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>();return c.OffsetMs==85&&c.NoteSpeed==1.25f;','Rhythm values reload after Play mode restart');
 ev('UnityEngine.SceneManagement.SceneManager.LoadScene("Settings");return true;');
 check('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeSettings>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;return ((UnityEngine.UI.Slider)typeof(RhythmRehab.PrototypeSettings).GetField("beatVolume",f).GetValue(c)).value==.31f&&((UnityEngine.UI.Slider)typeof(RhythmRehab.PrototypeSettings).GetField("sfxVolume",f).GetValue(c)).value==.62f;','Audio sliders reload after restart');
} finally {
 if(ev('return UnityEditor.EditorApplication.isPlaying;')){call('editor.playmode','stop');ready();}
 ev(`System.IO.File.WriteAllBytes(RhythmRehab.LocalSettingsStore.Default.FilePath,System.Convert.FromBase64String("${original}"));return true;`);
 call('editor.playmode','set_run_in_background',{enable:oldBackground});
 call('scene','open',{path:originalScene});
}
console.log(JSON.stringify(call('console','get',{level:'error',tail:5})));
