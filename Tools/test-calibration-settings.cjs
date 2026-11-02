const {call}=require('./unity-authoring.cjs');const fs=require('fs');
let checks=0;const ev=code=>call('script','eval',{code}).result;
function check(code,label){ev('if(!('+code+'))throw new System.Exception('+JSON.stringify(label)+');return true;');console.log('PASS '+label);checks++;}
function settle(){for(let i=0;i<12;i++){if(!ev('return RhythmRehab.IrisSceneTransition.IsBusy;'))return;Atomics.wait(new Int32Array(new SharedArrayBuffer(4)),0,0,100);}throw new Error('Transition did not settle');}
function click(p){settle();ev('UnityEngine.GameObject.Find('+JSON.stringify(p)+').GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return true;');settle();}
function shot(name){call('capture','game_view',{width:1920,height:1080,save_path:'Temp/'+name+'.png',source:'screen'});fs.renameSync('Assets/Temp/'+name+'.png','Screenshots/'+name+'.png');if(fs.existsSync('Assets/Temp/'+name+'.png.meta'))fs.unlinkSync('Assets/Temp/'+name+'.png.meta');}
const savedSettings=ev('var store=RhythmRehab.LocalSettingsStore.Default;store.Load(out var error);if(error!=null)throw new System.Exception(error);return System.Convert.ToBase64String(System.IO.File.ReadAllBytes(store.FilePath));');
const old=ev('return new {hasOffset=UnityEngine.PlayerPrefs.HasKey(RhythmRehab.RhythmPreferences.OffsetKey),offset=RhythmRehab.RhythmPreferences.OffsetMs,hasSpeed=UnityEngine.PlayerPrefs.HasKey(RhythmRehab.RhythmPreferences.SpeedKey),speed=RhythmRehab.RhythmPreferences.Speed};');
try{
 shot('Settings-Hub');click('/Canvas/RhythmRow');
 check('UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="RhythmCalibration"','Settings opens rhythm calibration');
 ev('var a=UnityEngine.GameObject.Find("/Canvas/TimingControls/OffsetSlider").GetComponent<UnityEngine.UI.Slider>();var b=UnityEngine.GameObject.Find("/Canvas/TimingControls/SpeedSlider").GetComponent<UnityEngine.UI.Slider>();a.value=120;b.value=1.5f;return true;');
 check('UnityEngine.GameObject.Find("/Canvas/TimingControls/OffsetValue").GetComponent<TMPro.TMP_Text>().text.Contains("120")','Offset slider label updates');
 click('/Canvas/PracticeInfo/StartButton');
 check('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>().IsPlaying','Preview starts');
 ev('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>();c.StopPreview();c.TogglePreview();if(!c.TryHit(2.12,0)||!UnityEngine.GameObject.Find("/Canvas/PreviewPanel/Feedback").GetComponent<TMPro.TMP_Text>().text.StartsWith("PERFECT"))throw new System.Exception("Offset preview hit failed");return true;');
 console.log('PASS Preview judges offset timing');checks++;
 ev('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>();c.StopPreview();c.TogglePreview();c.TryHit(3.12,1);if(UnityEngine.GameObject.Find("/Canvas/PreviewPanel/Feedback").GetComponent<TMPro.TMP_Text>().text!="WRONG HEIGHT")throw new System.Exception("Preview ignored height");return true;');
 console.log('PASS Preview requires correct height');checks++;
 click('/Canvas/SaveButton');
 check('RhythmRehab.RhythmPreferences.OffsetMs==120&&RhythmRehab.RhythmPreferences.Speed==1.5f','Save persists both settings');
 shot('Rhythm-Calibration');click('/Canvas/ResetButton');
 check('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>().OffsetMs==0&&UnityEngine.Object.FindAnyObjectByType<RhythmRehab.RhythmCalibrationController>().NoteSpeed==1','Reset updates draft values');
 check('RhythmRehab.RhythmPreferences.OffsetMs==120','Reset does not silently overwrite saved values');
 click('/Canvas/BackButton');check('UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Settings"','Back returns to Settings');
 click('/Canvas/ConnectionRow');check('UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SensorConnection"','Connection page navigation');
 check('!UnityEngine.GameObject.Find("/Canvas/ConnectionStatus/ConnectButton").GetComponent<UnityEngine.UI.Button>().interactable','Hardware connection is disabled');
 click('/Canvas/AvailableDevices/ScanButton');check('UnityEngine.GameObject.Find("/Canvas/AvailableDevices/FrameworkStatus").GetComponent<TMPro.TMP_Text>().text.Contains("not implemented")','Scan honestly reports framework state');shot('Sensor-Connection');
 click('/Canvas/AboutSensor/CalibrationButton');check('UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SensorCalibration"','Sensor calibration navigation');
 check('!UnityEngine.GameObject.Find("/Canvas/CalibrationStatus/CalibrateButton").GetComponent<UnityEngine.UI.Button>().interactable&&!UnityEngine.GameObject.Find("/Canvas/NextButton").GetComponent<UnityEngine.UI.Button>().interactable','No fabricated sensor calibration');shot('Sensor-Calibration');
 ev('var song=UnityEditor.AssetDatabase.LoadAssetAtPath<RhythmRehab.SongCatalog>("Assets/Data/PlaceholderSongs.asset").songs[0];RhythmRehab.SessionSelection.SelectSong(song);RhythmRehab.SessionSelection.SelectMode(RhythmRehab.SessionMode.Free);RhythmRehab.SessionSelection.SelectDifficulty(RhythmRehab.FreeDifficulty.Medium);RhythmRehab.SessionSelection.Confirm();RhythmRehab.IrisSceneTransition.Load("Gameplay");return true;');
 ev('var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();g.enabled=false;if(g.IsPaused)g.TogglePause();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;float offset=(float)GameplayEditorAccess.Read(g,"judgementOffsetSeconds");float travel=(float)GameplayEditorAccess.Read(g,"approachSeconds");if(System.Math.Abs(offset-.12f)>.0001||System.Math.Abs(travel-2)>.0001)throw new System.Exception("Saved settings not applied to gameplay");var chart=(RhythmRehab.ChartNote[])GameplayEditorAccess.Read(g,"chart");var resolved=(bool[])GameplayEditorAccess.Read(g,"resolved");System.Array.Clear(resolved,0,resolved.Length);int before=g.Summary.perfect;if(!g.TryHit(chart[0].beat*60.0/g.Summary.bpm+.12,chart[0].TargetHeight)||g.Summary.perfect!=before+1)throw new System.Exception("Gameplay offset hit failed");return true;');
 console.log('PASS Saved settings affect gameplay travel and hit timing');checks++;
}finally{
 ev('System.IO.File.WriteAllBytes(RhythmRehab.LocalSettingsStore.Default.FilePath,System.Convert.FromBase64String("'+savedSettings+'"));return true;');
 ev((old.hasOffset?'UnityEngine.PlayerPrefs.SetFloat(RhythmRehab.RhythmPreferences.OffsetKey,'+old.offset+'f);':'UnityEngine.PlayerPrefs.DeleteKey(RhythmRehab.RhythmPreferences.OffsetKey);')+(old.hasSpeed?'UnityEngine.PlayerPrefs.SetFloat(RhythmRehab.RhythmPreferences.SpeedKey,'+old.speed+'f);':'UnityEngine.PlayerPrefs.DeleteKey(RhythmRehab.RhythmPreferences.SpeedKey);')+'UnityEngine.PlayerPrefs.Save();var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();if(g!=null)g.Home();else RhythmRehab.IrisSceneTransition.Load("Settings");return true;');
}
console.log(checks+' live settings checks passed. Original preferences restored; no test sessions saved.');
