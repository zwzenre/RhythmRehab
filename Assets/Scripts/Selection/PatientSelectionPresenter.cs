using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PatientSelectionPresenter : MonoBehaviour
    {
        [Header("Patient card slots and page UI")]
        [SerializeField] private PatientCardView guestCard;
        [SerializeField] private PatientCardView[] cardSlots;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect patientScroll;
        [SerializeField] private TMP_InputField search;
        [SerializeField] private TMP_Text status, selectionLabel;
        [SerializeField] private Button refreshButton, confirmButton;
        [SerializeField] private Button previousPageButton, nextPageButton;
        [SerializeField] private UIScreenAudio screenAudio;
        private PatientProfile[] filteredPatients = Array.Empty<PatientProfile>();
        private int page;
        private PatientProfile chosen;
        private PatientDirectory directory;

        private void Start()
        {
            directory = PatientDirectory.Instance;
            chosen = PatientContext.IsGuest ? PatientProfile.Guest : new PatientProfile { id = PatientContext.PatientId, displayName = PatientContext.DisplayName };
            guestCard.Bind(PatientProfile.Guest, this);
            if (directory) directory.Changed += RefreshPage;
            RefreshPage();
        }
        public void RefreshPatients() { if (directory) directory.Refresh(); }
        public void Choose(PatientProfile patient)
        {
            if (IrisSceneTransition.IsBusy || patient == null) return;
            chosen = patient;
            if (screenAudio) screenAudio.PlayClick();
            UpdateSelection();
        }
        public void Confirm()
        {
            if (IrisSceneTransition.IsBusy || chosen == null) return;
            if (!chosen.IsGuest && (!FirebaseLoginSession.Instance || !FirebaseLoginSession.Instance.IsSignedIn || !directory || !directory.Patients.Any(p => p.id == chosen.id))) return;
            IrisSceneTransition.Load("MainMenu");
            if (chosen.IsGuest && FirebaseLoginSession.Instance) FirebaseLoginSession.Instance.SignOut();
            else PatientContext.Select(chosen);
        }
        private void RefreshPage()
        {
            if (directory && directory.HasLoaded && !directory.IsLoading && chosen != null && !chosen.IsGuest)
                chosen = directory.Patients.FirstOrDefault(patient => patient.id == chosen.id) ?? PatientProfile.Guest;
            refreshButton.interactable = directory && !directory.IsLoading;
            Filter(search ? search.text : "");
        }

        public void Filter(string query)
        {
            query = (query ?? "").Trim();
            filteredPatients = directory ? directory.Patients.Where(patient =>
                query.Length == 0 ||
                patient.displayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                patient.id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray() : Array.Empty<PatientProfile>();
            page = 0;
            ShowPage();
        }

        private int PageCount => cardSlots.Length == 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt((float)filteredPatients.Length / cardSlots.Length));
        public void PreviousPage() => ChangePage(-1);
        public void NextPage() => ChangePage(1);

        private void ChangePage(int direction)
        {
            int next = Mathf.Clamp(page + direction, 0, PageCount - 1);
            if (next == page) return;
            page = next;
            ShowPage();
        }

        private void ShowPage()
        {
            int first = page * cardSlots.Length;
            for (int i = 0; i < cardSlots.Length; i++)
            {
                var card = cardSlots[i];
                if (!card) continue;
                int index = first + i;
                var patient = index < filteredPatients.Length ? filteredPatients[index] : null;
                card.Bind(patient, this);
                card.gameObject.SetActive(patient != null);
            }

            bool canPage = PageCount > 1;
            if (previousPageButton)
            {
                previousPageButton.gameObject.SetActive(canPage);
                previousPageButton.interactable = page > 0;
            }
            if (nextPageButton)
            {
                nextPageButton.gameObject.SetActive(canPage);
                nextPageButton.interactable = page + 1 < PageCount;
            }
            if (!directory) status.text = "Patient list unavailable · Guest is ready";
            else if (directory.IsLoading) status.text = "Refreshing patients… · Guest is ready";
            else if (directory.Error != null) status.text = directory.Error;
            else if (directory.Patients.Count == 0) status.text = "No patients found · Guest is ready";
            else if (filteredPatients.Length == 0) status.text = "No matching patients";
            else status.text = canPage
                ? $"{filteredPatients.Length} PATIENTS · PAGE {page + 1}/{PageCount}"
                : $"{filteredPatients.Length} PATIENTS";

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            patientScroll.horizontalNormalizedPosition = 0;
            UpdateSelection();
        }
        private void UpdateSelection()
        {
            if (chosen == null) return;
            guestCard.SetSelected(chosen.IsGuest);
            foreach (var card in cardSlots)
                if (card) card.SetSelected(card.Patient != null && card.Patient.id == chosen.id);
            bool valid = chosen.IsGuest || (directory && directory.Patients.Any(p => p.id == chosen.id));
            confirmButton.interactable = valid;
            selectionLabel.text = "SELECTED · " + PatientContext.CleanName(chosen.displayName);
        }
        private void OnDestroy() { if (directory) directory.Changed -= RefreshPage; }
    }
}
