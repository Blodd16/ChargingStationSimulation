# ⚡ Charging Station Simulation
<img width="1907" height="975" alt="Charging" src="https://github.com/user-attachments/assets/424e8ea1-128c-49ed-a2a5-e6cb9b2fd11c" />


<div align="center">

![Avalonia UI](https://img.shields.io/badge/Avalonia_UI-11.x-blueviolet?style=for-the-badge&logo=dotnet)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows%20|%20Linux%20|%20macOS-lightgrey?style=for-the-badge)

**A real-time animated EV charging station simulator built with Avalonia UI.**  
Vehicles drive in, park, charge, and drive out — fully animated and simulated.

[Features](#-features) • [Screenshots](#-screenshots) • [Getting Started](#-getting-started) • [Architecture](#-architecture) • [Usage](#-usage)

</div>

---

## ✨ Features

- 🚗 **Real vehicle animations** — cars drive in, park at charging spots, and leave when done
- ⚡ **Live charging simulation** — battery levels fill up in real time with color indicators
- 🔄 **Dual orientation modes** — switch between **Horizontal** and **Vertical** station layout
- 📊 **Live statistics display** — power output (kW), utilization (%), slots used
- 🟡 **Queue system** — vehicles wait in queue and auto-assign to free spots
- 🎨 **Smooth animations** — cubic ease-in-out transitions for all vehicle movements
- 🌙 **Dark UI theme** — professional dark design with glowing green accents
- 🖥️ **Cross-platform** — runs on Windows, Linux, and macOS via Avalonia UI
---
---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/)
- Avalonia UI extension (for Visual Studio)

  <img width="1908" height="978" alt="ChargingS" src="https://github.com/user-attachments/assets/e33d958e-c244-487b-b123-14b1db4af5d8" />


## 🏗️ Architecture

```
ChargingStationSimulation/
│
├── Models/
│   ├── ChargingStation.cs      # Station logic — capacity, queue, slots
│   ├── Vehicle.cs              # Vehicle model — battery, type, charging state
│   └── SimulationEngine.cs     # Core simulation loop & event dispatcher
│
├── Views/
│   ├── AnimatedStationView.axaml       # Station UI layout (XAML)
│   ├── AnimatedStationView.axaml.cs    # Animation logic — drive in/out, charging
│   └── MainWindow.axaml                # Main application window
│
├── ViewModels/
│   └── MainViewModel.cs        # MVVM bindings & simulation state
│
└── Assets/
    └── Icons/                  # Vehicle and UI icons
```

### Key Components

#### `AnimatedStationView`
The main animated view with the following capabilities:

| Method | Description |
|--------|-------------|
| `ToggleOrientation()` | Switches between Horizontal ↔ Vertical layout |
| `AnimateVehicleDriveIn()` | Smooth cubic ease-in animation for arriving vehicles |
| `AnimateVehicleDriveAway()` | Smooth cubic ease-out animation for departing vehicles |
| `UpdateChargingProgress()` | Animates battery bar fill with color transition |
| `DrawStationLayout()` | Redraws parking spots, arrows, and ports |
| `RedrawAllVehicles()` | Repositions all vehicles after orientation change |

#### `ChargingStation` Model

```csharp
public class ChargingStation
{
    public int Id { get; set; }
    public int Capacity { get; set; }           // Max simultaneous vehicles
    public double CurrentPowerOutput { get; }   // Live kW output
    public double Utilization { get; }          // Usage percentage
    public int QueueLength { get; }             // Waiting vehicles
    public int FreeSlots { get; }               // Available spots
    public List<Vehicle> ChargingVehicles { get; }
}
```

---

## 🎮 Usage

### Toggle Orientation

Switch the station layout programmatically:

```csharp
// Find the control
var stationView = this.FindControl<AnimatedStationView>("MyStation");

// Toggle between horizontal and vertical
stationView.ToggleOrientation();
```

Or bind a button in XAML:

```xml
<Button Content="🔄 Rotate Layout"
        Click="OnToggleOrientationClick"
        Background="#1ABC9C"
        Foreground="White"
        CornerRadius="8"
        Padding="12,6"/>
```

```csharp
private void OnToggleOrientationClick(object sender, RoutedEventArgs e)
{
    StationView.ToggleOrientation();
}
```

### Bind a Station Model

```xml
<views:AnimatedStationView
    x:Name="StationView"
    Station="{Binding MyStation}"
    Width="400"
    Height="500"/>
```

---

## 🔋 Vehicle States

Each vehicle goes through the following animation states:

```
[OFF SCREEN] ──► ARRIVING ──► CHARGING ──► LEAVING ──► [OFF SCREEN]
                    │                          │
               Drives in                  Drives out
           (ease-in animation)        (ease-out animation)
```

Battery color changes based on charge level:

| Level | Color | Meaning |
|-------|-------|---------|
| 0–30% | 🔴 Red `#E74C3C` | Low battery |
| 30–70% | 🟡 Orange `#F39C12` | Charging |
| 70–100% | 🟢 Green `#1ABC9C` | Full charge |

---

## ⚙️ Configuration

You can tweak simulation constants in `AnimatedStationView.axaml.cs`:

```csharp
// Animation canvas size
private const double CANVAS_WIDTH  = 350;
private const double CANVAS_HEIGHT = 250;

// Vehicle dimensions
private const double VEHICLE_WIDTH  = 60;
private const double VEHICLE_HEIGHT = 30;

// Charging port size
private const double PORT_SIZE = 30;

// Update rate (ms) — lower = smoother animation
Interval = TimeSpan.FromMilliseconds(50); // 20 FPS
```

---

## 🛠️ Built With

| Technology | Purpose |
|------------|---------|
| [Avalonia UI 11](https://avaloniaui.net/) | Cross-platform UI framework |
| [.NET 8](https://dotnet.microsoft.com/) | Runtime & language platform |
| [C# 12](https://docs.microsoft.com/en-us/dotnet/csharp/) | Primary language |
| XAML | Declarative UI layout |
| `DispatcherTimer` | UI thread animation loop |
| `async/await` | Non-blocking animations |

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create your feature branch: `git checkout -b feature/new-vehicle-type`
3. Commit your changes: `git commit -m 'Add truck vehicle type'`
4. Push to the branch: `git push origin feature/new-vehicle-type`
5. Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 👤 Author
  
🔗 GitHub: [@SaidSoftware](https://github.com/your-username)  
📧 Email: saidmubin.s16m@gmail.com

---

<div align="center">
  Made with ❤️ and Avalonia UI
</div>
