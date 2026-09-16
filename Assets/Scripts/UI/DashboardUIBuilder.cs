using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ThermalPumpDT.UI
{
    /// <summary>Programmatically builds the full HUD at runtime.</summary>
    public static class DashboardUIBuilder
    {
        private static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.14f, 0.92f);
        private static readonly Color Accent  = new Color(0.2f, 0.55f, 0.95f, 1f);

        public static PumpDashboard Build()
        {
            EnsureEventSystem();

            var canvasGO = new GameObject("DashboardCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            var dashboard = canvasGO.AddComponent<PumpDashboard>();

            // Left – pump list
            var leftPanel = CreatePanel(canvasGO.transform, "LeftPanel",
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10, 10), new Vector2(260, -10));
            leftPanel.GetComponent<Image>().color = PanelBg;
            AddHeader(leftPanel, "Pumps");
            var listContent = CreateScrollContent(leftPanel, "PumpList");
            dashboard.PumpListContainer = listContent;
            dashboard.PumpListItemPrefab  = CreateListItemPrefab();

            // Right – sensor panel
            var rightPanel = CreatePanel(canvasGO.transform, "RightPanel",
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-310, 10), new Vector2(-10, -10));
            rightPanel.GetComponent<Image>().color = PanelBg;
            AddHeader(rightPanel, "Sensor Readouts");

            dashboard.PumpNameLabel   = CreateLabel(rightPanel, "PumpName",   new Vector2(10, -50),  22, FontStyles.Bold);
            dashboard.PumpStatusLabel = CreateLabel(rightPanel, "PumpStatus", new Vector2(10, -80),  18, FontStyles.Normal);

            float gaugeY = -120f;
            dashboard.RPMGauge             = CreateGauge(rightPanel, "RPM",             new Vector2(10, gaugeY));
            dashboard.FlowGauge            = CreateGauge(rightPanel, "Flow",            new Vector2(160, gaugeY));
            dashboard.InletPressureGauge   = CreateGauge(rightPanel, "Inlet P",         new Vector2(10, gaugeY - 110));
            dashboard.OutletPressureGauge  = CreateGauge(rightPanel, "Outlet P",        new Vector2(160, gaugeY - 110));
            dashboard.TemperatureGauge     = CreateGauge(rightPanel, "Temp",            new Vector2(10, gaugeY - 220));
            dashboard.VibrationGauge     = CreateGauge(rightPanel, "Vibration",       new Vector2(160, gaugeY - 220));

            // Health block
            var healthGO = new GameObject("HealthBlock");
            healthGO.transform.SetParent(rightPanel, false);
            var healthRect = healthGO.AddComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 1);
            healthRect.anchorMax = new Vector2(1, 1);
            healthRect.pivot     = new Vector2(0.5f, 1);
            healthRect.anchoredPosition = new Vector2(0, -460);
            healthRect.sizeDelta = new Vector2(-20, 80);

            dashboard.HealthRingImage = CreateFilledImage(healthGO.transform, "HealthRing",
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(40, 0), new Vector2(70, 70));
            dashboard.HealthLabel   = CreateLabel(healthGO, "HealthLabel",   new Vector2(90, -10), 20, FontStyles.Bold);
            dashboard.RULLabel      = CreateLabel(healthGO, "RULLabel",      new Vector2(90, -35), 16, FontStyles.Normal);
            dashboard.RunHoursLabel = CreateLabel(healthGO, "RunHoursLabel", new Vector2(90, -58), 16, FontStyles.Normal);

            // Bottom – time series graph
            var graphPanel = CreatePanel(canvasGO.transform, "GraphPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-350, 10), new Vector2(350, 160));
            graphPanel.GetComponent<Image>().color = PanelBg;
            dashboard.Graph = graphPanel.gameObject.AddComponent<TimeSeriesGraph>();

            // Top – alert toast
            var alertPanel = CreatePanel(canvasGO.transform, "AlertPanel",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-300, -70), new Vector2(300, -10));
            var alerts = alertPanel.gameObject.AddComponent<AlertPanel>();
            alerts.ToastRoot       = alertPanel;
            alerts.ToastBackground = alertPanel.GetComponent<Image>();
            alerts.ToastMessage    = CreateLabel(alertPanel, "ToastMsg", Vector2.zero, 16, FontStyles.Normal);
            alerts.ToastMessage.alignment = TextAlignmentOptions.Center;
            alerts.ToastBackground.color  = new Color(0.85f, 0.1f, 0.1f, 0.9f);
            alertPanel.gameObject.SetActive(false);

            // Alert history (bottom-right)
            var historyPanel = CreatePanel(canvasGO.transform, "AlertHistory",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-310, 170), new Vector2(-10, 400));
            historyPanel.GetComponent<Image>().color = PanelBg;
            AddHeader(historyPanel, "Alert History");
            alerts.HistoryContainer       = CreateScrollContent(historyPanel, "HistoryList");
            alerts.AlertHistoryItemPrefab = CreateHistoryItemPrefab();

            dashboard.Alerts = alerts;

            // Fault injection panel (left-bottom)
            BuildFaultInjectionPanel(canvasGO.transform);

            return dashboard;
        }

        private static void BuildFaultInjectionPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "FaultInjection",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(10, 10), new Vector2(260, 200));
            panel.GetComponent<Image>().color = PanelBg;
            AddHeader(panel, "Fault Injection (Demo)");

            var faultPanel = panel.gameObject.AddComponent<FaultInjectionPanel>();
            faultPanel.ActiveFaultsLabel = CreateLabel(panel, "ActiveFaults", new Vector2(10, -45), 14, FontStyles.Italic);
            faultPanel.ActiveFaultsLabel.text = "No active faults";

            float y = -75f;
            faultPanel.CavitationBtn   = CreateButton(panel, "Cavitation",   new Vector2(10, y),      new Vector2(115, 28));
            faultPanel.BearingWearBtn  = CreateButton(panel, "Bearing Wear", new Vector2(135, y),     new Vector2(115, 28));
            faultPanel.OverheatBtn     = CreateButton(panel, "Overheat",     new Vector2(10, y - 32), new Vector2(115, 28));
            faultPanel.SealLeakBtn     = CreateButton(panel, "Seal Leak",    new Vector2(135, y - 32),new Vector2(115, 28));
            faultPanel.ImpellerWearBtn = CreateButton(panel, "Impeller",     new Vector2(10, y - 64), new Vector2(115, 28));
            faultPanel.DeadHeadBtn     = CreateButton(panel, "Dead Head",    new Vector2(135, y - 64),new Vector2(115, 28));
            faultPanel.ClearAllBtn     = CreateButton(panel, "Clear All",    new Vector2(10, y - 96), new Vector2(115, 28));
            faultPanel.MaintenanceBtn  = CreateButton(panel, "Maintenance",  new Vector2(135, y - 96),new Vector2(115, 28));
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot     = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            go.AddComponent<Image>().color = PanelBg;
            return rect;
        }

        private static void AddHeader(RectTransform panel, string text)
        {
            var lbl = CreateLabel(panel, "Header", new Vector2(10, -8), 18, FontStyles.Bold);
            lbl.text = text;
            lbl.color = Accent;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 pos, int size, FontStyles style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot     = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(-20, 28);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = size;
            tmp.fontStyle = style;
            tmp.color     = Color.white;
            return tmp;
        }

        private static Image CreateFilledImage(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot     = new Vector2(0, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.type      = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillAmount = 1f;
            img.color     = new Color(0.2f, 0.8f, 0.3f);
            return img;
        }

        private static GaugeWidget CreateGauge(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject($"Gauge_{name}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot     = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(140, 100);

            var widget = go.AddComponent<GaugeWidget>();
            widget.NameLabel = CreateSmallLabel(go.transform, name, new Vector2(0, 0), 13);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(go.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(0, 20);
            fillRect.offsetMax = new Vector2(0, -10);
            widget.FillImage = fillGO.AddComponent<Image>();
            widget.FillImage.type = Image.Type.Filled;
            widget.FillImage.fillMethod = Image.FillMethod.Horizontal;
            widget.FillImage.color = new Color(0.2f, 0.8f, 0.3f);

            widget.ValueLabel = CreateSmallLabel(go.transform, "0.0", new Vector2(0, -75), 16);
            widget.UnitLabel  = CreateSmallLabel(go.transform, "",    new Vector2(60, -75), 12);

            return widget;
        }

        private static TextMeshProUGUI CreateSmallLabel(Transform parent, string text, Vector2 pos, int size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot     = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(0, 20);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot     = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.3f, 0.4f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var lblGO = new GameObject("Text");
            lblGO.transform.SetParent(go.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static Transform CreateScrollContent(RectTransform panel, string name)
        {
            var scrollGO = new GameObject(name);
            scrollGO.transform.SetParent(panel, false);
            var scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -40);

            scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            scrollGO.AddComponent<Mask>().showMaskGraphic = false;
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var content = new GameObject("Content");
            content.transform.SetParent(scrollGO.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot     = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = scrollRect;
            return content.transform;
        }

        private static GameObject CreateListItemPrefab()
        {
            var go = new GameObject("PumpListItem");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 36);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.22f, 0.3f);
            go.AddComponent<Button>();

            var lblGO = new GameObject("Text");
            lblGO.transform.SetParent(go.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = new Vector2(8, 0);
            lblRect.offsetMax = new Vector2(-8, 0);
            lblGO.AddComponent<TextMeshProUGUI>().fontSize = 14;

            go.SetActive(false);
            return go;
        }

        private static GameObject CreateHistoryItemPrefab()
        {
            var go = new GameObject("AlertHistoryItem");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);
            go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

            var lblGO = new GameObject("Text");
            lblGO.transform.SetParent(go.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = new Vector2(6, 2);
            lblRect.offsetMax = new Vector2(-6, -2);
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 11;
            tmp.enableWordWrapping = true;

            go.SetActive(false);
            return go;
        }
    }
}
