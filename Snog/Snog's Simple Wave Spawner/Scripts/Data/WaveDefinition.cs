
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wave System/Wave Definition", fileName = "WD_WaveDefinition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Scaling Curves")]
    [Tooltip("Total budget for the wave. (X = Wave Number, Y = Value)")]
    public AnimationCurve budgetCurve = AnimationCurve.Linear(1f, 10f, 20f, 80f);
    public AnimationCurve durationCurve = AnimationCurve.Linear(1f, 20f, 20f, 60f);

    [Tooltip("Spawns per second. Example: 0.5 = one spawn every 2 seconds, 2 = two spawns per second.")]
    public AnimationCurve spawnRateCurve = AnimationCurve.Linear(1f, 0.5f, 20f, 2f);

    [Tooltip("Max enemies generated for the wave.")]
    public AnimationCurve maxEnemiesCurve = AnimationCurve.Linear(1f, 25f, 20f, 60f);

    [Tooltip("Max alive at once. Set Y <= 0 to disable cap.")]
    public AnimationCurve maxAliveCurve = AnimationCurve.Constant(1f, 20f, 0f);

    [Header("Spawn Pacing (X = 0..1 wave progress, Y = spawn rate multiplier)")]
    public AnimationCurve spawnPacingCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Allowed Enemies (Optional)")]
    [Tooltip("If empty, spawner uses its fallback list.")]
    public List<EnemyDefinition> allowedEnemies = new();

    [Header("Behavior")]
    public bool endEarlyWhenSpawnQueueEmpty = true;

    [Header("Multipliers")]
    [Min(0.1f)]
    public float budgetMultiplier = 1f;

    [Min(0.1f)]
    public float durationMultiplier = 1f;

    [Min(0.1f)]
    public float spawnRateMultiplier = 1f;

    public int GetBudget(int wave)
    {
        float v = budgetCurve.Evaluate(wave) * budgetMultiplier;
        return Mathf.Max(0, Mathf.RoundToInt(v));
    }

    public float GetDuration(int wave)
    {
        float v = durationCurve.Evaluate(wave) * durationMultiplier;
        return Mathf.Max(0.1f, v);
    }

    public float GetSpawnRate(int wave)
    {
        float v = spawnRateCurve.Evaluate(wave) * spawnRateMultiplier;
        return Mathf.Max(0.01f, v);
    }

    public int GetMaxEnemies(int wave)
    {
        float v = maxEnemiesCurve.Evaluate(wave);
        return Mathf.Max(1, Mathf.RoundToInt(v));
    }

    public int GetMaxAlive(int wave)
    {
        float v = maxAliveCurve.Evaluate(wave);
        int value = Mathf.RoundToInt(v);

        if (value <= 0)
        {
            return int.MaxValue;
        }

        return value;
    }

    public float GetSpawnPacingMultiplier(float normalizedProgress)
    {
        normalizedProgress = Mathf.Clamp01(normalizedProgress);
        float v = spawnPacingCurve.Evaluate(normalizedProgress);
        return Mathf.Max(0.01f, v);
    }
}
