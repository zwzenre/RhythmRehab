const fs=require('fs');const {call}=require('./unity-authoring.cjs');
const mode=process.argv[2],file=process.argv[3];
if(mode==='eval')console.log(call('script','eval',{code:fs.readFileSync(file,'utf8')}));
else if(mode==='run')console.log(call('script','run',{file,entry:process.argv[4]}));
else if(mode==='refresh')console.log(call('editor','invoke_menu',{path:'Assets/Refresh'}));
else throw Error('Use eval <file>, run <file> <Class.Method>, or refresh.');
