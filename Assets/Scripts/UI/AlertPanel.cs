using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ThermalPumpDT.Core;

namespace ThermalPumpDT.UI
{
    /// <summary>Toast-style alert system with history scroll list.</summary>
    public class AlertPanel : MonoBehaviour
    {
        [Header("Toast")]
        public RectTransform ToastRoot;
        public TextMeshProUGUI ToastMessage;
        public Image ToastBackground;
        public float ToastDuration = 5f;

        [Header("Alert History")]
        public Transform HistoryContainer;
        public GameObject AlertHistoryItemPrefab;
        public int MaxHistoryItems = 50;

        [Header("Colors")]
        public Color WarningBg  = new Color(1f,   0.85f, 0f,   0.9f);
        public Color CriticalBg = new Color(0.85f, 0.1f,  0.1f, 0.9f);
        public Color InfoBg     = new Color(0.2f,  0.6f,  1f,   0.9f);

        private readonly List<GameObject> _historyItems = new();
        private Coroutine _toastCoroutine;

        public void ShowAlert(AnomalyEvent evt)
        {
            ShowToast(evt);
            AddToHistory(evt);
        }

        private void ShowToast(AnomalyEvent evt)
        {
            if (ToastRoot == null) return;
            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);

            ToastRoot.gameObject.SetActive(true);
            if (ToastMessage)  ToastMessage.text = evt.Description;
            if (ToastBackground) ToastBackground.color = SeverityColor(evt.Severity);

            _toastCoroutine = StartCoroutine(HideToastAfter(ToastDuration));
        }

        private IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            ToastRoot?.gameObject.SetActive(false);
        }

        private void AddToHistory(AnomalyEvent evt)
        {
            if (HistoryContainer == null || AlertHistoryItemPrefab == null) return;
            var go = Instantiate(AlertHistoryItemPrefab, HistoryContainer);
            go.SetActive(true);

            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = $"[{evt.Severity}] {evt.Fault}: {evt.Description}";

            var bg = go.GetComponent<Image>();
            if (bg) bg.color = SeverityColor(evt.Severity);

            _historyItems.Add(go);
            if (_historyItems.Count > MaxHistoryItems)
            {
                Destroy(_historyItems[0]);
                _historyItems.RemoveAt(0);
            }
        }

        private Color SeverityColor(FaultSeverity s) => s switch
        {
            FaultSeverity.Critical => CriticalBg,
            FaultSeverity.Warning  => WarningBg,
            _                      => InfoBg
        };
    }
}
