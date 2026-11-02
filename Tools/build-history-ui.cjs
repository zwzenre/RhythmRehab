const fs=require('fs');
const helpers=fs.readFileSync(__dirname+'/build-selection-ui.cjs','utf8').split('const page=process.argv[2];')[0];
const build=`
base('PerformanceHistory','PERFORMANCE HISTORY','Track your progress across sessions.');
comp('/Controllers','RhythmRehab.PerformanceHistoryPresenter');comp('/Controllers','RhythmRehab.SceneNavigation');
text('Overview','/Canvas',0,280,1720,75,'Your journey starts with your first session.',29,cyan);
const graph=panel('GraphPanel','/Canvas',-300,-120,1140,700);
const names=['ACCURACY','AVERAGE ROM','SCORE','MOVEMENT'];
for(let i=0;i<4;i++)button('Metric'+i,graph,-405+i*270,285,250,65,names[i],true);
text('GraphTitle',graph,0,210,1050,65,'OVERALL ACCURACY · LAST 8 SESSIONS',26,purple);
const plot=go('Plot',graph,-450,-235,0,0);
for(let i=0;i<5;i++){
 panel('Grid'+i,plot,450,i*100,900,2,[.1,.13,.23,1]);
 text('Axis'+i,plot,-65,i*100,100,45,(i*25)+'%',23,muted);
}
for(let i=0;i<7;i++)panel('Segment'+i,plot,0,0,0,4,purple);
for(let i=0;i<8;i++){
 panel('Point'+i,plot,0,0,16,16,purple);
 text('Value'+i,plot,0,30,120,45,'',23,purple);
 text('Date'+i,plot,0,-35,115,40,'',20,muted);
}
text('GraphMessage',graph,10,-15,880,230,'No sessions yet.\\nPlay a session to see your progress here.',34,muted);
text('GraphRange',graph,0,-310,1050,65,'Oldest → newest',23,muted);
const list=panel('SessionPanel','/Canvas',590,-120,580,700);
text('ListTitle',list,0,300,540,60,'RECENT SESSIONS',30,purple);
const scroll=go('SessionScroll',list,0,-5,540,525);comp(scroll,'UnityEngine.UI.ScrollRect');
const viewport=panel('Viewport',scroll,0,0,540,525,[1,1,1,1]);set(viewport,'UnityEngine.UI.Image',{m_RaycastTarget:true});comp(viewport,'UnityEngine.UI.Mask');set(viewport,'UnityEngine.UI.Mask',{m_ShowMaskGraphic:false});
const content=go('Content',viewport,0,0,530,0);set(content,'UnityEngine.RectTransform',{m_AnchorMin:[.5,1],m_AnchorMax:[.5,1],m_Pivot:[.5,1]});
comp(content,'UnityEngine.UI.VerticalLayoutGroup');set(content,'UnityEngine.UI.VerticalLayoutGroup',{m_Spacing:12,m_ChildAlignment:1,m_ChildControlWidth:true,m_ChildControlHeight:true,m_ChildForceExpandWidth:true,m_ChildForceExpandHeight:false});
comp(content,'UnityEngine.UI.ContentSizeFitter');set(content,'UnityEngine.UI.ContentSizeFitter',{m_HorizontalFit:0,m_VerticalFit:2});
for(let i=0;i<10;i++){
 const row=panel('Row'+i,content,0,0,530,180,[.05,.065,.13,1]);comp(row,'UnityEngine.UI.LayoutElement');set(row,'UnityEngine.UI.LayoutElement',{m_PreferredHeight:180,m_MinHeight:180});
 text('Text',row,0,0,495,165,'',25,white,1);
}
button('ViewAllButton',list,0,-310,500,65,'VIEW ALL');
button('PreviousButton','/Canvas',345,-510,90,55,'<',true);button('NextButton','/Canvas',835,-510,90,55,'>',true);
text('PageLabel','/Canvas',590,-510,390,55,'Complete a session to begin your history.',19,muted);
text('StorageNote','/Canvas',-300,-510,1130,50,'Saved on this computer · Mouse prototype · ROM not measured',24,muted);
flush('PerformanceHistory');
`;
eval(helpers+build);
