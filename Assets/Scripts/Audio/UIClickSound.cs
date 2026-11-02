using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmRehab
{
    [RequireComponent(typeof(Selectable))]
    [DisallowMultipleComponent]
    public sealed class UIClickSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] private UIScreenAudio screenAudio;
        private bool CanPlay => isActiveAndEnabled && GetComponent<Selectable>().IsInteractable();
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && CanPlay && screenAudio)
                screenAudio.PlayClick();
        }
        public void OnSubmit(BaseEventData eventData)
        {
            if (GetComponent<Button>() && CanPlay && screenAudio) screenAudio.PlayClick();
        }
    }
}
