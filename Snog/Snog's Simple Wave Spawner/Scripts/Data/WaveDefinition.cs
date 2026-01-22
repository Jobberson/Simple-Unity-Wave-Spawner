
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Snog/Simple Wave System/Wave Definition", fileName = "WaveDefinition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Wave Curves (X = Wave Number, Y = Value)")]
    public AnimationCurve durationByWave = AnimationCurve.Linear(1f, 20f, 20f, 60f);
    public AnimationCurve budgetByWave = AnimationCurve.Linear(1f, 10f, 20f, 80f);

    [Header("Spawn Pacing (X = 0..1 wave progress, Y = spawn speed multiplier)")]
    public AnimationCurve spawnPacing = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Allowed Enemies (Optional)")]
    [Tooltip("If empty, spawner will use its fallback enemy list.")]
    public List<EnemyDefinition> allowedEnemies = new();

    [Header("Limits / Behavior")]
    [Min(1)]
    public int maxEnemiesPerWave = 50;

    [Tooltip("If true, the wave can end early when there are no enemies left to spawn.")]
    public bool endEarlyWhenSpawnQueueEmpty = true;

    [Header("Multipliers")]
    [Min(0.1f)]
    public float durationMultiplier = 1f;

    [Min(0.1f)]
    public float budgetMultiplier = 1f;

    public int GetBudget(int wave)
    {
        float v = budgetByWave.Evaluate(wave) * budgetMultiplier;
        return Mathf.Max(0, Mathf.RoundToInt(v));
    }

    public float GetDuration(int wave)
    {
        float v = durationByWave.Evaluate(wave) * durationMultiplier;
        return Mathf.Max(0.1f, v);
    }

    public float GetSpawnSpeedMultiplier(float normalizedProgress)
    {
        normalizedProgress = Mathf.Clamp01(normalizedProgress);
        float v = spawnPacing.Evaluate(normalizedProgress);
        return Mathf.Max(0.01f, v);
    }
}
