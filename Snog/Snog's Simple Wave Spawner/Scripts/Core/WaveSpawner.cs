
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Config")]
    [Min(1)]
    public int currentWave = 1;

    [SerializeField] private WaveDefinition waveDefinition;

    [Tooltip("Used only if WaveDefinition.allowedEnemies is empty.")]
    [SerializeField] private List<EnemyDefinition> fallbackEnemies = new();

    [Header("Spawning")]
    [SerializeField] private List<Transform> spawnLocations = new();

    [Header("Runtime (Read Only)")]
    [SerializeField] private readonly List<GameObject> spawnedEnemies = new();

    private readonly List<GameObject> enemiesToSpawn = new();
    private int spawnIndex;
    private float waveTimer;
    private float waveDuration;
    private float spawnTimer;
    private float baseSpawnInterval;

    private void Start()
    {
        if (waveDefinition == null)
        {
            Debug.LogWarning($"{nameof(WaveSpawner)}: No WaveDefinition assigned. Disabling spawner.");
            enabled = false;
            return;
        }

        if (spawnLocations == null || spawnLocations.Count == 0)
        {
            Debug.LogWarning($"{nameof(WaveSpawner)}: No spawn locations assigned. Disabling spawner.");
            enabled = false;
            return;
        }

        GenerateWave();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        spawnedEnemies.RemoveAll(e => e == null);

        waveTimer -= dt;
        spawnTimer -= dt;

        if (spawnTimer <= 0f)
        {
            if (enemiesToSpawn.Count > 0)
            {
                SpawnEnemy();
                ResetSpawnTimer();
            }
            else if (waveDefinition.endEarlyWhenSpawnQueueEmpty)
            {
                waveTimer = 0f;
            }
        }

        if (waveTimer <= 0f && spawnedEnemies.Count <= 0 && enemiesToSpawn.Count <= 0)
        {
            currentWave++;
            GenerateWave();
        }
    }

    private void ResetSpawnTimer()
    {
        float normalizedProgress = 1f;

        if (waveDuration > 0.0001f)
        {
            normalizedProgress = Mathf.Clamp01(1f - (waveTimer / waveDuration));
        }

        float speedMultiplier = waveDefinition.GetSpawnSpeedMultiplier(normalizedProgress);

        // Higher multiplier => faster spawns => smaller interval
        float interval = baseSpawnInterval / speedMultiplier;

        spawnTimer = Mathf.Max(0.01f, interval);
    }

    private void SpawnEnemy()
    {
        if (spawnLocations.Count == 0)
        {
            return;
        }

        if (spawnIndex >= spawnLocations.Count)
        {
            spawnIndex = 0;
        }

        GameObject prefab = enemiesToSpawn[0];
        enemiesToSpawn.RemoveAt(0);

        Transform spawnPoint = spawnLocations[spawnIndex];

        if (prefab != null)
        {
            GameObject enemy = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            spawnedEnemies.Add(enemy);
        }

        spawnIndex++;
        if (spawnIndex >= spawnLocations.Count)
        {
            spawnIndex = 0;
        }
    }

    public void GenerateWave()
    {
        waveDuration = waveDefinition.GetDuration(currentWave);
        waveTimer = waveDuration;

        int budget = waveDefinition.GetBudget(currentWave);

        GenerateEnemiesFromBudget(budget);

        if (enemiesToSpawn.Count <= 0)
        {
            baseSpawnInterval = waveDuration;
            spawnTimer = 0f;

            Debug.LogWarning($"{nameof(WaveSpawner)}: Generated 0 enemies for wave {currentWave}. Check enemy costs, budgets, and wave ranges.");
            return;
        }

        baseSpawnInterval = waveDuration / enemiesToSpawn.Count;
        spawnTimer = 0f;
    }

    private void GenerateEnemiesFromBudget(int budget)
    {
        enemiesToSpawn.Clear();

        List<EnemyDefinition> pool = GetEnemyPoolForWave(currentWave);

        if (pool.Count == 0 || budget <= 0)
        {
            return;
        }

        int cap = Mathf.Max(1, waveDefinition.maxEnemiesPerWave);
        int safetyIterations = cap * 10;

        int iterations = 0;

        while (budget > 0 && enemiesToSpawn.Count < cap && iterations < safetyIterations)
        {
            iterations++;

            EnemyDefinition chosen = ChooseWeightedEnemy(pool, budget);

            if (chosen == null)
            {
                break;
            }

            enemiesToSpawn.Add(chosen.prefab);
            budget -= chosen.cost;
        }
    }

    private List<EnemyDefinition> GetEnemyPoolForWave(int wave)
    {
        List<EnemyDefinition> source = waveDefinition.allowedEnemies;

        if (source == null || source.Count == 0)
        {
            source = fallbackEnemies;
        }

        List<EnemyDefinition> pool = new();

        if (source == null)
        {
            return pool;
        }

        for (int i = 0; i < source.Count; i++)
        {
            EnemyDefinition def = source[i];

            if (def != null && def.IsAvailableForWave(wave))
            {
                pool.Add(def);
            }
        }

        return pool;
    }

    private EnemyDefinition ChooseWeightedEnemy(List<EnemyDefinition> pool, int remainingBudget)
    {
        float totalWeight = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            EnemyDefinition def = pool[i];

            if (def.cost <= remainingBudget)
            {
                totalWeight += def.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            EnemyDefinition def = pool[i];

            if (def.cost > remainingBudget)
            {
                continue;
            }

            cumulative += def.weight;

            if (roll <= cumulative)
            {
                return def;
            }
        }

        return null;
    }
}
