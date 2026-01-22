
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaveSpawner : MonoBehaviour
{
    public enum SpawnMode
    {
        RoundRobin,
        Random,
        WeightedRandom,
        ClosestToTarget,
        FarthestFromTarget,
        OutsideCamera,
        NavMeshValid
    }

    public enum SpawnShape
    {
        PredefinedTransforms,
        RandomInBox,
        RingAroundTarget
    }

    [Header("Wave")]
    [Min(1)]
    public int currentWave = 1;

    [SerializeField] private WaveDefinition waveDefinition;

    [Tooltip("Used only if WaveDefinition.allowedEnemies is empty.")]
    [SerializeField] private List<EnemyDefinition> fallbackEnemies = new();

    [Header("Spawn Selection")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.RoundRobin;
    [SerializeField] private SpawnShape spawnShape = SpawnShape.PredefinedTransforms;

    [SerializeField] private Transform target;
    [SerializeField] private Camera referenceCamera;

    [Header("Spawn Shape: Predefined Transforms")]
    [SerializeField] private List<Transform> spawnPoints = new();

    [Header("Spawn Shape: Random In Box")]
    [SerializeField] private Bounds spawnBox = new Bounds(Vector3.zero, new Vector3(20f, 0f, 20f));

    [Header("Spawn Shape: Ring Around Target")]
    [SerializeField] private float ringInnerRadius = 8f;
    [SerializeField] private float ringOuterRadius = 12f;

    [Header("NavMesh (Optional)")]
    [SerializeField] private bool useNavMeshSampling = false;
    [SerializeField] private float navMeshSampleRadius = 2f;

    [Header("Pooling (Optional)")]
    [SerializeField] private bool usePooling = false;
    [SerializeField] private SimplePool pool;
    [SerializeField] private bool poolAutoExpand = true;

    [Header("Runtime (Read Only)")]
    [SerializeField] private List<GameObject> spawnedEnemies = new();

    private readonly List<GameObject> enemiesToSpawn = new();

    private int spawnIndex;
    private float waveTimer;
    private float waveDuration;
    private float spawnTimer;

    private void Start()
    {
        if (waveDefinition == null)
        {
            Debug.LogWarning($"{nameof(WaveSpawner)}: No WaveDefinition assigned. Disabling spawner.");
            enabled = false;
            return;
        }

        if (spawnShape == SpawnShape.PredefinedTransforms && (spawnPoints == null || spawnPoints.Count == 0))
        {
            Debug.LogWarning($"{nameof(WaveSpawner)}: SpawnShape is PredefinedTransforms but no spawnPoints assigned. Disabling spawner.");
            enabled = false;
            return;
        }

        if (usePooling)
        {
            if (pool == null)
            {
                GameObject go = new GameObject("SimplePool");
                pool = go.AddComponent<SimplePool>();
            }

            pool.SetAutoExpand(poolAutoExpand);
        }

        if (referenceCamera == null)
        {
            referenceCamera = Camera.main;
        }

        GenerateWave();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        spawnedEnemies.RemoveAll(e => e == null);

        waveTimer -= dt;
        spawnTimer -= dt;

        int maxAlive = waveDefinition.GetMaxAlive(currentWave);

        if (spawnTimer <= 0f)
        {
            if (spawnedEnemies.Count < maxAlive && enemiesToSpawn.Count > 0)
            {
                SpawnEnemy();
                ResetSpawnTimer();
            }
            else if (enemiesToSpawn.Count <= 0 && waveDefinition.endEarlyWhenSpawnQueueEmpty)
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
        float spawnRate = waveDefinition.GetSpawnRate(currentWave);

        float normalizedProgress = 1f;

        if (waveDuration > 0.0001f)
        {
            normalizedProgress = Mathf.Clamp01(1f - (waveTimer / waveDuration));
        }

        float pacingMultiplier = waveDefinition.GetSpawnPacingMultiplier(normalizedProgress);

        float effectiveRate = spawnRate * pacingMultiplier;
        float interval = 1f / Mathf.Max(0.01f, effectiveRate);

        spawnTimer = Mathf.Max(0.01f, interval);
    }

    public void GenerateWave()
    {
        waveDuration = waveDefinition.GetDuration(currentWave);
        waveTimer = waveDuration;

        int budget = waveDefinition.GetBudget(currentWave);
        int cap = waveDefinition.GetMaxEnemies(currentWave);

        GenerateEnemiesFromBudget(budget, cap);

        spawnTimer = 0f;
    }

    private void GenerateEnemiesFromBudget(int budget, int cap)
    {
        enemiesToSpawn.Clear();

        List<EnemyDefinition> poolDefs = GetEnemyPoolForWave(currentWave);

        if (poolDefs.Count == 0 || budget <= 0 || cap <= 0)
        {
            return;
        }

        int safetyIterations = cap * 10;
        int iterations = 0;

        while (budget > 0 && enemiesToSpawn.Count < cap && iterations < safetyIterations)
        {
            iterations++;

            EnemyDefinition chosen = ChooseWeightedEnemy(poolDefs, budget);

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

        List<EnemyDefinition> result = new();

        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            EnemyDefinition def = source[i];

            if (def != null && def.IsAvailableForWave(wave))
            {
                result.Add(def);
            }
        }

        return result;
    }

    private EnemyDefinition ChooseWeightedEnemy(List<EnemyDefinition> poolDefs, int remainingBudget)
    {
        float totalWeight = 0f;

        for (int i = 0; i < poolDefs.Count; i++)
        {
            EnemyDefinition def = poolDefs[i];

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

        for (int i = 0; i < poolDefs.Count; i++)
        {
            EnemyDefinition def = poolDefs[i];

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

    private void SpawnEnemy()
    {
        GameObject prefab = enemiesToSpawn[0];
        enemiesToSpawn.RemoveAt(0);

        Vector3 position = GetSpawnPosition();
        Quaternion rotation = Quaternion.identity;

        GameObject enemy = null;

        if (usePooling && pool != null)
        {
            enemy = pool.Get(prefab, position, rotation);
        }
        else
        {
            enemy = Instantiate(prefab, position, rotation);
        }

        if (enemy != null)
        {
            spawnedEnemies.Add(enemy);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 pos = transform.position;

        if (spawnShape == SpawnShape.PredefinedTransforms)
        {
            pos = GetSpawnPositionFromPoints();
        }
        else if (spawnShape == SpawnShape.RandomInBox)
        {
            pos = GetRandomPointInBox();
        }
        else if (spawnShape == SpawnShape.RingAroundTarget)
        {
            pos = GetRandomPointInRing();
        }

        if (spawnMode == SpawnMode.NavMeshValid || useNavMeshSampling)
        {
            pos = SampleNavMesh(pos);
        }

        if (spawnMode == SpawnMode.OutsideCamera)
        {
            pos = EnsureOutsideCamera(pos);
        }

        return pos;
    }

    private Vector3 GetSpawnPositionFromPoints()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return transform.position;
        }

        if (spawnMode == SpawnMode.RoundRobin)
        {
            Transform t = spawnPoints[spawnIndex];
            spawnIndex++;

            if (spawnIndex >= spawnPoints.Count)
            {
                spawnIndex = 0;
            }

            return t.position;
        }

        if (spawnMode == SpawnMode.Random)
        {
            int index = Random.Range(0, spawnPoints.Count);
            return spawnPoints[index].position;
        }

        if (spawnMode == SpawnMode.WeightedRandom)
        {
            return GetWeightedRandomSpawnPointPosition();
        }

        if (spawnMode == SpawnMode.ClosestToTarget)
        {
            return GetClosestSpawnPointPosition();
        }

        if (spawnMode == SpawnMode.FarthestFromTarget)
        {
            return GetFarthestSpawnPointPosition();
        }

        if (spawnMode == SpawnMode.OutsideCamera)
        {
            return GetOutsideCameraSpawnPointPosition();
        }

        return spawnPoints[0].position;
    }

    private Vector3 GetWeightedRandomSpawnPointPosition()
    {
        float total = 0f;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            SpawnPoint sp = spawnPoints[i].GetComponent<SpawnPoint>();
            float w = 1f;

            if (sp != null)
            {
                w = Mathf.Max(0f, sp.weight);
            }

            total += w;
        }

        if (total <= 0f)
        {
            int index = Random.Range(0, spawnPoints.Count);
            return spawnPoints[index].position;
        }

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            SpawnPoint sp = spawnPoints[i].GetComponent<SpawnPoint>();
            float w = 1f;

            if (sp != null)
            {
                w = Mathf.Max(0f, sp.weight);
            }

            cumulative += w;

            if (roll <= cumulative)
            {
                return spawnPoints[i].position;
            }
        }

        return spawnPoints[0].position;
    }

    private Vector3 GetClosestSpawnPointPosition()
    {
        if (target == null)
        {
            int index = Random.Range(0, spawnPoints.Count);
            return spawnPoints[index].position;
        }

        float best = float.MaxValue;
        Transform bestT = spawnPoints[0];

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            float d = Vector3.SqrMagnitude(spawnPoints[i].position - target.position);

            if (d < best)
            {
                best = d;
                bestT = spawnPoints[i];
            }
        }

        return bestT.position;
    }

    private Vector3 GetFarthestSpawnPointPosition()
    {
        if (target == null)
        {
            int index = Random.Range(0, spawnPoints.Count);
            return spawnPoints[index].position;
        }

        float best = float.MinValue;
        Transform bestT = spawnPoints[0];

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            float d = Vector3.SqrMagnitude(spawnPoints[i].position - target.position);

            if (d > best)
            {
                best = d;
                bestT = spawnPoints[i];
            }
        }

        return bestT.position;
    }

    private Vector3 GetOutsideCameraSpawnPointPosition()
    {
        if (referenceCamera == null)
        {
            int index = Random.Range(0, spawnPoints.Count);
            return spawnPoints[index].position;
        }

        List<Transform> outside = new();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Vector3 vp = referenceCamera.WorldToViewportPoint(spawnPoints[i].position);

            bool inFront = vp.z > 0f;
            bool onScreen = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

            if (!(inFront && onScreen))
            {
                outside.Add(spawnPoints[i]);
            }
        }

        if (outside.Count > 0)
        {
            int idx = Random.Range(0, outside.Count);
            return outside[idx].position;
        }

        int fallback = Random.Range(0, spawnPoints.Count);
        return spawnPoints[fallback].position;
    }

    private Vector3 GetRandomPointInBox()
    {
        Vector3 center = spawnBox.center;
        Vector3 ext = spawnBox.extents;

        float x = Random.Range(center.x - ext.x, center.x + ext.x);
        float y = center.y;
        float z = Random.Range(center.z - ext.z, center.z + ext.z);

        return new Vector3(x, y, z);
    }

    private Vector3 GetRandomPointInRing()
    {
        Vector3 center = transform.position;

        if (target != null)
        {
            center = target.position;
        }

        float radius = Random.Range(Mathf.Min(ringInnerRadius, ringOuterRadius), Mathf.Max(ringInnerRadius, ringOuterRadius));
        float angle = Random.Range(0f, Mathf.PI * 2f);

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        return new Vector3(center.x + x, center.y, center.z + z);
    }

    private Vector3 SampleNavMesh(Vector3 desired)
    {
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return desired;
    }

    private Vector3 EnsureOutsideCamera(Vector3 desired)
    {
        if (referenceCamera == null)
        {
            return desired;
        }

        Vector3 vp = referenceCamera.WorldToViewportPoint(desired);
        bool inFront = vp.z > 0f;
        bool onScreen = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

        if (!(inFront && onScreen))
        {
            return desired;
        }

        // Fallback: try a few random ring samples to get offscreen.
        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate = GetRandomPointInRing();
            Vector3 cVp = referenceCamera.WorldToViewportPoint(candidate);

            bool cInFront = cVp.z > 0f;
            bool cOnScreen = cVp.x >= 0f && cVp.x <= 1f && cVp.y >= 0f && cVp.y <= 1f;

            if (!(cInFront && cOnScreen))
            {
                return candidate;
            }
        }

        return desired;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnShape == SpawnShape.RandomInBox)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(spawnBox.center, spawnBox.size);
        }

        if (spawnShape == SpawnShape.RingAroundTarget)
        {
            Vector3 center = target != null ? target.position : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, ringInnerRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, ringOuterRadius);
        }
    }
}
