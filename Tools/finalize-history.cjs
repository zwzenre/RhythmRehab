const {call}=require('./unity-authoring.cjs');
console.log(call('scene','open',{path:'Assets/Scenes/PerformanceHistory.unity'}));
console.log(call('script','eval',{code:`
var p=UnityEngine.Object.FindAnyObjectByType<RhythmRehab.PerformanceHistoryPresenter>();
var so=new UnityEditor.SerializedObject(p);var it=so.GetIterator();int refs=0;
while(it.NextVisible(true))if(it.propertyType==UnityEditor.SerializedPropertyType.ObjectReference){
 if(it.objectReferenceValue==null)throw new System.Exception("Missing reference: "+it.propertyPath);refs++;
}
var content=UnityEngine.GameObject.Find("/Canvas/SessionPanel/SessionScroll/Viewport/Content").transform;
for(int i=0;i<content.childCount;i++)content.GetChild(i).gameObject.SetActive(false);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
if(UnityEditor.EditorBuildSettings.scenes.Count(s=>s.enabled&&s.path.EndsWith("PerformanceHistory.unity"))!=1)throw new System.Exception("History build entry missing");
return refs+" assigned scene references checked; history scene saved.";`}));
console.log(call('console','get',{level:'error',tail:3}));
