using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PatientSelectionPresenter : MonoBehaviour
    {
        [Header("Authored patient card prefab and page UI")]
        [SerializeField] private PatientCardView cardPrefab, guestCard;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect patientScroll;
        [SerializeField] private TMP_InputField search;
        [SerializeField] private TMP_Text status, selectionLabel;
        [SerializeField] private Button refreshButton, confirmButton;
        [SerializeField] private UIScreenAudio screenAudio;
        private readonly List<PatientCardView> cards = new List<PatientCardView>();
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
            if (!chosen.IsGuest && (!directory || !directory.Patients.Any(p => p.id == chosen.id))) return;
            PatientContext.Select(chosen);
            IrisSceneTransition.Load("MainMenu");
        }
        private void RefreshPage()
        {
            if (!directory) status.text = "Patient list unavailable · Guest is ready";
            else if (directory.IsLoading) status.text = "Refreshing patients… · Guest is ready";
            else
            {
                RebuildCards();
                status.text = directory.Error ?? (directory.Patients.Count == 0 ? "No patients found · Guest is ready" : directory.Patients.Count + " PATIENTS · DRAG OR SCROLL TO BROWSE");
            }
            refreshButton.interactable = directory && !directory.IsLoading;
            UpdateSelection();
        }
        private void RebuildCards()
        {
            foreach (var card in cards) if (card) { card.gameObject.SetActive(false); Destroy(card.gameObject); }
            cards.Clear();
            if (directory)
            {
                if (chosen != null && !chosen.IsGuest)
                    chosen = directory.Patients.FirstOrDefault(p => p.id == chosen.id) ?? chosen;
                foreach (var patient in directory.Patients)
                {
                    var card = Instantiate(cardPrefab, content);
                    card.name = "PatientCard"; card.Bind(patient, this); cards.Add(card);
                }
            }
            Filter(search ? search.text : "");
            patientScroll.horizontalNormalizedPosition = 0;
        }
        public void Filter(string query)
        {
            query = (query ?? "").Trim();
            foreach (var card in cards)
                card.gameObject.SetActive(query.Length == 0 || card.Patient.displayName.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0 || card.Patient.id.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
        private void UpdateSelection()
        {
            if (chosen == null) return;
            guestCard.SetSelected(chosen.IsGuest);
            foreach (var card in cards) card.SetSelected(card.Patient.id == chosen.id);
            bool valid = chosen.IsGuest || (directory && directory.Patients.Any(p => p.id == chosen.id));
            confirmButton.interactable = valid;
            selectionLabel.text = "SELECTED · " + PatientContext.CleanName(chosen.displayName);
        }
        private void OnDestroy() { if (directory) directory.Changed -= RefreshPage; }
    }
}
