using System.Collections.Generic;
using UnityEngine;
 
public class WaveSpawner : MonoBehaviour
{
   
    [SerializeField] private List<Enemy> enemies = new();
    public int currWave;
    private int waveValue;
    [SerializeField] private List<GameObject> enemiesToSpawn = new();
    [SerializeField] private Transform[] spawnLocation;
    [SerializeField] private int spawnIndex;
    [SerializeField] private int waveDuration;
    private float waveTimer;
    private float spawnInterval;
    private float spawnTimer;
    public List<GameObject> spawnedEnemies = new();
    [Space]
    public UImanager uiManager;

    void Start()
    {
        GenerateWave();
        uiManager.SetWaveText(currWave);
    }
    void FixedUpdate()
    {
        if(spawnTimer <= 0)
        {
            //spawn an enemy
            if(enemiesToSpawn.Count > 0)
            {
                GameObject enemy = Instantiate(enemiesToSpawn[0], spawnLocation[spawnIndex].position, Quaternion.identity); // spawn first enemy in our list
                enemiesToSpawn.RemoveAt(0); // and remove it
                spawnedEnemies.Add(enemy);
                spawnTimer = spawnInterval;
 
                if(spawnIndex + 1 <= spawnLocation.Length - 1)
                {
                    spawnIndex++;
                }
                else
                {
                    spawnIndex = 0;
                }
            }
            else
            {
                waveTimer = 0; // if no enemies remain, end wave
            }
        }
        else
        {
            spawnTimer -= Time.fixedDeltaTime;
            waveTimer -= Time.fixedDeltaTime;
        }
 
        if(waveTimer <= 0 && spawnedEnemies.Count <= 0)
        {
            currWave++;
            uiManager.SetWaveText(currWave);
            GenerateWave();
        }
    }
 
    public void GenerateWave()
    {
        if(currWave <= 5)
            waveDuration = currWave * 11;
        else
            waveDuration = currWave * 6;

        waveValue = currWave * 16 - (currWave + 4);
        
        GenerateEnemies();
 
        spawnInterval = waveDuration / enemiesToSpawn.Count; // gives a fixed time between each enemies
        waveTimer = waveDuration; // wave duration is read only
    }
 
    public void GenerateEnemies()
    {

 
        List<GameObject> generatedEnemies = new();
        while(waveValue > 0 || generatedEnemies.Count < 50)
        {
            int randEnemyId = Random.Range(0, enemies.Count);
            int randEnemyCost = enemies[randEnemyId].cost;
 
            if(waveValue-randEnemyCost >= 0)
            {
                generatedEnemies.Add(enemies[randEnemyId].enemyPrefab);
                waveValue -= randEnemyCost;
            }
            else if(waveValue <= 0)
            {
                break;
            }
        }
        enemiesToSpawn.Clear();
        enemiesToSpawn = generatedEnemies;
    }
  
}
 
[System.Serializable]
public class Enemy
{
    public GameObject enemyPrefab;
    public int cost;
}
 