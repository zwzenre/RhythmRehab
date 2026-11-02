const {call}=require('./unity-authoring.cjs');
for (const name of ['VerifyLocalSettings','VerifySessionHistory','VerifyRhythmPreferences'])
 console.log(JSON.stringify(call('script','run',{file:`Tools/${name}.cs`,entry:`${name}.Main`})));
console.log(JSON.stringify(call('script','eval',{code:'return new { path=UnityEngine.Application.persistentDataPath, scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, playing=UnityEditor.EditorApplication.isPlaying };'})));
console.log(JSON.stringify(call('console','get',{level:'error',tail:5})));
