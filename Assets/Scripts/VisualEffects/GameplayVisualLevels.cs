using System;
using UnityEngine;

namespace RhythmRehab
{
    public sealed class GameplayVisualLevels : MonoBehaviour
    {
        [Serializable] public sealed class Theme
        {
            public GameObject root;
            public UnityEngine.UI.Graphic[] reactiveDetails;
            public VisualLevelHitAccent[] hitAccents;
            [Range(0,.4f)] public float reactionOpacity = .15f;
        }
        [SerializeField] private PrototypeGameplay gameplay;
        [Tooltip("Therapy, Easy, Medium, Hard, in that order.")]
        [SerializeField] private Theme[] themes;
        [SerializeField, Range(0,2)] private float reactionIntensity = 1f;
        private Color[][] originalColours;
        private Color reactionColour;
        private float reaction;
        private int nextAccent;
        public float Clock { get; private set; }
        public bool Frozen => !gameplay || gameplay.IsPaused || IrisSceneTransition.IsBusy;
        public int ActiveTheme { get; private set; }
        public int SuccessfulReactions { get; private set; }
        public float ReactionStrength => reaction;

        private void Awake()
        {
            originalColours=new Color[themes.Length][];
            for(int i=0;i<themes.Length;i++)
            {
                originalColours[i]=new Color[themes[i].reactiveDetails.Length];
                for(int j=0;j<originalColours[i].Length;j++)originalColours[i][j]=themes[i].reactiveDetails[j].color;
            }
            if(SessionSelection.Configured)Show(SessionSelection.Mode,SessionSelection.Difficulty);
            else Show(SessionMode.Free,FreeDifficulty.Easy);
        }
        public void Show(SessionMode mode,FreeDifficulty difficulty)
        {
            ActiveTheme=mode==SessionMode.Therapy?0:1+(int)difficulty;
            reaction=0;nextAccent=0;
            for(int i=0;i<themes.Length;i++)
            {
                foreach(var accent in themes[i].hitAccents)accent.Clear();
                for(int j=0;j<themes[i].reactiveDetails.Length;j++)themes[i].reactiveDetails[j].color=originalColours[i][j];
                themes[i].root.SetActive(i==ActiveTheme);
            }
        }
        private void Update()
        {
            if(Frozen)return;
            Clock+=Time.unscaledDeltaTime;
            reaction=Mathf.MoveTowards(reaction,0,Time.unscaledDeltaTime/1.1f);
            var theme=themes[ActiveTheme];
            for(int i=0;i<theme.reactiveDetails.Length;i++)
            {
                var original=originalColours[ActiveTheme][i];
                var colour=Color.Lerp(original,reactionColour,reaction*.45f);
                colour.a=Mathf.Clamp01(original.a+reaction*theme.reactionOpacity*reactionIntensity);
                theme.reactiveDetails[i].color=colour;
            }
        }
        public void OnSuccessfulHit(Vector2 position,HitJudgement judgement,Color colour)
        {
            if(judgement==HitJudgement.Miss||Frozen)return;
            SuccessfulReactions++;reactionColour=colour;reaction=judgement==HitJudgement.Perfect?1:judgement==HitJudgement.Great?.8f:.6f;
            var accents=themes[ActiveTheme].hitAccents;
            int count=ActiveTheme<=1?1:judgement==HitJudgement.Perfect?3:judgement==HitJudgement.Great?2:1;
            for(int i=0;i<count&&accents.Length>0;i++)accents[nextAccent++%accents.Length].Play(position,colour,i);
        }
    }
}
