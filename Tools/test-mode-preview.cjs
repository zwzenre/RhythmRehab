const {call}=require('./unity-authoring.cjs');
let checks=0;
function ev(code){return call('script','eval',{code}).result;}
function check(code,label){ev(`if(!(${code}))throw new System.Exception(${JSON.stringify(label)});return true;`);checks++;console.log('PASS '+label);}
function click(path){ev(`UnityEngine.GameObject.Find(${JSON.stringify(path)}).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return true;`);}
function ready(){for(let i=0;i<10;i++){if(ev('var m=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeSelectionPreview>();return !RhythmRehab.IrisSceneTransition.IsBusy&&(!m||!m.IsTransitioning);'))return;}throw Error('Transition did not settle');}
function scene(name){ready();check(`UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==${JSON.stringify(name)}`,'Screen is '+name);}
scene('ModeSelection');
check('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeSelectionPreview>().IsOpen','Therapy preview is open');
const before=ev('var d=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeGameplayDemo>();return new {time=d.DemoTime,frame=UnityEngine.Time.frameCount};');
const after=ev('var d=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeGameplayDemo>();return new {time=d.DemoTime,frame=UnityEngine.Time.frameCount};');
if(after.time<=before.time||after.frame<=before.frame)throw Error('Demo not advancing');checks++;
check('!RhythmRehab.SessionSelection.Configured','Preview does not start a session');
click('/Canvas/ModeGameplayPreview/BackToModes');ready();
check('!UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeSelectionPreview>().IsOpen&&UnityEngine.GameObject.Find("/Canvas/FreeCard").GetComponent<UnityEngine.CanvasGroup>().alpha==1','Back to modes restores other card');
click('/Canvas/FreeCard');ready();
check('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeGameplayDemo>().Mode==RhythmRehab.SessionMode.Free','Free selection displays Free demo');
check('UnityEngine.GameObject.Find("/Canvas/TherapyCard").GetComponent<UnityEngine.CanvasGroup>().alpha==0&&!UnityEngine.GameObject.Find("/Canvas/TherapyCard").GetComponent<UnityEngine.CanvasGroup>().interactable','Free demo replaces and blocks Therapy card');
check('UnityEngine.GameObject.Find("/Canvas/ModeGameplayPreview").GetComponent<UnityEngine.RectTransform>().anchoredPosition.x<0','Free preview appears on the left');
check('((float[])GameplayEditorAccess.Read(UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeGameplayDemo>(),"freeHeights")).Distinct().Count()>3','Free demo includes intermediate heights');
call('capture','game_view',{width:1920,height:1080,save_path:'Temp/ModePreview-Free.png',source:'screen'});
click('/Canvas/BackButton');ready();scene('ModeSelection');
check('!UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeSelectionPreview>().IsOpen','Page Back closes preview first');
click('/Canvas/FreeCard');ready();click('/Canvas/ContinueButton');scene('DifficultySelection');
click('/Canvas/BackButton');scene('ModeSelection');
check('!UnityEngine.Object.FindAnyObjectByType<RhythmRehab.ModeSelectionPreview>().IsOpen','Returning starts with both cards');
click('/Canvas/TherapyCard');ready();click('/Canvas/ContinueButton');scene('ExerciseSelection');
click('/Canvas/BackButton');scene('ModeSelection');
click('/Canvas/BackButton');scene('SongSelection');
console.log(checks+' mode-preview navigation and live-animation checks passed. No sessions or settings saved.');
