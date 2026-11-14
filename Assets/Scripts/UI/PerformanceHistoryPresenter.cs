using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmRehab
{
    public sealed class PerformanceHistoryPresenter : MonoBehaviour
    {
        [Header("Saved UI objects — no runtime object creation")]
        [SerializeField] private TMP_Text overview, graphTitle, graphMessage, graphRange, pageLabel, listTitle;
        [SerializeField] private TMP_Text[] rows, valueLabels, dateLabels;
        [SerializeField] private RectTransform[] points, segments;
        [SerializeField] private Image[] tabImages;
        [SerializeField] private TMP_Text[] axisLabels;
        [SerializeField] private ScrollRect sessionScroll;
        [SerializeField] private Button previousButton, nextButton;
        [SerializeField] private TMP_Text viewAllLabel;
        [SerializeField] private Button uploadSelectedButton;
        [SerializeField] private Button deleteSelectedButton;
        [SerializeField] private GameObject deleteConfirmation;
        [SerializeField] private TMP_Text deletePrompt;
        [SerializeField] private Outline[] selectionOutlines;
        [SerializeField] private TMP_Text patientContextLabel, storageLabel;
        private List<SessionSummary> cloudSessions = new List<SessionSummary>();
        private string cloudError;
        private bool cloudLoading;
        public bool IsLoadingCloudHistory => cloudLoading;
        private readonly HashSet<string> pendingDeletion = new HashSet<string>();
        private List<SessionSummary> sessions;
        private List<SessionSummary> reviewSessions;
        private readonly HashSet<string> selectedSessions = new HashSet<string>();
        private int metric, page;
        private bool all;
        private const int PageSize = 10;
        private static readonly string[] Titles = { "OVERALL ACCURACY", "AVERAGE ROM", "SCORE", "MOVEMENT ACCURACY" };

        private void Start()
        {
            cloudLoading = Application.isPlaying && !PatientContext.IsGuest && PatientDirectory.Instance;
            Reload();
            if (cloudLoading)
            {
                string owner = PatientContext.PatientId;
                PatientDirectory.Instance.LoadHistory(owner, (records, error) =>
                {
                    if (!this || owner != PatientContext.PatientId) return;
                    cloudSessions = records; cloudError = error; cloudLoading = false; Reload();
                });
            }
        }
        private void Reload()
        {
            sessions = SessionHistoryStore.Default.LoadForPatient(PatientContext.PatientId, out var error);
            var ids = new HashSet<string>(sessions.Select(s => s.sessionId));
            sessions.AddRange(cloudSessions.Where(s => s.patientId == PatientContext.PatientId && ids.Add(s.sessionId)));
            sessions = sessions.OrderByDescending(s => System.DateTimeOffset.Parse(s.endedUtc)).ToList();
            if (patientContextLabel) patientContextLabel.text = PatientContext.DisplayName + " · " + (PatientContext.IsGuest ? "Local guest history" : cloudLoading ? "Refreshing cloud history…" : cloudError ?? "Patient session history");
            if (storageLabel) storageLabel.text = PatientContext.IsGuest ? "Guest history · Saved on this computer" : "Patient history · Cloud records are read-only";
            reviewSessions = sessions.OrderByDescending(s => s.endedEarly).ToList();
            selectedSessions.IntersectWith(sessions.Select(s => s.sessionId));
            page = Mathf.Min(page, Mathf.Max(0, (sessions.Count - 1) / PageSize));
            overview.text = error != null ? "History could not be read. Your saved file has been preserved." :
                sessions.Count == 0 ? "Your journey starts with your first session." :
                $"{sessions.Count} SESSIONS  ·  {sessions.Sum(s => s.duration) / 60f:0.0} MIN PLAYED  ·  BEST OVERALL {sessions.Max(s => s.Accuracy):P1}";
            RefreshList(); ChooseMetric(metric);
            if (error != null) graphMessage.text = "History unavailable. Check access to the local history file.";
        }
        public void ChooseMetric(int index)
        {
            metric = Mathf.Clamp(index,0,3);
            for(int i=0;i<tabImages.Length;i++) tabImages[i].color = i==metric ? new Color(.5f,.18f,.85f) : new Color(.08f,.10f,.19f);
            graphTitle.text = Titles[metric] + "  ·  LAST 8 SESSIONS";
            foreach(var p in points) p.gameObject.SetActive(false);
            foreach(var p in segments) p.gameObject.SetActive(false);
            foreach(var p in valueLabels) p.gameObject.SetActive(false);
            foreach(var p in dateLabels) p.gameObject.SetActive(false);
            var data = sessions.Where(s => metric != 3 || s.heightAttempts > 0).Take(points.Length).Reverse().ToArray();
            bool measured = metric != 1;
            graphMessage.gameObject.SetActive(!measured || data.Length==0);
            graphMessage.text = metric==1 ? "Knee-joint ROM is not recorded.\nThe shin sensor currently controls game height." :
                metric==3 ? "No height-match attempts recorded yet.\nMove the mouse to a note height and press Space during a session." :
                "No sessions yet.\nPlay a session to see your progress here.";
            float max = metric==2 && data.Length>0 ? Mathf.Max(1000, Mathf.Ceil(data.Max(s=>s.score)/1000f)*1000) : 100;
            for(int i=0;i<axisLabels.Length;i++) axisLabels[i].text = metric==1 ? "—" : (max*i/4f).ToString("0") + (metric==2?"":"%");
            graphRange.text = metric==3 ? "Mouse height-match rate among timed hit attempts. No sensor data." :
                metric==1 ? "Sensor measurement unavailable in the mouse prototype." : "Oldest → newest  ·  Early-ended sessions are marked in the list.";
            if (!measured) return;
            for(int i=0;i<data.Length;i++)
            {
                var s=data[i];
                float value=metric==2?s.score:metric==3?100f*s.correctHeightAttempts/s.heightAttempts:100*s.Accuracy;
                Vector2 pos=new Vector2(data.Length==1?450:900f*i/(data.Length-1),400*value/max);
                points[i].anchoredPosition=pos; points[i].gameObject.SetActive(true);
                valueLabels[i].rectTransform.anchoredPosition=pos+new Vector2(0,30);
                valueLabels[i].text=value.ToString(metric==2?"N0":"0")+(metric==2?"":"%"); valueLabels[i].gameObject.SetActive(true);
                dateLabels[i].rectTransform.anchoredPosition=new Vector2(pos.x,-35);
                dateLabels[i].text=DateTimeOffset.Parse(s.endedUtc).ToLocalTime().ToString("dd MMM");dateLabels[i].gameObject.SetActive(true);
                if(i==0)continue;
                Vector2 a=points[i-1].anchoredPosition, delta=pos-a;
                segments[i-1].anchoredPosition=(a+pos)/2;
                segments[i-1].sizeDelta=new Vector2(delta.magnitude,4);
                segments[i-1].localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                segments[i-1].gameObject.SetActive(true);
            }
        }
        public void ToggleAll() {all=!all;page=0;RefreshList();}
        public void Previous() {page=Mathf.Max(0,page-1);RefreshList();}
        public void Next() {page=Mathf.Min(Mathf.Max(0,(sessions.Count-1)/PageSize),page+1);RefreshList();}
        public void ToggleSessionSelection(int row)
        {
            if (deleteConfirmation && deleteConfirmation.activeSelf) return;
            int index = page * PageSize + row;
            if (!all || row < 0 || row >= PageSize || index >= reviewSessions.Count) return;
            string id = reviewSessions[index].sessionId;
            if (reviewSessions[index].isCloudRecord) return;
            if (!selectedSessions.Add(id)) selectedSessions.Remove(id);
            RefreshList(resetScroll: false);
        }
        public void RequestDelete()
        {
            if (!all || selectedSessions.Count == 0 || !deleteConfirmation) return;
            pendingDeletion.Clear();
            pendingDeletion.UnionWith(selectedSessions);
            deletePrompt.text = $"Delete {pendingDeletion.Count} selected local session record{(pendingDeletion.Count == 1 ? "" : "s")}?\n\nThis cannot be undone.";
            deleteConfirmation.SetActive(true);
        }
        public void CancelDelete()
        {
            pendingDeletion.Clear();
            deleteConfirmation.SetActive(false);
        }
        public void ConfirmDelete()
        {
            if (!deleteConfirmation.activeSelf || pendingDeletion.Count == 0) return;
            if (!SessionHistoryStore.Default.TryDelete(pendingDeletion, out var error))
            {
                CancelDelete();
                pageLabel.text = "Could not delete records. Please try again.\n" + error;
                return;
            }
            selectedSessions.ExceptWith(pendingDeletion);
            CancelDelete();
            Reload();
        }
        private void RefreshList(bool resetScroll = true)
        {
            var list = all ? reviewSessions : sessions;
            int limit=all?PageSize:4, start=all?page*PageSize:0;
            for(int i=0;i<rows.Length;i++)
            {
                int n=start+i; bool show=i<limit&&n<list.Count;
                var row = rows[i].transform.parent;
                row.gameObject.SetActive(show);
                if(!show)continue;
                var s=list[n];
                bool selected = all && selectedSessions.Contains(s.sessionId);
                if (selectionOutlines != null && i < selectionOutlines.Length && selectionOutlines[i]) selectionOutlines[i].enabled = selected;
                if (row.TryGetComponent<Button>(out var select)) select.interactable = all && !s.isCloudRecord;
                string accuracy=!s.HasAccuracy?"—":s.Accuracy.ToString("P1");
                string noteCount = s.isCloudRecord && s.totalNotes == 0 ? "CLOUD HISTORY" : $"{s.Judged}/{s.totalNotes} NOTES";
                rows[i].text=$"<color=#AF8BF5>{DateTimeOffset.Parse(s.endedUtc).ToLocalTime():dd MMM yyyy  HH:mm}</color>\n"+
                    $"{s.songTitle}\n<color=#76D8F3>{s.modeLabel}</color>\n"+
                    $"{accuracy} OVERALL  ·  {s.Completion:P0} COMPLETE\n"+
                    $"<color=#9DA7BF>{TimeSpan.FromSeconds(s.duration):mm\\:ss}  ·  {noteCount}  ·  "+
                    (s.endedEarly?"ENDED EARLY":"COMPLETED")+(s.isCloudRecord?" · CLOUD":"")+"</color>\n"+
                    $"<color=#C5CBE0>{SessionWellbeing.ScenarioLabel(s.wellbeing)}</color>";
            }
            listTitle.text=all?"NEEDS REVIEW":sessions.Count==0?"NO RECENT SESSIONS":"RECENT SESSIONS";
            if (uploadSelectedButton) uploadSelectedButton.gameObject.SetActive(all);
            if (deleteSelectedButton)
            {
                deleteSelectedButton.gameObject.SetActive(all);
                deleteSelectedButton.interactable = selectedSessions.Count > 0;
                var reviewButton = (RectTransform)viewAllLabel.transform.parent;
                reviewButton.anchoredPosition = new Vector2(all ? -140 : 0, reviewButton.anchoredPosition.y);
                reviewButton.sizeDelta = new Vector2(all ? 254 : 520, reviewButton.sizeDelta.y);
            }
            pageLabel.text=all ? $"{selectedSessions.Count} SELECTED  ·  PAGE {page+1}/{Mathf.Max(1,(sessions.Count+9)/10)}\nAccount link required for upload" :
                sessions.Count==0 ? "Complete a session to begin your history." : "Your 4 most recent sessions";
            previousButton.interactable=all&&page>0;
            nextButton.interactable=all&&(page+1)*PageSize<sessions.Count;
            viewAllLabel.text=all?"SHOW RECENT":"REVIEW QUEUE ("+sessions.Count+")";
            if (resetScroll) { Canvas.ForceUpdateCanvases(); sessionScroll.verticalNormalizedPosition=1; }
        }
    }
}
