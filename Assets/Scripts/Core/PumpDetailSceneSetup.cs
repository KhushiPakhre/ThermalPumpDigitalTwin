using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using ThermalPumpDT.Core;
using ThermalPumpDT.UI;
using ThermalPumpDT.Visualization;

namespace ThermalPumpDT
{
    /// <summary>Wires up the PumpDetail scene with selected pump model and HUD.</summary>
    public class PumpDetailSceneSetup : MonoBehaviour
    {
        private void Start()
        {
            EnsureEventSystem();
            EnsureSystems();

            var twin = TwinManager.Instance?.SelectedTwin;
            if (twin == null)
            {
                Debug.LogWarning("[PumpDetail] No pump selected – returning to plant.");
                TwinManager.Instance?.LoadPlantScene();
                return;
            }

            BuildScene(twin);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureSystems()
        {
            if (TwinManager.Instance == null)
            {
                var tm = new GameObject("TwinManager").AddComponent<TwinManager>();
                DontDestroyOnLoad(tm.gameObject);
            }
        }

        private void BuildScene(PumpTwin twin)
        {
            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "DetailFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
            floor.GetComponent<Renderer>().material = MaterialHelper.CreateLitMaterial(new Color(0.25f, 0.25f, 0.28f));

            // Pump model
            var pumpGO = new GameObject($"PumpDetail_{twin.PumpId}");
            pumpGO.transform.position = new Vector3(0, 0.1f, 0);
            var visual = pumpGO.AddComponent<PumpVisual3D>();
            visual.PumpId = twin.PumpId;

            // Lighting
            var lightGO = new GameObject("DirectionalLight");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0);

            // Camera orbit
            var cam = Camera.main;
            if (cam != null)
            {
                var ctrl = cam.gameObject.AddComponent<CameraController>();
                ctrl.Target    = new Vector3(0, 1f, 0);
                ctrl.MinDistance = 3f;
                ctrl.MaxDistance = 15f;
            }

            BuildDetailHUD(twin);
        }

        private static void BuildDetailHUD(PumpTwin twin)
        {
            var canvasGO = new GameObject("DetailHUD");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Title bar
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(canvasGO.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot     = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -10);
            titleRect.sizeDelta = new Vector2(-20, 50);
            titleGO.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.9f);
            var titleLbl = new GameObject("TitleText");
            titleLbl.transform.SetParent(titleGO.transform, false);
            var tlRect = titleLbl.AddComponent<RectTransform>();
            tlRect.anchorMin = Vector2.zero;
            tlRect.anchorMax = Vector2.one;
            tlRect.offsetMin = new Vector2(60, 0);
            tlRect.offsetMax = new Vector2(-10, 0);
            var tmp = titleLbl.AddComponent<TextMeshProUGUI>();
            tmp.text      = $"Detail View — {twin.PumpName} ({twin.PumpId})";
            tmp.fontSize  = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.white;

            // Back button
            var backBtn = CreateButton(titleGO.transform, "← Back to Plant",
                new Vector2(10, 5), new Vector2(140, 40));
            backBtn.onClick.AddListener(() => TwinManager.Instance.LoadPlantScene());

            // Full dashboard (reuses builder)
            DashboardUIBuilder.Build();
        }

        private static Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot     = new Vector2(0, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.2f, 0.55f, 0.95f);
            var btn = go.AddComponent<Button>();
            var lblGO = new GameObject("Text");
            lblGO.transform.SetParent(go.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return btn;
        }
    }
}
