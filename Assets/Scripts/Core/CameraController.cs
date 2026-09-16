using UnityEngine;

namespace ThermalPumpDT
{
    /// <summary>
    /// Orbit / pan / zoom camera controller for the 3D plant view.
    /// LMB drag = orbit, MMB drag = pan, scroll = zoom.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        public Vector3 Target = Vector3.zero;

        [Header("Settings")]
        public float OrbitSpeed = 200f;
        public float PanSpeed   = 0.5f;
        public float ZoomSpeed  = 5f;
        public float MinDistance = 2f;
        public float MaxDistance = 40f;

        private float _distance = 12f;
        private float _yaw   = 30f;
        private float _pitch = 35f;

        private void Start() => ApplyTransform();

        private void Update()
        {
            // Orbit
            if (Input.GetMouseButton(0))
            {
                _yaw   += Input.GetAxis("Mouse X") * OrbitSpeed * Time.deltaTime;
                _pitch -= Input.GetAxis("Mouse Y") * OrbitSpeed * Time.deltaTime;
                _pitch  = Mathf.Clamp(_pitch, 5f, 85f);
            }

            // Pan
            if (Input.GetMouseButton(2))
            {
                Target -= transform.right   * Input.GetAxis("Mouse X") * PanSpeed;
                Target -= transform.up      * Input.GetAxis("Mouse Y") * PanSpeed;
            }

            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _distance = Mathf.Clamp(_distance - scroll * ZoomSpeed, MinDistance, MaxDistance);

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            transform.position  = Target + rotation * new Vector3(0, 0, -_distance);
            transform.LookAt(Target);
        }
    }
}
