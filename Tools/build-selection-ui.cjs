const {call}=require('./unity-authoring.cjs');
let ops=[];
const kit='Assets/Assets/Space_Exploration_GUI_Kit/';
const white=[0.94,0.95,1,1],muted=[0.57,0.61,0.75,1],purple=[0.63,0.3,1,1],cyan=[0.12,0.85,1,1];
const add=(command,params)=>ops.push({command,params});
const comp=(target,type)=>add('component add',{target,type});
const set=(target,type,properties)=>add('component set_properties',{target,type,properties});
function go(name,parent='/Canvas',x=0,y=0,w=1920,h=1080){const p=parent?parent+'/'+name:'/'+name;add('gameobject create',{name,...(parent?{parent}:{})});comp(p,'UnityEngine.RectTransform');set(p,'UnityEngine.RectTransform',{m_AnchorMin:[0.5,0.5],m_AnchorMax:[0.5,0.5],m_Pivot:[0.5,0.5],m_AnchoredPosition:[x,y],m_SizeDelta:[w,h],m_LocalScale:[1,1,1]});add('gameobject set_layer',{target:p,layer:'UI'});return p;}
function panel(name,parent,x,y,w,h,color=[0.035,0.04,0.095,1]){const p=go(name,parent,x,y,w,h);comp(p,'UnityEngine.UI.Image');set(p,'UnityEngine.UI.Image',{m_Color:color,m_RaycastTarget:false});return p;}
function text(name,parent,x,y,w,h,value,size=32,color=white,align=2){const p=go(name,parent,x,y,w,h);comp(p,'TMPro.TextMeshProUGUI');set(p,'TMPro.TextMeshProUGUI',{m_text:value,m_fontAsset:kit+'Fonts/Asap-Bold SDF.asset',m_sharedMaterial:{guid:'b633915a7eab00342a258e5c2f50a75f',fileId:'-5571592025542048100'},m_fontSize:size,m_fontSizeBase:size,m_enableAutoSizing:false,m_fontColor:color,m_HorizontalAlignment:align,m_VerticalAlignment:512,m_TextWrappingMode:1,m_RaycastTarget:false});return p;}
function button(name,parent,x,y,w,h,label,blue=false){const p=parent+'/'+name;const c=blue?'Blue':'Purple';add('prefab instantiate',{prefab:kit+'Prefabs/Buttons_Simple/Large/'+c+'/Long_'+c+'_Large_Button.prefab',name});add('gameobject set_parent',{target:'/'+name,parent,world_position_stays:false});set(p,'UnityEngine.RectTransform',{m_AnchorMin:[0.5,0.5],m_AnchorMax:[0.5,0.5],m_AnchoredPosition:[x,y],m_SizeDelta:[w,h],m_LocalScale:[1,1,1]});set(p,'UnityEngine.UI.Image',{m_Color:blue?[0.22,0.5,0.8,1]:[0.55,0.2,0.95,1]});set(p+'/Text (TMP)','TMPro.TextMeshProUGUI',{m_text:label,m_fontSize:29,m_fontSizeBase:29,m_enableAutoSizing:false,m_fontColor:white,m_RaycastTarget:false});return p;}
function outline(p,color){comp(p,'UnityEngine.UI.Outline');set(p,'UnityEngine.UI.Outline',{m_EffectColor:color,m_EffectDistance:[3,-3]});}
function card(name,parent,x,y,w,h,color){const p=panel(name,parent,x,y,w,h);set(p,'UnityEngine.UI.Image',{m_RaycastTarget:true});comp(p,'UnityEngine.UI.Button');outline(p,color);return p;}
function base(scene,title,subtitle){call('scene','create',{path:'Scenes/'+scene,template:'default'});const root=go('Canvas','');comp(root,'UnityEngine.Canvas');comp(root,'UnityEngine.UI.CanvasScaler');comp(root,'UnityEngine.UI.GraphicRaycaster');set(root,'UnityEngine.Canvas',{m_RenderMode:0,m_AdditionalShaderChannelsFlag:25});set(root,'UnityEngine.UI.CanvasScaler',{m_UiScaleMode:1,m_ReferenceResolution:[1920,1080],m_MatchWidthOrHeight:0.5});const bg=panel('Background',root,0,0,1920,1080,[0.012,0.018,0.045,1]);set(bg,'UnityEngine.RectTransform',{m_AnchorMin:[0,0],m_AnchorMax:[1,1],m_SizeDelta:[0,0]});panel('TopAccent',root,0,537,1920,5,[0.35,0.16,0.65,1]);text('Title',root,0,443,1240,90,title,58);text('Subtitle',root,0,365,1450,70,subtitle,28,muted);button('BackButton',root,-810,450,200,75,'BACK',true);button('HomeButton',root,810,450,200,75,'HOME',true);add('gameobject create',{name:'EventSystem'});comp('/EventSystem','UnityEngine.EventSystems.EventSystem');comp('/EventSystem','UnityEngine.InputSystem.UI.InputSystemUIInputModule');add('gameobject create',{name:'Controllers'});}
function flush(scene){console.log(scene+': '+ops.length+' operations');for(let i=0;i<ops.length;i+=45){const r=call('job','batch',{operations:ops.slice(i,i+45)});if(r.reverted||r.results.some(v=>!v.success))throw Error(JSON.stringify(r));console.log(scene+' '+Math.min(i+45,ops.length)+'/'+ops.length);}ops=[];call('scene','save');call('scene','add_to_build',{path:'Assets/Scenes/'+scene+'.unity'});}
const page=process.argv[2];
if(page==='SongSelection'){
 base(page,'SONG SELECTION','Choose a tempo demo for your session.');comp('/Controllers','RhythmRehab.SongSelectionController');
 const search=panel('Search','/Canvas',-200,280,1120,75,[0.035,0.05,0.1,1]);set(search,'UnityEngine.UI.Image',{m_RaycastTarget:true});comp(search,'TMPro.TMP_InputField');
 const area=go('TextArea',search,0,0,1050,65);comp(area,'UnityEngine.UI.RectMask2D');text('Placeholder',area,0,0,1050,65,'Search song or artist...',29,muted,1);text('Text',area,0,0,1050,65,'',29,white,1);
 button('FilterButton','/Canvas',630,280,400,75,'ALL TEMPOS',true);
 const scroll=go('SongScroll','/Canvas',0,-70,1640,565);comp(scroll,'UnityEngine.UI.ScrollRect');
 const viewport=panel('Viewport',scroll,0,0,1630,560,[1,1,1,1]);set(viewport,'UnityEngine.UI.Image',{m_RaycastTarget:true});comp(viewport,'UnityEngine.UI.Mask');set(viewport,'UnityEngine.UI.Mask',{m_ShowMaskGraphic:false});
 const content=go('Content',viewport,0,0,0,535);set(content,'UnityEngine.RectTransform',{m_AnchorMin:[0,0.5],m_AnchorMax:[0,0.5],m_Pivot:[0,0.5]});comp(content,'UnityEngine.UI.HorizontalLayoutGroup');set(content,'UnityEngine.UI.HorizontalLayoutGroup',{m_Spacing:28,m_ChildAlignment:4,m_ChildControlWidth:true,m_ChildControlHeight:true,m_ChildForceExpandWidth:false,m_ChildForceExpandHeight:true});comp(content,'UnityEngine.UI.ContentSizeFitter');set(content,'UnityEngine.UI.ContentSizeFitter',{m_HorizontalFit:2,m_VerticalFit:0});
 for(let i=0;i<8;i++){
  const p=card('Song'+i,content,0,0,375,520,purple);comp(p,'UnityEngine.UI.LayoutElement');set(p,'UnityEngine.UI.LayoutElement',{m_PreferredWidth:375,m_PreferredHeight:520,m_MinWidth:375});comp(p,'RhythmRehab.SongCardView');
  const cover=panel('Cover',p,0,95,345,275,[0.2,0.14,0.4,1]);
  text('Index',cover,0,30,300,150,(i+1).toString().padStart(2,'0'),108,[1,1,1,0.8]);text('Demo',cover,0,-82,300,60,'TEMPO STUDY',26,[1,1,1,0.8]);
  text('SongTitle',p,0,-90,335,70,'SONG PLACEHOLDER',34,white,1);text('Artist',p,0,-150,335,50,'RhythmRehab Demo',26,muted,1);text('Tempo',p,0,-205,335,55,'120 BPM',25,cyan,1);
  const badge=panel('SelectedBadge',p,90,212,155,40,purple);text('Label',badge,0,0,155,40,'SELECTED',21);
 }
 text('EmptyMessage',scroll,0,0,1400,150,'No songs found. Try a different search or tempo filter.',36,muted);
 button('PreviousButton','/Canvas',-760,-400,110,70,'<',true);button('NextButton','/Canvas',-620,-400,110,70,'>',true);
 text('BrowseLabel','/Canvas',100,-400,1150,60,'8 SONGS · DRAG OR SCROLL TO BROWSE',25,muted);
 text('SelectionLabel','/Canvas',-250,-475,1100,80,'Select a song',30,white,1);button('ContinueButton','/Canvas',650,-475,430,85,'CONTINUE');
 flush(page);
}
if(page==='ModeSelection'){
 base(page,'GAME MODE SELECTION','Choose the mode that suits your session.');comp('/Controllers','RhythmRehab.SelectionPageController');
 text('SongContext','/Canvas',0,265,1600,100,'Selected song',26,muted);
 const therapy=card('TherapyCard','/Canvas',-425,-60,760,500,purple);
 text('Title',therapy,0,155,680,80,'THERAPY MODE',50,purple);panel('Divider',therapy,0,80,650,2,[0.35,0.16,0.65,1]);
 text('Description',therapy,0,-20,650,180,'Consistent note height\nChoose LOW, MEDIUM or HIGH\nHold one target level throughout',34,white,1);
 text('Footnote',therapy,0,-185,650,70,'STRUCTURED HEIGHT PRACTICE',26,purple);
 const free=card('FreeCard','/Canvas',425,-60,760,500,cyan);
 text('Title',free,0,155,680,80,'FREE MODE',50,cyan);panel('Divider',free,0,80,650,2,[0.05,0.5,0.65,1]);
 text('Description',free,0,-20,650,180,'Varied heights between guide lines\nChoose EASY, MEDIUM or HARD\nFollow a changing rhythm pattern',34,white,1);
 text('Footnote',free,0,-185,650,70,'MIXED HEIGHT CHALLENGE',26,cyan);
 panel('InfoPanel','/Canvas',0,-425,1620,115);text('Info','/Canvas',0,-425,1520,95,'THERAPY keeps notes at one height. FREE varies the heights and challenge.',29,muted);
 flush(page);
}
if(page==='ExerciseSelection'||page==='DifficultySelection'){
 const height=page==='ExerciseSelection';base(page,height?'EXERCISE SELECTION':'DIFFICULTY SELECTION',height?'Choose the target height for your leg-raise session.':'Choose your challenge level for Free Mode.');comp('/Controllers','RhythmRehab.SelectionPageController');
 text('SongContext','/Canvas',0,270,1600,100,'Selected song',26,muted);
 const colors=height?[[0.2,0.9,0.45,1],[1,0.7,0.1,1],[1,0.25,0.4,1]]:[[0.2,0.9,0.45,1],cyan,[1,0.25,0.4,1]];
 const names=height?['LOW','MEDIUM','HIGH']:['EASY','MEDIUM','HARD'];
 const desc=height?['Lower target level\nAll notes stay LOW\n32 evenly spaced notes','Middle target level\nAll notes stay MEDIUM\n32 evenly spaced notes','Upper target level\nAll notes stay HIGH\n32 evenly spaced notes']:['16 notes\nHeights from LOW to MID\nWider timing window','24 notes\nHeights across the full range\nStandard timing window','32 notes\nLarger continuous height changes\nTighter timing window'];
 for(let i=0;i<3;i++){
  const p=card('Choice'+i,'/Canvas',(i-1)*555,-70,500,500,colors[i]);
  text('Title',p,0,155,445,85,names[i],50,colors[i]);text('Level',p,0,65,430,70,height?'LEVEL '+(i+1):'*'.repeat(i+1),40,colors[i]);
  panel('Divider',p,0,10,425,2,colors[i]);text('Description',p,0,-100,420,175,desc[i],31,white,1);
  const badge=panel('SelectedBadge',p,0,-210,240,45,colors[i]);text('Label',badge,0,0,240,45,'SELECTED',23,[0.025,0.035,0.065,1]);
 }
 text('SelectedLabel','/Canvas',-235,-440,1100,110,'Selected option',28,muted,1);button('ContinueButton','/Canvas',655,-450,430,95,'START SESSION');
 flush(page);
}

