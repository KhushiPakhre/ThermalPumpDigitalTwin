using UnityEngine;
using ThermalPumpDT.Core;

namespace ThermalPumpDT.Visualization
{
    /// <summary>
    /// MonoBehaviour that procedurally builds a 3D pump model from Unity primitives
    /// and animates it based on live PumpTwin state (impeller rotation, color coding).
    /// </summary>
    public class PumpVisual3D : MonoBehaviour
    {
        [Header("Twin")]
        public string PumpId;

        [Header("Animation")]
        public float MaxImpellerRPM = 3000f;

        // ── Materials (assigned at runtime or via Inspector) ────────
        private Material _bodyMat;
        private Material _pipeMat;

        // ── Transform references ────────────────────────────────────
        private Transform _impeller;
        private Transform _statusLight;
        private Renderer  _statusLightRenderer;

        // ── Colors ─────────────────────────────────────────────────
        private static readonly Color ColorNormal   = new Color(0.18f, 0.72f, 0.3f);
        private static readonly Color ColorWarning  = new Color(1f,    0.65f, 0f);
        private static readonly Color ColorCritical = new Color(0.85f, 0.1f,  0.1f);
        private static readonly Color ColorOffline  = Color.gray;

        // ── Live state ──────────────────────────────────────────────
        private float _targetRPM;
        private float _currentRotSpeed;
        private PumpStatus _status = PumpStatus.Offline;

        // ─────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────

        private void Start()
        {
            BuildModel();
            TwinManager.OnAnyPumpStateUpdated += OnStateUpdate;
        }

        private void OnDestroy() => TwinManager.OnAnyPumpStateUpdated -= OnStateUpdate;

        private void Update()
        {
            if (_impeller == null) return;

            // Smooth RPM tracking
            float targetSpeed = (_targetRPM / MaxImpellerRPM) * 720f; // deg/s
            _currentRotSpeed  = Mathf.Lerp(_currentRotSpeed, targetSpeed, Time.deltaTime * 2f);
            _impeller.Rotate(Vector3.up, _currentRotSpeed * Time.deltaTime, Space.Self);

            // Status light pulse for Warning / Critical
            if (_statusLightRenderer && _status == PumpStatus.Warning)
            {
                float pulse = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                _statusLightRenderer.material.SetColor("_EmissionColor", ColorWarning * pulse * 3f);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Event handling
        // ─────────────────────────────────────────────────────────

        private void OnStateUpdate(PumpTwin twin, PumpState state)
        {
            if (twin.PumpId != PumpId) return;
            _targetRPM = state.RPM;
            _status    = state.Status;
            ApplyStatusColor(state.Status);
        }

        private void ApplyStatusColor(PumpStatus status)
        {
            if (_bodyMat == null) return;
            Color c = status switch
            {
                PumpStatus.Normal   => ColorNormal,
                PumpStatus.Warning  => ColorWarning,
                PumpStatus.Critical => ColorCritical,
                _                   => ColorOffline
            };
            _bodyMat.color = c;
            if (_statusLightRenderer)
                _statusLightRenderer.material.SetColor("_EmissionColor", c * 2f);
        }

        // ─────────────────────────────────────────────────────────
        //  Procedural 3D model construction
        // ─────────────────────────────────────────────────────────

        private void BuildModel()
        {
            _bodyMat = MaterialHelper.CreateLitMaterial(ColorOffline);
            var pipeMat = MaterialHelper.CreateLitMaterial(new Color(0.4f, 0.5f, 0.6f));

            // Pump volute (main housing)
            var volute = CreatePart("Volute", PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0),
                                    new Vector3(1.0f, 0.25f, 1.0f), _bodyMat);

            // Motor housing
            CreatePart("Motor", PrimitiveType.Cylinder, new Vector3(0, 0.9f, 0),
                        new Vector3(0.7f, 0.5f, 0.7f), _bodyMat);

            // Suction pipe (horizontal, left)
            CreatePart("SuctionPipe", PrimitiveType.Cylinder, new Vector3(-1.2f, 0.2f, 0),
                        new Vector3(0.2f, 0.8f, 0.2f), pipeMat,
                        Quaternion.Euler(0, 0, 90));

            // Discharge pipe (vertical)
            CreatePart("DischargePipe", PrimitiveType.Cylinder, new Vector3(0.8f, 0.8f, 0),
                        new Vector3(0.2f, 0.6f, 0.2f), pipeMat,
                        Quaternion.Euler(0, 0, 30));

            // Bearing housing
            CreatePart("BearingHousing", PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0),
                        new Vector3(0.85f, 0.12f, 0.85f), pipeMat);

            // Shaft coupling
            CreatePart("ShaftCoupling", PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0),
                        new Vector3(0.2f, 0.15f, 0.2f), pipeMat);

            // Impeller (inside volute, rotates)
            var impellerGO = CreatePart("Impeller", PrimitiveType.Sphere, new Vector3(0, 0.22f, 0),
                                        new Vector3(0.7f, 0.15f, 0.7f), _bodyMat);
            _impeller = impellerGO.transform;

            // Impeller blades (6 blades as cubes)
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                var blade = CreatePart($"Blade{i}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * 0.28f, 0.22f, Mathf.Sin(angle) * 0.28f),
                    new Vector3(0.06f, 0.12f, 0.3f), _bodyMat,
                    Quaternion.Euler(0, i * 60f, 0));
                blade.transform.SetParent(_impeller, true);
            }

            // Status light
            var lightGO = new GameObject("StatusLight");
            lightGO.transform.SetParent(transform);
            lightGO.transform.localPosition = new Vector3(0, 1.45f, 0);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(lightGO.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale    = Vector3.one * 0.12f;
            var lightMat = MaterialHelper.CreateLitMaterial(ColorOffline);
            lightMat.EnableKeyword("_EMISSION");
            if (lightMat.HasProperty("_EmissionColor"))
                lightMat.SetColor("_EmissionColor", ColorOffline * 2f);
            sphere.GetComponent<Renderer>().material = lightMat;
            _statusLightRenderer = sphere.GetComponent<Renderer>();
            _statusLight = lightGO.transform;

            // Base plate
            CreatePart("BasePlate", PrimitiveType.Cube, new Vector3(0, -0.08f, 0),
                        new Vector3(2f, 0.1f, 1.4f), pipeMat);
        }

        private GameObject CreatePart(string partName, PrimitiveType type, Vector3 localPos,
                                       Vector3 localScale, Material mat, Quaternion? localRot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = partName;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.GetComponent<Renderer>().material = mat;

            // Remove colliders from decoration parts except base
            if (partName != "BasePlate" && partName != "Volute")
                Destroy(go.GetComponent<Collider>());

            return go;
        }
    }
}
