const {call}=require('./unity-authoring.cjs');
const ev=code=>call('script','eval',{code}).result;
function settle(){for(let i=0;i<15;i++){if(!ev('return RhythmRehab.IrisSceneTransition.IsBusy;'))return;}throw Error('Transition still busy');}
function check(code,message){if(!ev(code))throw Error(message);console.log('PASS '+message);}
function click(path){settle();ev(`UnityEngine.GameObject.Find("${path}").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return true;`);settle();}
click('/Canvas/FreeCard');
check('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="ModeSelection"&&RhythmRehab.SessionSelection.Mode==RhythmRehab.SessionMode.Free;','Free card selects without navigating');
check('return UnityEngine.GameObject.Find("/Canvas/FreeCard/SelectedBadge")!=null&&UnityEngine.GameObject.Find("/Canvas/FreeCard").GetComponent<UnityEngine.UI.Outline>().enabled&&!UnityEngine.GameObject.Find("/Canvas/TherapyCard").GetComponent<UnityEngine.UI.Outline>().enabled;','Selected badge and outline update');
click('/Canvas/ContinueButton');
check('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="DifficultySelection";','Continue routes Free to Difficulty Selection');
click('/Canvas/BackButton');click('/Canvas/TherapyCard');
check('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="ModeSelection"&&RhythmRehab.SessionSelection.Mode==RhythmRehab.SessionMode.Therapy;','Therapy card selects without navigating');
console.log(JSON.stringify(call('capture','game_view',{width:1920,height:1080,save_path:'Temp/Mode-Confirm.png',source:'screen'})));
click('/Canvas/ContinueButton');
check('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="ExerciseSelection";','Continue routes Therapy to Exercise Selection');
click('/Canvas/BackButton');
console.log(JSON.stringify(call('console','get',{level:'error',tail:1})));
