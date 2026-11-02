const {call}=require('./unity-authoring.cjs');
let checks=0;
function evaluate(code){const r=call('script','eval',{code});return r.result;}
function check(code,label){evaluate('if(!('+code+'))throw new System.Exception('+JSON.stringify(label)+'); return true;');console.log('PASS '+label);checks++;}
function click(path){evaluate('UnityEngine.GameObject.Find('+JSON.stringify(path)+').GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;');}
function scene(name){check('UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=='+JSON.stringify(name),name+' navigation');}
function profile(count,lane,bpm,scale){
 check('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().Summary.totalNotes=='+count,'Configured note count '+count);
 check('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().Summary.bpm=='+bpm,'Selected BPM '+bpm);
 check('System.Math.Abs((double)GameplayEditorAccess.Read(UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>(),"timingScale")-'+scale+')<0.0001','Configured timing window '+scale);
 const expression='((RhythmRehab.ChartNote[])GameplayEditorAccess.Read(UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>(),"chart"))';
 check(lane>=0?expression+'.All(n=>n.lane=='+lane+')':expression+'.Select(n=>n.lane).Distinct().Count()>1',lane>=0?'All notes remain at chosen therapy height':'Free chart varies height');
 evaluate('var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();if(!g.IsPaused)g.TogglePause();return true;');
}
click('/MenuCanvas/MenuButtons/StartButton');scene('WellbeingAssessment');click('/Canvas/SkipButton');scene('SongSelection');
check('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.SongSelectionController>()!=null','Song browser initialized');
evaluate('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.SongSelectionController>();c.Next();return true;');
check('UnityEngine.GameObject.Find("/Canvas/SongScroll").GetComponent<UnityEngine.UI.ScrollRect>().horizontalNormalizedPosition>0.4f','Horizontal browse arrow');
evaluate('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.SongSelectionController>().Filter("no-such-song-xyz");return true;');
check('UnityEngine.GameObject.Find("/Canvas/SongScroll/EmptyMessage")!=null','Search empty state');
evaluate('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.SongSelectionController>().Filter("Neon");return true;');
check('UnityEngine.GameObject.Find("/Canvas/SongScroll/Viewport/Content").transform.Cast<UnityEngine.Transform>().Count(t=>t.gameObject.activeSelf)==1','Song search filters cards');
evaluate('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.SongSelectionController>();c.Filter("");c.CycleFilter();return true;');
check('UnityEngine.GameObject.Find("/Canvas/SongScroll/Viewport/Content").transform.Cast<UnityEngine.Transform>().Count(t=>t.gameObject.activeSelf)==4','Calm tempo filter');
evaluate('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.SongSelectionController>();c.CycleFilter();c.CycleFilter();c.SelectSong(7);return true;');
click('/Canvas/ContinueButton');scene('ModeSelection');click('/Canvas/TherapyCard');scene('ModeSelection');click('/Canvas/ContinueButton');scene('ExerciseSelection');
click('/Canvas/Choice2');click('/Canvas/ContinueButton');scene('Gameplay');profile(32,2,132,1.25);
evaluate('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().CompleteSession();return true;');scene('Results');
check('RhythmRehab.SessionSummary.Last.songTitle=="Bright Horizon"&&RhythmRehab.SessionSummary.Last.modeLabel.Contains("HIGH")','Results preserve session choices');
click('/Canvas/ReplayButton');scene('Gameplay');profile(32,2,132,1.25);
evaluate('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().Home();return true;');
for(const [index,count,scale] of [[0,16,1.3],[1,24,1],[2,32,.8]]){
 click('/MenuCanvas/MenuButtons/StartButton');scene('WellbeingAssessment');click('/Canvas/SkipButton');scene('SongSelection');click('/Canvas/ContinueButton');click('/Canvas/FreeCard');scene('ModeSelection');click('/Canvas/ContinueButton');scene('DifficultySelection');
 click('/Canvas/Choice'+index);click('/Canvas/ContinueButton');scene('Gameplay');profile(count,-1,132,scale);
 evaluate('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>().Home();return true;');
}
console.log(checks+' live selection, filter, scroll, therapy/free gameplay and replay checks passed');
