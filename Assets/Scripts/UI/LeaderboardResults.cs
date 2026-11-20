using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class LeaderboardResults : MonoBehaviour
    {
        [SerializeField] private Button publish, browse, confirm, cancel;
        [SerializeField] private Button publishAsSavedName;
        [SerializeField] private TMP_Text savedNameLabel;
        [SerializeField] private RectTransform promptPanel;
        [SerializeField] private GameObject prompt;
        [SerializeField] private TMP_InputField displayName;
        [SerializeField] private TMP_Text status, footerStatus;
        [SerializeField] private RectTransform replay, menu;
        private SessionSummary session;
        private bool publishing;
        private string savedName;
        private bool CanSubmit => !publishing && prompt.activeSelf && session?.CanPublishLeaderboard == true &&
            !IrisSceneTransition.IsBusy;

        private void Start()
        {
            session = SessionSummary.Last;
            bool free = session?.freeMode == true;
            browse.gameObject.SetActive(free);
            publish.gameObject.SetActive(free);
            footerStatus.gameObject.SetActive(free);
            publish.interactable = session?.CanPublishLeaderboard == true;
            if (free)
            {
                PlaceAction(replay, -595);
                PlaceAction(menu, -63);
            }
            if (free && !publish.interactable)
                footerStatus.text = session.endedEarly ? "Finish the song to publish." :
                    "Use one control type for the whole song to publish.";
        }

        private static void PlaceAction(RectTransform action, float x)
        {
            action.anchorMin = action.anchorMax = new Vector2(.5f, 0);
            action.anchoredPosition = new Vector2(x, 75);
            action.sizeDelta = new Vector2(510, 82);
        }

        public void ShowPublish()
        {
            if (session?.CanPublishLeaderboard != true || publishing || !publish.interactable || IrisSceneTransition.IsBusy) return;
            status.text = "";
            ApplySavedName(LocalSettingsStore.Default.Load(out _).lastLeaderboardName);
            prompt.SetActive(true);
            if (publishAsSavedName.gameObject.activeSelf) publishAsSavedName.Select();
            else
            {
                displayName.Select();
                displayName.ActivateInputField();
            }
        }

        private void ApplySavedName(string previousName)
        {
            bool hasName = FreeLeaderboard.TryName(previousName, out savedName);
            publishAsSavedName.gameObject.SetActive(hasName);
            if (hasName) savedNameLabel.text = $"Publish as {savedName}";
            promptPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, hasName ? 505 : 415);
        }

        public void CancelPublish()
        {
            if (publishing) return;
            prompt.SetActive(false);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(publish.gameObject);
        }

        public void ConfirmPublish()
        {
            if (!CanSubmit) return;
            if (!FreeLeaderboard.TryName(displayName.text, out var alias))
            {
                status.text = "Use 1–24 letters, numbers, spaces, hyphens or underscores.";
                return;
            }
            StartCoroutine(Publish(alias));
        }

        public void PublishWithSavedName()
        {
            if (CanSubmit && publishAsSavedName.isActiveAndEnabled)
                StartCoroutine(Publish(savedName));
        }

        private IEnumerator Publish(string alias)
        {
            publishing = true;
            confirm.interactable = cancel.interactable = displayName.interactable = publishAsSavedName.interactable = false;
            status.text = "Publishing…";
            yield return FreeLeaderboard.Publish(session, alias, result =>
            {
                if (result == FreeLeaderboard.PublishResult.Unavailable)
                    status.text = "Couldn’t publish. Please try again.";
                else
                {
                    if (!LocalSettingsStore.Default.TrySaveLeaderboardName(alias, out var error))
                        Debug.LogWarning("Could not remember the leaderboard display name: " + error);
                    prompt.SetActive(false);
                    publish.interactable = false;
                    if (EventSystem.current) EventSystem.current.SetSelectedGameObject(browse.gameObject);
                    footerStatus.text = result == FreeLeaderboard.PublishResult.Saved
                        ? "Score published." : "Your best score is already on the board.";
                }
            });
            publishing = false;
            confirm.interactable = cancel.interactable = displayName.interactable = publishAsSavedName.interactable = true;
        }

        public void Browse()
        {
            if (session?.freeMode != true || publishing || IrisSceneTransition.IsBusy) return;
            LeaderboardPresenter.OpenFromResults = session;
            if (!IrisSceneTransition.Load("SongSelection")) LeaderboardPresenter.OpenFromResults = null;
        }
    }
}
