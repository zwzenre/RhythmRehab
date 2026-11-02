const {call}=require('./unity-authoring.cjs');const fs=require('fs');
const ev=code=>call('script','eval',{code}).result;
function check(code,label){if(!ev(code))throw Error(label);console.log('PASS '+label);}
function settle(){for(let i=0;i<15;i++){if(!ev('return RhythmRehab.IrisSceneTransition.IsBusy;'))return;}throw Error('Transition busy');}
function click(path){settle();ev(`UnityEngine.GameObject.Find("${path}").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return true;`);settle();}
function values(pain,stress){ev(`UnityEngine.GameObject.Find("/Canvas/PainCard/Slider").GetComponent<UnityEngine.UI.Slider>().value=${pain};UnityEngine.GameObject.Find("/Canvas/StressCard/Slider").GetComponent<UnityEngine.UI.Slider>().value=${stress};return true;`);}
function assessment(){ev('UnityEngine.SceneManagement.SceneManager.LoadScene("WellbeingAssessment");return true;');settle();}
function shot(name){call('capture','game_view',{width:1920,height:1080,save_path:'Temp/'+name+'.png',source:'screen'});fs.renameSync('Assets/Temp/'+name+'.png','Screenshots/'+name+'.png');if(fs.existsSync('Assets/Temp/'+name+'.png.meta'))fs.unlinkSync('Assets/Temp/'+name+'.png.meta');}
click('/MenuCanvas/MenuButtons/StartButton');
check('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="WellbeingAssessment";','Main Menu Start opens assessment');
shot('Wellbeing-Assessment');
values(7,7);click('/Canvas/ContinueButton');
check('var w=RhythmRehab.SessionWellbeing.Snapshot;return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SongSelection"&&w.pain==7&&w.stress==7&&!w.reducedPerformanceFeedback;','Below-threshold ratings continue normally');
assessment();values(8,0);click('/Canvas/ContinueButton');
check('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.WellbeingAssessmentController>();return c.MessageOpen&&UnityEngine.GameObject.Find("/Canvas/MessageOverlay/Dialog/Title").GetComponent<TMPro.TMP_Text>().text.Contains("HIGH PAIN");','Pain 8 opens pain message');shot('Wellbeing-High-Pain');
click('/Canvas/MessageOverlay/Dialog/ReviewButton');
check('var c=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.WellbeingAssessmentController>();return !c.MessageOpen&&c.Pain==8;','Review keeps draft rating and stays on assessment');
values(8,9);click('/Canvas/ContinueButton');click('/Canvas/MessageOverlay/Dialog/AcknowledgeButton');
check('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="WellbeingAssessment"&&UnityEngine.GameObject.Find("/Canvas/MessageOverlay/Dialog/Title").GetComponent<TMPro.TMP_Text>().text.Contains("HIGH STRESS");','Both high ratings show pain then stress messages');shot('Wellbeing-High-Stress');
click('/Canvas/MessageOverlay/Dialog/AcknowledgeButton');
check('var w=RhythmRehab.SessionWellbeing.Snapshot;return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SongSelection"&&w.highPain&&w.highStress&&w.pain==8&&w.stress==9;','Acknowledging both commits both responses');
click('/Canvas/ContinueButton');click('/Canvas/TherapyCard');click('/Canvas/ContinueButton');click('/Canvas/ContinueButton');
check('var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();g.enabled=false;return g.Summary.wellbeing.pain==8&&g.Summary.wellbeing.stress==9;','Assessment reaches gameplay session snapshot');
check('return UnityEngine.GameObject.Find("/Canvas/ScoreCard")==null&&UnityEngine.GameObject.Find("/Canvas/AccuracyCard")==null&&UnityEngine.GameObject.Find("/Canvas/Combo")==null&&UnityEngine.GameObject.Find("/Canvas/ProgressTrack")==null&&UnityEngine.GameObject.Find("/Canvas/RemainingTime")==null;','High ratings hide score, accuracy, combo, progress and timer');
check('return UnityEngine.GameObject.Find("/Canvas/BottomBar/HoldingTip").GetComponent<TMPro.TMP_Text>().text.Contains("discomfort");','Supportive gameplay message displayed');shot('Wellbeing-Reduced-Pressure-Gameplay');
assessment();values(10,10);click('/Canvas/SkipButton');
check('var w=RhythmRehab.SessionWellbeing.Snapshot;return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="SongSelection"&&w.skipped&&w.pain==-1&&!w.reducedPerformanceFeedback;','Skip records no ratings and restores normal feedback');
ev('UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");return true;');settle();
check('var g=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PrototypeGameplay>();g.enabled=false;return UnityEngine.GameObject.Find("/Canvas/ScoreCard")!=null&&UnityEngine.GameObject.Find("/Canvas/ProgressTrack")!=null&&g.Summary.wellbeing.skipped;','Skipped assessment leaves normal gameplay HUD visible');
ev('UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");return true;');settle();click('/MenuCanvas/MenuButtons/StartButton');
check('return RhythmRehab.SessionWellbeing.Snapshot==null;','New Start clears prior session response');
click('/Canvas/BackButton');check('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu";','Assessment Back returns to main menu');
console.log(JSON.stringify(call('console','get',{level:'error',tail:4})));
