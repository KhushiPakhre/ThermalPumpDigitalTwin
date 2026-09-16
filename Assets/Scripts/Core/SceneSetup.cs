using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ThermalPumpDT.Core;
using ThermalPumpDT.Anomaly;
using ThermalPumpDT.UI;
using ThermalPumpDT.Visualization;

namespace ThermalPumpDT
{
    /// <summary>
    /// Programmatically wires up the MainPlant scene at runtime:
    /// TwinManager, anomaly systems, 3D plant layout, and full HUD.
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        private void Start()
        {
            if (TwinManager.Instance == null)
            {
                var tm = new GameObject("TwinManager").AddComponent<TwinManager>();
                DontDestroyOnLoad(tm.gameObject);
            }

            if (!FindObjectOfType<AnomalyDetector>())
                new GameObject("AnomalyDetector").AddComponent<AnomalyDetector>();
            if (!FindObjectOfType<MaintenanceScheduler>())
                new GameObject("MaintenanceScheduler").AddComponent<MaintenanceScheduler>();
            if (!FindObjectOfType<DataLogger>())
                new GameObject("DataLogger").AddComponent<DataLogger>();

            BuildPlantLayout();
            SetupLighting();
            DashboardUIBuilder.Build();
        }

        private void BuildPlantLayout()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "FloorSlab";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(20f, 0.2f, 14f);
            floor.GetComponent<Renderer>().material = MaterialHelper.CreateLitMaterial(new Color(0.25f, 0.25f, 0.28f));

            SpawnPump("BFP-01", new Vector3(-4f, 0.1f, -2f));
            SpawnPump("BFP-02", new Vector3( 0f, 0.1f, -2f));
            SpawnPump("CEP-01", new Vector3( 4f, 0.1f,  2f));

            AddPipeHeader(new Vector3(-4f, 1.5f, -4f), new Vector3(8f, 0.25f, 0.25f));
            AddPipeHeader(new Vector3(-4f, 1.5f,  0f), new Vector3(8f, 0.25f, 0.25f));

            AddWall(new Vector3(-10f, 3f, 0f), new Vector3(0.3f, 6f, 14f));
            AddWall(new Vector3( 10f, 3f, 0f), new Vector3(0.3f, 6f, 14f));
        }

        private void SpawnPump(string pumpId, Vector3 position)
        {
            var go = new GameObject($"PumpAssembly_{pumpId}");
            go.transform.position = position;

            var visual = go.AddComponent<PumpVisual3D>();
            visual.PumpId = pumpId;

            var col = go.AddComponent<BoxCollider>();
            col.size   = new Vector3(2f, 1.8f, 1.5f);
            col.center = new Vector3(0f, 0.9f, 0f);

            go.AddComponent<PumpClickHandler>();
            CreateWorldSpaceLabel(go.transform, pumpId, new Vector3(0, 2.2f, 0));
        }

        private void CreateWorldSpaceLabel(Transform parent, string text, Vector3 localOffset)
        {
            var canvas = new GameObject("PumpLabel");
            canvas.transform.SetParent(parent);
            canvas.transform.localPosition = localOffset;
            canvas.transform.localScale    = Vector3.one * 0.01f;

            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            canvas.AddComponent<CanvasScaler>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvas.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale    = Vector3.one;
            var img = bg.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            var rect = bg.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);

            var lbl = new GameObject("Label");
            lbl.transform.SetParent(canvas.transform);
            var tmp = lbl.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);

            canvas.AddComponent<BillboardLabel>();
        }

        private void AddPipeHeader(Vector3 pos, Vector3 scale)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = "PipeHeader";
            p.transform.position   = pos;
            p.transform.localScale = scale;
            p.transform.rotation   = Quaternion.Euler(0, 0, 90);
            p.GetComponent<Renderer>().material =
                MaterialHelper.CreateLitMaterial(new Color(0.4f, 0.45f, 0.5f));
        }

        private void AddWall(Vector3 pos, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "Wall";
            w.transform.position   = pos;
            w.transform.localScale = scale;
            w.GetComponent<Renderer>().material =
                MaterialHelper.CreateLitMaterial(new Color(0.7f, 0.7f, 0.72f));
        }

        private void SetupLighting()
        {
            if (FindObjectOfType<Light>() != null) return;

            var dirLightGO = new GameObject("DirectionalLight");
            var dirLight   = dirLightGO.AddComponent<Light>();
            dirLight.type      = LightType.Directional;
            dirLight.intensity = 1.2f;
            dirLight.color     = new Color(1f, 0.95f, 0.85f);
            dirLightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0);

            RenderSettings.ambientMode           = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor       = new Color(0.4f, 0.5f, 0.7f);
            RenderSettings.ambientEquatorColor   = new Color(0.35f, 0.35f, 0.4f);
            RenderSettings.ambientGroundColor    = new Color(0.2f, 0.2f, 0.25f);
        }
    }

    public class BillboardLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
                transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                                 Camera.main.transform.rotation * Vector3.up);
        }
    }

    public class PumpClickHandler : MonoBehaviour
    {
        private PumpVisual3D _visual;
        private void Awake() => _visual = GetComponent<PumpVisual3D>();

        private void OnMouseDown()
        {
            if (_visual != null)
                TwinManager.Instance?.LoadDetailScene(_visual.PumpId);
        }
    }
}
