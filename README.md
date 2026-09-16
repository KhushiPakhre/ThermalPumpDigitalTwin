# 🏭 Digital Twin of Pumps — Thermal Power Generation Plant

[![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-black?logo=unity)](https://unity.com)
[![C#](https://img.shields.io/badge/C%23-10.0-blue?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A real-time **Digital Twin** simulation of critical pumps in a Thermal Power Generation Plant, built with Unity 2022.3 LTS. The application provides interactive 3D visualization, live sensor telemetry, fault detection, predictive maintenance, and data logging.

---

## 📸 Features

| Feature | Description |
|---------|-------------|
| 🏗️ **3D Plant Visualization** | Procedurally built pump models with animated impellers, color-coded status |
| 📊 **Real-Time Sensor Dashboard** | Live gauges for RPM, flow, pressure, temperature, vibration |
| ⚡ **Fault Injection** | Simulate cavitation, bearing wear, overheating, seal leaks, dead-head |
| 🔮 **Predictive Maintenance** | Health score + Remaining Useful Life (RUL) via regression |
| 🚨 **Anomaly Detection** | Rule-based fault detection with toast alerts and history log |
| 📈 **Time-Series Graph** | Scrolling real-time graph of last 120 sensor readings |
| 💾 **Data Logging** | JSONL + CSV export of all sensor readings |
| 🎮 **Camera Controls** | Orbit (LMB), Pan (MMB), Zoom (scroll) |

---

## 🏗️ Architecture

```
ThermalPumpDigitalTwin/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── PumpTwin.cs            ← Domain model: state, KPI computation
│   │   │   ├── PumpSimulator.cs       ← Perlin-noise sensor data generator
│   │   │   ├── TwinManager.cs         ← Singleton managing all twins
│   │   │   ├── DataLogger.cs          ← JSONL/CSV persistence
│   │   │   ├── PumpVisual3D.cs        ← 3D primitive pump model + animation
│   │   │   ├── SceneSetup.cs          ← Programmatic scene wiring
│   │   │   └── CameraController.cs   ← Orbit/pan/zoom camera
│   │   ├── Sensors/
│   │   │   └── SensorBase.cs          ← All 5 sensors (RPM, Flow, Pressure×2, Temp, Vib)
│   │   ├── Anomaly/
│   │   │   ├── FaultCodes.cs          ← Fault enums & AnomalyEvent
│   │   │   ├── AnomalyDetector.cs     ← Rule-based fault detection
│   │   │   └── MaintenanceScheduler.cs ← RUL via linear regression
│   │   └── UI/
│   │       ├── PumpDashboard.cs       ← Main HUD controller
│   │       ├── GaugeWidget.cs         ← Animated circular gauge
│   │       ├── AlertPanel.cs          ← Toast + alert history
│   │       ├── TimeSeriesGraph.cs     ← GPU-drawn scrolling graph
│   │       └── FaultInjectionPanel.cs ← Debug/demo fault buttons
│   └── Scenes/
│       ├── MainPlant.unity            ← Plant floor with 3 pumps
│       └── PumpDetail.unity           ← Close-up pump inspection
├── Packages/manifest.json             ← TextMeshPro, URP, InputSystem
├── ProjectSettings/
└── .gitignore
```

---

## 🚀 Getting Started

### Prerequisites

- **Unity 2022.3 LTS** (download from [unity.com](https://unity.com/download))
- Git

### Installation

```bash
git clone https://github.com/<your-username>/ThermalPumpDigitalTwin.git
```

1. Open **Unity Hub** → **Add project from disk** → select the `ThermalPumpDigitalTwin` folder
2. Unity will import packages automatically (TextMeshPro, URP)
3. Open scene: `Assets/Scenes/MainPlant.unity`
4. Press ▶ **Play**

---

## 🖱️ Controls

| Action | Input |
|--------|-------|
| Orbit camera | Left Mouse Button + drag |
| Pan camera | Middle Mouse Button + drag |
| Zoom | Scroll wheel |
| Select pump | Click on pump model |
| Navigate to detail | Click pump → PumpDetail scene loads |

---

## 📐 Pump Parameters

### Boiler Feed Water Pump (BFP)

| Parameter | Nominal | Warning | Critical |
|-----------|---------|---------|---------|
| Speed | 2950 RPM | <2655 / >3098 | <2360 / >3245 |
| Flow Rate | 900 m³/h | <702 | <495 |
| Inlet Pressure | 4.0 bar | <2.4 bar | <1.2 bar |
| Outlet Pressure | 165 bar | >184.8 bar | >194.7 bar |
| Temperature | 170°C | >183.6°C | >195.5°C |
| Vibration | <3 mm/s | >6 mm/s | >12 mm/s |

### Condenser Extraction Pump (CEP)

| Parameter | Nominal | Warning | Critical |
|-----------|---------|---------|---------|
| Speed | 1475 RPM | <1328 | <1180 |
| Flow Rate | 400 m³/h | <312 | <220 |
| Inlet Pressure | 0.8 bar | <0.48 bar | <0.24 bar |
| Outlet Pressure | 12 bar | >13.4 bar | >14.2 bar |
| Temperature | 45°C | >48.6°C | >51.8°C |
| Vibration | <2.8 mm/s | >5.6 mm/s | >11.2 mm/s |

---

## ⚙️ Fault Injection (Demo Mode)

Use the **Fault Injection Panel** (in-game UI) to simulate:

| Fault | Effect |
|-------|--------|
| **Cavitation** | Inlet pressure collapses, vibration spikes 3-5× |
| **Bearing Wear** | Progressive vibration increase, slight RPM drop |
| **Overheating** | Temperature ramps above limits |
| **Seal Leak** | Flow drops 40% while RPM stays constant |
| **Impeller Wear** | Reduced flow + outlet pressure |
| **Dead Head** | Zero flow while pump running |

---

## 📊 Health Score Algorithm

The health score (0–100%) is computed as a weighted combination:

```
HealthScore = (0.40 × EfficiencyScore + 0.35 × VibrationScore + 0.25 × TemperatureScore)
              × (1 - Degradation × 0.30)
```

---

## 🔮 Remaining Useful Life (RUL)

The `MaintenanceScheduler` uses a **sliding window linear regression** on the health score time series to extrapolate when health will fall below the maintenance threshold (default: 70%).

---

## 💾 Data Logging

Sensor readings are logged every second to:
- `Assets/StreamingAssets/logs/pump_log.jsonl` — line-delimited JSON
- Click **Export CSV** to generate `pump_export.csv`

---

## 🔌 Future Roadmap

- [ ] MQTT/OPC-UA real sensor integration
- [ ] ML-based anomaly detection (LSTM autoencoder)
- [ ] VR mode (Meta Quest 3 / OpenXR)
- [ ] Digital twin synchronisation via REST API
- [ ] Historical playback from CSV

---

## 📄 License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first.

---

*Built with Unity 2022.3 LTS | Digital Twin for Thermal Power Plant Pumps*
