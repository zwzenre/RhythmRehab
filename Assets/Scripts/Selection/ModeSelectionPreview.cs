using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RhythmRehab
{
    public sealed class ModeSelectionPreview : MonoBehaviour
    {
        [SerializeField] private CanvasGroup[] cards;
        [SerializeField] private CanvasGroup preview;
        [SerializeField] private ModeGameplayDemo demo;
        [SerializeField, Min(.1f)] private float transitionSeconds = .28f;
        private Vector2 previewOrigin;
        private int replacedCard = -1;
        public bool IsOpen => replacedCard >= 0;
        public bool IsTransitioning { get; private set; }

        private void Awake() { preview.gameObject.SetActive(false); }
        public void Show(SessionMode mode)
        {
            if (IsTransitioning || IrisSceneTransition.IsBusy || IsOpen) return;
            replacedCard = mode == SessionMode.Therapy ? 1 : 0;
            previewOrigin = ((RectTransform)cards[replacedCard].transform).anchoredPosition;
            StartCoroutine(Open(mode));
        }
        private IEnumerator Open(SessionMode mode)
        {
            IsTransitioning = true;
            var card = cards[replacedCard];
            card.interactable = card.blocksRaycasts = false;
            yield return Animate(card, 1, 0, false);
            ((RectTransform)preview.transform).anchoredPosition = previewOrigin;
            preview.alpha = 0; preview.interactable = preview.blocksRaycasts = false;
            preview.gameObject.SetActive(true); demo.Play(mode);
            yield return Animate(preview, 0, 1, true);
            preview.interactable = preview.blocksRaycasts = true;
            IsTransitioning = false;
        }
        public void Close()
        {
            if (!IsOpen || IsTransitioning || IrisSceneTransition.IsBusy) return;
            StartCoroutine(Restore());
        }
        private IEnumerator Restore()
        {
            IsTransitioning = true; preview.interactable = preview.blocksRaycasts = false;
            yield return Animate(preview, 1, 0, true);
            preview.gameObject.SetActive(false);
            var card = cards[replacedCard];
            yield return Animate(card, 0, 1, false);
            card.interactable = card.blocksRaycasts = true;
            replacedCard = -1; IsTransitioning = false;
        }
        private IEnumerator Animate(CanvasGroup group, float from, float to, bool isPreview)
        {
            var rect = (RectTransform)group.transform;
            Vector2 origin = isPreview ? previewOrigin : rect.anchoredPosition;
            float direction = replacedCard == 1 ? 1 : -1;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / transitionSeconds)
            {
                float ease = 1 - Mathf.Pow(1 - t, 3);
                group.alpha = Mathf.Lerp(from, to, ease);
                rect.anchoredPosition = origin + Vector2.right * direction * 35 * (to > from ? 1 - ease : ease);
                yield return null;
            }
            group.alpha = to; rect.anchoredPosition = origin;
        }
        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            // Restore authored card state if the page is disabled during an animation.
            if (IsOpen && cards[replacedCard])
                ((RectTransform)cards[replacedCard].transform).anchoredPosition = previewOrigin;
            foreach (var card in cards) if (card) { card.alpha = 1; card.interactable = card.blocksRaycasts = true; }
            if (preview) preview.gameObject.SetActive(false);
            replacedCard = -1; IsTransitioning = false;
        }
    }
}
