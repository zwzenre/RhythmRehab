using System.Collections;
using TMPro;
using UnityEngine;

namespace RhythmRehab
{
    internal enum ChartAward { None, FullCombo, AllPerfect }

    public sealed class ChartFinishCelebration : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private RectTransform banner;
        [SerializeField] private GameObject fullCombo, allPerfect;
        [SerializeField] private ChartAwardParticles particles;
        [SerializeField] private AudioSource awardAudio;
        [SerializeField] private AudioClip fullComboSound, allPerfectSound;

        internal static ChartAward AwardFor(SessionSummary session)
        {
            if (session == null || session.endedEarly || session.guidedReps || session.isCloudRecord ||
                session.totalNotes <= 0 || session.Judged != session.totalNotes || session.miss != 0 ||
                session.brokenHolds != 0 || session.bestCombo != session.totalNotes)
                return ChartAward.None;
            return session.perfect == session.totalNotes ? ChartAward.AllPerfect : ChartAward.FullCombo;
        }

        internal IEnumerator Play(ChartAward award)
        {
            bool perfect = award == ChartAward.AllPerfect;
            fullCombo.SetActive(!perfect);
            allPerfect.SetActive(perfect);
            var center = banner.anchoredPosition;
            var text = (perfect ? allPerfect : fullCombo).GetComponentInChildren<TMP_Text>();
            Color tint = perfect ? new Color32(255, 195, 88, 255) : new Color32(99, 222, 255, 255);
            text.canvasRenderer.SetAlpha(0);
            overlay.alpha = 0;
            particles.ShowFrame(0, tint);
            gameObject.SetActive(true);
            text.ForceMeshUpdate();
            AudioClip cue = perfect ? allPerfectSound : fullComboSound;
            float cueTime = perfect ? 0f : .25f;
            // Skip the quiet lead-in so the whoosh peaks as the gold title settles.
            float cueOffset = perfect ? .85f : 0f;
            float volume = LocalSettingsStore.Default.Load(out _).sfxVolume * (perfect ? .95f : .8f);
            const float duration = 1.9f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float reveal = Mathf.Clamp01((elapsed - .09f) / .45f);
                float exit = Mathf.InverseLerp(1.58f, duration, elapsed);
                banner.anchoredPosition = center + Vector2.right *
                    (345f * Mathf.Pow(1 - reveal, 3) - 95f * exit * exit);
                overlay.alpha = Mathf.Clamp01(elapsed / .24f) * (1 - exit);
                text.canvasRenderer.SetAlpha(Mathf.SmoothStep(0, 1, reveal));
                if (awardAudio)
                {
                    awardAudio.volume = volume * (1 - exit);
                    if (cue && elapsed >= cueTime)
                    {
                        awardAudio.clip = cue;
                        awardAudio.time = Mathf.Min(cueOffset, cue.length * .5f);
                        awardAudio.Play();
                        cue = null;
                    }
                }
                particles.ShowFrame(elapsed, tint);
                Shine(text, tint, elapsed);
                yield return null;
            }
            if (awardAudio) awardAudio.Stop();
            gameObject.SetActive(false);
            banner.anchoredPosition = center;
        }

        private static void Shine(TMP_Text text, Color tint, float time)
        {
            var bounds = text.textBounds;
            float sweep = Mathf.Lerp(-.3f, 1.3f, Mathf.Repeat((time - .22f) / .78f, 1));
            var info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible) continue;
                var mesh = info.meshInfo[character.materialReferenceIndex];
                for (int v = character.vertexIndex; v < character.vertexIndex + 4; v++)
                {
                    Vector3 point = mesh.vertices[v];
                    float x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, point.x);
                    float y = Mathf.InverseLerp(bounds.min.y, bounds.max.y, point.y);
                    float highlight = 1 - Mathf.Clamp01(Mathf.Abs(x + .12f * y - sweep) / .24f);
                    Color baseColour = Color.Lerp(tint * .77f, tint, y);
                    Color result = Color.Lerp(baseColour, Color.white, .18f + .82f * highlight);
                    result.a = 1;
                    mesh.colors32[v] = result;
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
