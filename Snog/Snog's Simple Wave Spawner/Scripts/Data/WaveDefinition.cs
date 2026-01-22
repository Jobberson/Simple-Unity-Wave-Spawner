
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Snog/Simple Wave System/Wave Definition", fileName = "Snog_WaveDefinition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Wave Curves")]
    [Tooltip("X = wave number, Y = duration in seconds")]
    public AnimationCurve durationByWave = AnimationCurve.Linear(1f, 20f, 20f, 60f);
    public AnimationCurve budgetByWave = AnimationCurve.Linear(1f, 10f, 20f, 80f);

    [Header("Spawn Pacing Curve")]
    [Tooltip("X = normalized wave progress (0-1), Y = spawn speed multiplier")]
    public AnimationCurve spawnPacing = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Allowed Enemies")]
    [Tooltip("If empty, spawner will use its fallback enemy list.")]
    public List<EnemyDefinition> allowedEnemies = new();

    [Header("Modifiers")]
    [Min(1)]
    public int maxEnemiesPerWave = 50;

    [Min(0.1f)]
    public float durationMultiplier = 1f;

    [Min(0.1f)]
    public float budgetMultiplier = 1f;

    [Tooltip("If true, the wave can end early when there are no enemies left to spawn.")]
    public bool endEarlyWhenSpawnQueueEmpty = true;

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
