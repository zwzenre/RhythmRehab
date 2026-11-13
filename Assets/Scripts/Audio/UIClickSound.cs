using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmRehab
{
    [RequireComponent(typeof(Selectable))]
    [DisallowMultipleComponent]
    public sealed class UIClickSound : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UIScreenAudio screenAudio;
        private Selectable control;
        private Button button;

        private void Awake()
        {
            control = GetComponent<Selectable>();
            button = control as Button;
            if (button) button.onClick.AddListener(Play);
        }

        private void OnDestroy()
        {
            if (button) button.onClick.RemoveListener(Play);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!button && eventData.button == PointerEventData.InputButton.Left && control.IsInteractable())
                Play();
        }

        private void Play()
        {
            if (enabled && screenAudio) screenAudio.PlayClick();
        }
    }
}
