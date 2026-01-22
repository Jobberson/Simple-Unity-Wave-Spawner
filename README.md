
# Wave Spawner System (Free)

A lightweight, data-driven **wave spawning framework for Unity**. Configure enemies and waves using **ScriptableObjects**, scale difficulty via **AnimationCurves**, choose from multiple **spawn modes/shapes**, optionally enable **pooling**, and plug into any project using **C# events** or **UnityEvents**. Includes an **Editor Preview** button and validation warnings to speed up tuning.

---

## Features

### Data-Driven Setup
- **EnemyDefinition (ScriptableObject)**
  - Prefab, cost, weight, minWave / maxWave
- **WaveDefinition (ScriptableObject)**
  - Curve-based difficulty scaling
  - Allowed enemies + elite enemies
  - Boss waves, elite chance, and special wave modifiers

### Difficulty Scaling (Curves)
- Budget curve (points per wave)
- Duration curve (seconds per wave)
- Spawn rate curve (spawns per second)
- Optional caps via curves:
  - Max enemies per wave
  - Max alive at once (concurrency)

### Spawn System
- **Spawn modes**
  - Round-robin
  - Random
  - Weighted random (optional SpawnPoint component)
  - Closest / Farthest from target
  - Outside camera / offscreen
  - NavMesh valid spawn (optional)
- **Spawn shapes**
  - Predefined transforms
  - Random point in box (Bounds)
  - Ring around target (inner/outer radius)

### Wave Variety
- **Boss waves**
  - Boss every N waves
  - Boss at start or end
  - Optional: boss consumes budget
  - Boss spawns **in addition to** normal enemies
- **Elite enemies**
  - Separate elite pool
  - Elite chance scales by curve
  - Elites can spawn on any wave type (boss/rush/tank/swarm included)
- **Special wave modifiers**
  - Rush / Tank / Swarm multipliers (budget, duration, spawn rate, caps)
  - Optional cost bias to favor cheap or expensive enemies

### Integration
- **C# events + UnityEvents**
  - `OnWaveStarted(int wave)`
  - `OnWaveEnded(int wave)`
  - `OnEnemySpawned(GameObject enemy)`
  - `OnAllEnemiesDefeated()`

### Performance (Optional)
- Lightweight pooling (`SimplePool`)
  - Warmup list
  - Auto-expand toggle

---

## Installation

1. Download the repository.
2. Copy the folder into your project:
   - `Assets/Snog/WaveSpawnerSystem/`


---

## Quick Start

### 1) Create EnemyDefinition assets
1. Right-click in Project window  
2. **Create → Wave System → Enemy Definition**
3. Fill:
   - `prefab`
   - `cost` (>= 1)
   - `weight` (>= 0)
   - `minWave` / `maxWave`

### 2) Create a WaveDefinition asset
1. Right-click  
2. **Create → Wave System → Wave Definition**
3. Configure curves:
   - `budgetCurve`
   - `durationCurve`
   - `spawnRateCurve`
   - `maxEnemiesCurve`
   - `maxAliveEnemiesCurve` (set Y <= 0 for unlimited)
4. Add enemies:
   - `allowedEnemies` (optional)
   - if empty, the spawner will use its fallback list

### 3) Add WaveSpawner to the scene
1. Create an empty GameObject: `WaveSpawner`
2. Add component: `WaveSpawner`
3. Assign:
   - `waveDefinition`
   - `spawnShape`
   - `spawnMode`
   - Spawn points / bounds / ring settings depending on shape
   - (optional) `target` (recommended for ring/closest/farthest/offscreen)

---

## Events (C#)

Subscribe to events to integrate UI, audio, difficulty, etc.

```csharp
using UnityEngine;

public class WaveListener : MonoBehaviour
{
    [SerializeField] private WaveSpawner spawner;

    private void OnEnable()
    {
        if (spawner == null)
        {
            return;
        }

        spawner.OnWaveStarted += HandleWaveStarted;
        spawner.OnWaveEnded += HandleWaveEnded;
        spawner.OnEnemySpawned += HandleEnemySpawned;
        spawner.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
    }

    private void OnDisable()
    {
        if (spawner == null)
        {
            return;
        }

        spawner.OnWaveStarted -= HandleWaveStarted;
        spawner.OnWaveEnded -= HandleWaveEnded;
        spawner.OnEnemySpawned -= HandleEnemySpawned;
        spawner.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }

    private void HandleWaveStarted(int wave)
    {
        Debug.Log($"Wave started: {wave}");
    }

    private void HandleWaveEnded(int wave)
    {
        Debug.Log($"Wave ended: {wave}");
    }

    private void HandleEnemySpawned(GameObject enemy)
    {
        Debug.Log($"Enemy spawned: {enemy.name}");
    }

    private void HandleAllEnemiesDefeated()
    {
        Debug.Log("All enemies defeated!");
    }
}
