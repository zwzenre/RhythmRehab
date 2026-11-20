using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class GameplayAtmosphere : MonoBehaviour
    {
        [SerializeField] private PrototypeGameplay gameplay;
        [SerializeField] private RectTransform[] farStars;
        [SerializeField] private RectTransform[] nearStars;
        [SerializeField] private RawImage laneGlow;
        [SerializeField] private RawImage comet;
        private Vector2[] farPositions, nearPositions;
        private float motionTime, glowAge, cometAge;
        private float nextCometTime = 16f;
        private const float GlowDuration = .34f;
        private const float CometDuration = 2.6f;
        private bool reduced;

        private void Awake()
        {
            farPositions = RememberPositions(farStars);
            nearPositions = RememberPositions(nearStars);
            var wellbeing = SessionWellbeing.Snapshot;
            reduced = wellbeing != null && !wellbeing.skipped && (wellbeing.highPain || wellbeing.highStress);
            laneGlow.enabled = false;
            comet.enabled = false;
        }

        public void PulseLane(Vector2 hit, Color colour)
        {
            laneGlow.rectTransform.anchoredPosition = hit + new Vector2(105, 0);
            glowAge = 0;
            colour.a = reduced ? .105f : .3f;
            laneGlow.color = colour;
            laneGlow.enabled = true;
        }

        private void Update()
        {
            if (!gameplay || gameplay.IsPaused || IrisSceneTransition.IsBusy) return;
            float delta = Time.unscaledDeltaTime;
            motionTime += delta * (reduced ? .3f : 1f);
            Drift(farStars, farPositions, 4f);
            Drift(nearStars, nearPositions, 11f);
            if (laneGlow.enabled)
            {
                glowAge = Mathf.Min(glowAge + delta, GlowDuration);
                var tint = laneGlow.color;
                tint.a = (reduced ? .105f : .3f) * Mathf.Pow(1f - glowAge / GlowDuration, 2f);
                laneGlow.color = tint;
                laneGlow.enabled = glowAge < GlowDuration;
            }

            if (reduced || gameplay.Notes == null) return;
            UpdateComet(delta);
        }

        private void UpdateComet(float delta)
        {
            if (!comet.enabled)
            {
                float elapsed = gameplay.Elapsed;
                if (elapsed < nextCometTime || elapsed + CometDuration >= gameplay.Notes.EndTime || !HasQuietMoment(elapsed))
                    return;
                nextCometTime = elapsed + 27f;
                cometAge = 0;
                comet.enabled = true;
            }

            cometAge += delta;
            float progress = Mathf.Clamp01(cometAge / CometDuration);
            comet.rectTransform.anchoredPosition = Vector2.Lerp(new Vector2(1120, 475), new Vector2(-1140, 390), progress);
            var tint = comet.color;
            tint.a = .24f * Mathf.Sin(progress * Mathf.PI);
            comet.color = tint;
            comet.enabled = progress < 1f;
        }

        private bool HasQuietMoment(float elapsed)
        {
            var runner = gameplay.Notes;
            if (gameplay.ActiveHoldIndex >= 0) return false;
            foreach (var note in runner.Chart)
            {
                float start = note.beat * runner.SecondsPerBeat + runner.JudgementOffset;
                float end = note.EndBeat * runner.SecondsPerBeat + runner.JudgementOffset;
                if (start > elapsed + .65f) break;
                if (end >= elapsed - .3f) return false;
            }
            return true;
        }

        private void Drift(RectTransform[] stars, Vector2[] positions, float speed)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                float x = Mathf.Repeat(positions[i].x - motionTime * speed + 980, 1960) - 980;
                stars[i].anchoredPosition = new Vector2(x, positions[i].y);
            }
        }

        private static Vector2[] RememberPositions(RectTransform[] stars)
        {
            var positions = new Vector2[stars.Length];
            for (int i = 0; i < stars.Length; i++) positions[i] = stars[i].anchoredPosition;
            return positions;
        }
    }
}
