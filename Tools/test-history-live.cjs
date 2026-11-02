const {call}=require('./unity-authoring.cjs');
const fs=require('fs'),path=require('path');let checks=0;
function ev(code){return call('script','eval',{code}).result;}
function check(code,label){ev('if(!('+code+'))throw new System.Exception('+JSON.stringify(label)+');return true;');console.log('PASS '+label);checks++;}
function click(p){ev('UnityEngine.GameObject.Find('+JSON.stringify(p)+').GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return true;');}
function shot(name){call('capture','game_view',{width:1920,height:1080,save_path:'Temp/'+name+'.png',source:'screen'});fs.mkdirSync('Screenshots',{recursive:true});fs.renameSync('Assets/Temp/'+name+'.png','Screenshots/'+name+'.png');if(fs.existsSync('Assets/Temp/'+name+'.png.meta'))fs.unlinkSync('Assets/Temp/'+name+'.png.meta');}
const testFile=path.resolve('Temp/HistoryLiveChecks.json');
try {
 ev('var f=typeof(RhythmRehab.SessionHistoryStore).GetField("defaultStore",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);f.SetValue(null,new RhythmRehab.SessionHistoryStore('+JSON.stringify(testFile)+'));for(int i=0;i<13;i++){var s=new RhythmRehab.SessionSummary{songTitle="TEST SESSION "+(i+1),modeLabel=i%2==0?"THERAPY · LOW":"FREE · MEDIUM",bpm=120,totalNotes=20,perfect=10+i%8,miss=10-i%8,score=10000+i*700,duration=35,endedEarly=i==2,heightAttempts=20,correctHeightAttempts=10+i%8,endedUtc=System.DateTime.UtcNow.AddDays(-13+i).ToString("O")};if(!RhythmRehab.SessionHistoryStore.Default.TrySave(s,out var e))throw new System.Exception(e);}return true;');
 click('/MenuCanvas/MenuButtons/ProfileButton');
 check('UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="PerformanceHistory"','PROFILE opens history');
 check('UnityEngine.GameObject.Find("/Canvas/GraphPanel/Plot").transform.Cast<UnityEngine.Transform>().Count(t=>t.name.StartsWith("Point")&&t.gameObject.activeSelf)==8','Eight real record graph points');
 check('UnityEngine.GameObject.Find("/Canvas/SessionPanel/SessionScroll/Viewport/Content").transform.Cast<UnityEngine.Transform>().Count(t=>t.gameObject.activeSelf)==4','Four recent rows');
 shot('PerformanceHistory-TestData');
 click('/Canvas/GraphPanel/Metric1');
 check('UnityEngine.GameObject.Find("/Canvas/GraphPanel/GraphMessage").GetComponent<TMPro.TMP_Text>().text.Contains("not measured")','ROM honestly unavailable');
 click('/Canvas/GraphPanel/Metric2');
 check('UnityEngine.GameObject.Find("/Canvas/GraphPanel/Plot/Axis4").GetComponent<TMPro.TMP_Text>().text=="19000"','Score graph rescales');
 click('/Canvas/GraphPanel/Metric3');
 check('UnityEngine.GameObject.Find("/Canvas/GraphPanel/GraphRange").GetComponent<TMPro.TMP_Text>().text.Contains("Mouse")','Movement source identified');
 click('/Canvas/SessionPanel/ViewAllButton');
 check('UnityEngine.GameObject.Find("/Canvas/SessionPanel/SessionScroll/Viewport/Content").transform.Cast<UnityEngine.Transform>().Count(t=>t.gameObject.activeSelf)==10','View All fills ten saved rows');
 ev('UnityEngine.GameObject.Find("/Canvas/SessionPanel/SessionScroll").GetComponent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=0;return true;');
 check('UnityEngine.GameObject.Find("/Canvas/SessionPanel/SessionScroll").GetComponent<UnityEngine.UI.ScrollRect>().content.rect.height>525','List content scrolls beyond viewport');
 click('/Canvas/NextButton');
 check('UnityEngine.GameObject.Find("/Canvas/PageLabel").GetComponent<TMPro.TMP_Text>().text.Contains("2 / 2")','Next page reaches oldest sessions');
 check('UnityEngine.GameObject.Find("/Canvas/SessionPanel/SessionScroll/Viewport/Content").transform.Cast<UnityEngine.Transform>().Count(t=>t.gameObject.activeSelf)==3','Last page contains remaining three rows');
 click('/Canvas/PreviousButton');click('/Canvas/SessionPanel/ViewAllButton');click('/Canvas/HomeButton');
 ev('var song=UnityEditor.AssetDatabase.LoadAssetAtPath<RhythmRehab.SongCatalog>("Assets/Data/PlaceholderSongs.asset").songs[0];RhythmRehab.SessionSelection.SelectSong(song);RhythmRehab.SessionSelection.Confirm();UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");return true;');
 ev('var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();g.enabled=false;if(g.IsPaused)g.TogglePause();var field=typeof(RhythmRehab.PrototypeGameplay).GetField("chart",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);var chart=(RhythmRehab.ChartNote[])field.GetValue(g);g.TryHit(chart[0].beat*60/g.Summary.bpm,chart[0].TargetHeight);g.TryHit(chart[1].beat*60/g.Summary.bpm,chart[1].lane==0?1:0);if(g.Summary.heightAttempts!=2||g.Summary.correctHeightAttempts!=1)throw new System.Exception("Height attempts incorrect");g.CompleteSession();return true;');
 check('RhythmRehab.SessionHistoryStore.Default.Load(out var e).Count==14&&RhythmRehab.SessionHistoryStore.Default.Load(out e)[0].endedEarly','End-session persists early result');
 click('/Canvas/ReplayButton');
 ev('var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();g.enabled=false;if(g.IsPaused)g.TogglePause();var chart=(RhythmRehab.ChartNote[])GameplayEditorAccess.Read(g,"chart");foreach(var n in chart)g.TryHit(n.beat*60/g.Summary.bpm,n.TargetHeight);g.CompleteSession();return true;');
 check('RhythmRehab.SessionHistoryStore.Default.Load(out var e).Count==15&&!RhythmRehab.SessionHistoryStore.Default.Load(out e)[0].endedEarly','Completed session persists separately');
} finally {
 ev('typeof(RhythmRehab.SessionHistoryStore).GetField("defaultStore",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,null);RhythmRehab.SessionSummary.Last=null;UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");return true;');
 if(fs.existsSync(testFile))fs.unlinkSync(testFile);
 if(fs.existsSync(testFile+'.tmp'))fs.unlinkSync(testFile+'.tmp');
}
click('/MenuCanvas/MenuButtons/ProfileButton');shot('PerformanceHistory');
click('/Canvas/HomeButton');
console.log(checks+' live history checks passed. Test records isolated; real history restored.');

