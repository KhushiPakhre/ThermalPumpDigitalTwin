using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ThermalPumpDT.UI
{
    /// <summary>Reusable circular gauge widget driven by normalized value 0-1.</summary>
    public class GaugeWidget : MonoBehaviour
    {
        [Header("UI References")]
        public Image  FillImage;
        public Image  NeedleImage;
        public TextMeshProUGUI ValueLabel;
        public TextMeshProUGUI UnitLabel;
        public TextMeshProUGUI NameLabel;

        [Header("Colors")]
        public Color NormalColor  = new Color(0.2f, 0.8f, 0.3f);
        public Color WarningColor = new Color(1f,   0.7f, 0f);
        public Color CriticalColor = new Color(0.9f, 0.1f, 0.1f);

        [Header("Thresholds (normalised 0-1)")]
        [Range(0,1)] public float WarningThreshold  = 0.75f;
        [Range(0,1)] public float CriticalThreshold = 0.90f;

        private float _targetFill;

        private void Update()
        {
            if (FillImage == null) return;
            FillImage.fillAmount = Mathf.Lerp(FillImage.fillAmount, _targetFill, Time.deltaTime * 5f);

            if (NeedleImage)
                NeedleImage.rectTransform.localEulerAngles =
                    new Vector3(0, 0, Mathf.Lerp(135f, -135f, FillImage.fillAmount));
        }

        /// <summary>Push a new sensor reading.</summary>
        public void UpdateValue(float value, float nominal, float maxValue, string unit = "")
        {
            float norm = Mathf.Clamp01(value / Mathf.Max(maxValue, 0.001f));
            _targetFill = norm;

            if (ValueLabel) ValueLabel.text = $"{value:F1}";
            if (UnitLabel && !string.IsNullOrEmpty(unit)) UnitLabel.text = unit;

            Color c = norm >= CriticalThreshold ? CriticalColor :
                      norm >= WarningThreshold  ? WarningColor  :
                                                  NormalColor;
            if (FillImage) FillImage.color = c;
        }
    }
}
