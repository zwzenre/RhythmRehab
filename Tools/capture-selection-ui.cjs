const {call}=require('./unity-authoring.cjs');
function evaluate(code){return call('script','eval',{code}).result;}
function click(path){return evaluate('UnityEngine.GameObject.Find('+JSON.stringify(path)+').GetComponent<UnityEngine.UI.Button>().onClick.Invoke();return true;');}
function capture(name){const r=call('capture','game_view',{width:1920,height:1080,save_path:'Temp/'+name+'.png',source:'screen'});console.log(r.savedPath);}
click('/MenuCanvas/MenuButtons/StartButton');
evaluate('var input=UnityEngine.GameObject.Find("/Canvas/Search").GetComponent<TMPro.TMP_InputField>();input.text="Morning";if(UnityEngine.GameObject.Find("/Canvas/SongScroll/Viewport/Content").transform.Cast<UnityEngine.Transform>().Count(t=>t.gameObject.activeSelf)!=1)throw new System.Exception("Search input callback failed");input.text="";return "Search input callback verified";');
evaluate('UnityEngine.Object.FindAnyObjectByType<RhythmRehab.SongSelectionController>().SelectSong(0);return true;');
capture('SongSelection');click('/Canvas/ContinueButton');capture('ModeSelection');
click('/Canvas/TherapyCard');capture('ExerciseSelection');click('/Canvas/BackButton');
click('/Canvas/FreeCard');capture('DifficultySelection');click('/Canvas/HomeButton');
