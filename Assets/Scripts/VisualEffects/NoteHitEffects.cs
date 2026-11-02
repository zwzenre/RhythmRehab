using UnityEngine;

namespace RhythmRehab
{
    public sealed class NoteHitEffects : MonoBehaviour
    {
        [SerializeField] private RectTransform[] burstPositions;
        [SerializeField] private ParticleSystem[] bursts;
        [SerializeField] private NoteHitRipples ripples;
        [Header("Judgement colours")]
        [SerializeField] private Color perfectColor = new Color(1f, .82f, .15f);
        [SerializeField] private Color greatColor = new Color(.7f, .3f, 1f);
        [SerializeField] private Color goodColor = new Color(.2f, .6f, 1f);
        private int next;
        public int PlayedBursts { get; private set; }

        private void Awake() => Clear();
        public Color ColorFor(HitJudgement judgement) => judgement == HitJudgement.Perfect ? perfectColor :
            judgement == HitJudgement.Great ? greatColor : goodColor;

        public void Play(Vector2 localPosition, HitJudgement judgement = HitJudgement.Perfect)
        {
            if (judgement == HitJudgement.Miss || bursts == null || bursts.Length == 0) return;
            if (ripples) ripples.Play(localPosition, judgement, ColorFor(judgement));
            int slot = next++ % bursts.Length;
            bursts[slot].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            burstPositions[slot].anchoredPosition = localPosition;
            foreach (var graphic in burstPositions[slot].GetComponentsInChildren<CanvasParticleGraphic>())
                graphic.color = ColorFor(judgement);
            bursts[slot].Play(true);
            PlayedBursts++;
        }
        public void SetPaused(bool paused)
        {
            if (ripples) ripples.SetPaused(paused);
            foreach (var burst in bursts)
            {
                if (paused && burst.isPlaying) burst.Pause(true);
                else if (!paused && burst.isPaused) burst.Play(true);
            }
        }
        public void Clear()
        {
            if (ripples) ripples.Clear();
            if (bursts == null) return;
            foreach (var burst in bursts) if (burst) burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
