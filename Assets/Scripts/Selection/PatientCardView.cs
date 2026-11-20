using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PatientCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel, detailLabel, selectedLabel;
        [SerializeField] private Outline selectionOutline;
        private PatientSelectionPresenter owner;
        public PatientProfile Patient { get; private set; }
        public void Bind(PatientProfile patient, PatientSelectionPresenter presenter)
        {
            Patient = patient;
            owner = presenter;
            if (patient == null)
            {
                nameLabel.text = "";
                detailLabel.text = "";
                return;
            }
            nameLabel.text = patient.displayName;
            detailLabel.text = patient.IsGuest ? "Local play" : "ID · " + (patient.id.Length > 8 ? patient.id.Substring(patient.id.Length - 8) : patient.id);
        }
        public void SetSelected(bool selected)
        {
            selectionOutline.enabled = selected;
            selectedLabel.gameObject.SetActive(selected);
        }
        public void Choose() { if (owner) owner.Choose(Patient); }
    }
}
