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
        private List<SessionSummary> sessions;
        private int metric, page;
        private bool all;
        private const int PageSize = 10;
        private static readonly string[] Titles = { "OVERALL ACCURACY", "AVERAGE ROM", "SCORE", "MOVEMENT ACCURACY" };

        private void Start()
        {
            sessions = SessionHistoryStore.Default.Load(out var error);
            overview.text = error != null ? "History could not be read. Your saved file has been preserved." :
                sessions.Count == 0 ? "Your journey starts with your first session." :
                $"{sessions.Count} SESSIONS  ·  {sessions.Sum(s => s.duration) / 60f:0.0} MIN PLAYED  ·  BEST SCORE {sessions.Max(s => s.score):N0}";
            RefreshList(); ChooseMetric(0);
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
            graphMessage.text = metric==1 ? "ROM is not measured yet.\nConnect a calibrated sensor to record leg angles." :
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
        private void RefreshList()
        {
            int limit=all?PageSize:4, start=all?page*PageSize:0;
            for(int i=0;i<rows.Length;i++)
            {
                int n=start+i; bool show=i<limit&&n<sessions.Count;
                rows[i].transform.parent.gameObject.SetActive(show);
                if(!show)continue;
                var s=sessions[n];
                string accuracy=s.Judged==0?"—":s.Accuracy.ToString("P0");
                rows[i].text=$"<color=#AF8BF5>{DateTimeOffset.Parse(s.endedUtc).ToLocalTime():dd MMM yyyy  HH:mm}</color>\n"+
                    $"{s.songTitle}\n<color=#76D8F3>{s.modeLabel}  ·  {s.bpm:0} BPM</color>\n"+
                    $"{accuracy} ACCURACY  ·  {s.score:N0} PTS\n"+
                    $"<color=#9DA7BF>{TimeSpan.FromSeconds(s.duration):mm\\:ss}  ·  {s.Judged}/{s.totalNotes} NOTES  ·  "+
                    (s.endedEarly?"ENDED EARLY":"COMPLETED")+"</color>";
            }
            listTitle.text=sessions.Count==0?"NO RECENT SESSIONS":all?"ALL SESSIONS":"RECENT SESSIONS";
            pageLabel.text=sessions.Count==0?"Complete a session to begin your history.":all?$"PAGE {page+1} / {Mathf.Max(1,(sessions.Count+9)/10)}  ·  SCROLL TO BROWSE":"Your 4 most recent sessions";
            previousButton.interactable=all&&page>0;
            nextButton.interactable=all&&(page+1)*PageSize<sessions.Count;
            viewAllLabel.text=all?"SHOW RECENT":"VIEW ALL ("+sessions.Count+")";
            Canvas.ForceUpdateCanvases(); sessionScroll.verticalNormalizedPosition=1;
        }
    }
}
