# Charging Station Simulation

Desktop simulation app written in **C# / .NET** (UI: **Avalonia**, architecture: **MVVM**).  
The project simulates the work of an electric vehicle charging station: arrival of cars, queue handling, charging process and basic statistics.

## Features
- Simulation of incoming vehicles and charging sessions
- Queue management (when all chargers are busy)
- Charging progress / status visualization
- Basic results/statistics (e.g. sessions, waiting time, utilization) *(adjust if different)*
- Clean MVVM structure (Views / ViewModels / Models)

## Tech Stack
- **C#**
- **.NET 8** *(change if you use another version)*
- **Avalonia UI**
- **MVVM**

## Requirements
- .NET SDK 8.0+
- (Optional) Visual Studio 2022 / JetBrains Rider

## How to Run
### Option 1: Visual Studio / Rider
1. Open the solution (`.sln`)
2. Select the main project
3. Run

### Option 2: CLI
From the project folder:
```bash
dotnet restore
dotnet run
