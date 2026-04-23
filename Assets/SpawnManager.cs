using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaveEnemyConfig
{
    [Tooltip("Ab welcher Wave sollen diese Gegner spawnen?")]
    public int startWave = 1;
    [Tooltip("Die Liste der Gegner, die ab dieser Wave spawnen dürfen.")]
    public List<GameObject> allowedEnemies = new List<GameObject>();
}

[System.Serializable]
public class WaveWeaponReward
{
    [Tooltip("Bei welcher Wave soll die Waffe vergeben werden?")]
    public int awardAtWave = 2;
    [Tooltip("Welches Waffen-Prefab soll gegeben werden?")]
    public GameObject weaponPrefabReward;
}

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Setup")]
    public Transform playerTransform;
    [Tooltip("Standard Gegner-Liste, wenn keine spezifische Wave-Konfiguration greift.")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("Minimum distance from player where enemies can spawn.")]
    public float minSpawnRadius = 12f;
    [Tooltip("Maximum distance from player where enemies can spawn.")]
    public float maxSpawnRadius = 20f;

    [Header("Wave Settings")]
    public int enemiesInFirstWave = 4;
    public int enemiesIncreasePerWave = 2;
    public float spawnInterval = 0.2f;
    public float timeBetweenWaves = 2.5f;

    [Header("Custom Wave Enemies")]
    [Tooltip("Definiere, welche Gegner ab welcher Wave spawnen. Die Config mit der höchsten startWave, die kleiner oder gleich der aktuellen Wave ist, wird verwendet.")]
    public List<WaveEnemyConfig> waveConfigs = new List<WaveEnemyConfig>();

    [Header("Wave Rewards")]
    [Tooltip("Belohnungen (Waffen), die der Spieler zu Beginn bestimmter Waves ins Inventar bekommt.")]
    public List<WaveWeaponReward> weaponRewards = new List<WaveWeaponReward>();

    [Header("UI")]
    public TMP_Text remainingEnemiesText;
    public TMP_Text currentWaveText;
    [Tooltip("Text-Element, das anzeigt, welche Waffe man bekommen hat.")]
    public TMP_Text weaponRewardText;
    [Tooltip("Wie lange der Belohnungstext angezeigt werden soll.")]
    public float rewardTextDuration = 3f;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();
    private int currentWave = 0;
    private PlayerInventory playerInventory;

    void Start()
    {
        if (weaponRewardText != null)
        {
            weaponRewardText.gameObject.SetActive(false);
        }

        if (playerTransform == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
                playerInventory = player.GetComponent<PlayerInventory>();
            }
        }
        else
        {
            playerInventory = playerTransform.GetComponent<PlayerInventory>();
        }

        StartCoroutine(WaveLoop());
    }

    IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;

            GiveWaveRewards(currentWave);

            if (!HasEnemiesForWave(currentWave))
            {
                Debug.LogWarning($"SpawnManager: No enemy prefabs assigned for wave {currentWave}.");
                UpdateUI();
                yield break;
            }

            int enemiesThisWave = Mathf.Max(0, enemiesInFirstWave + (currentWave - 1) * enemiesIncreasePerWave);

            for (int i = 0; i < enemiesThisWave; i++)
            {
                SpawnRandomEnemy();
                UpdateUI();

                if (spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            while (GetAliveEnemyCount() > 0)
            {
                UpdateUI();
                yield return null;
            }

            UpdateUI();

            if (timeBetweenWaves > 0f)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
    }

    void SpawnRandomEnemy()
    {
        GameObject prefab = GetRandomEnemyPrefab(currentWave);
        if (prefab == null) return;

        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject enemyInstance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        aliveEnemies.Add(enemyInstance);
    }

    void GiveWaveRewards(int waveIndex)
    {
        if (playerInventory == null) return;

        foreach (var reward in weaponRewards)
        {
            if (reward.awardAtWave == waveIndex && reward.weaponPrefabReward != null)
            {
                GroundItemPickup pickup = reward.weaponPrefabReward.GetComponent<GroundItemPickup>();
                if (pickup != null && pickup.itemType == GroundItemPickup.ItemType.Weapon)
                {
                    WeaponData newWeaponData = pickup.CreateWeaponData();
                    playerInventory.AddWeapon(newWeaponData);
                    Debug.Log($"Belohnung für Wave {waveIndex}: {newWeaponData.weaponName} ins Inventar hinzugefügt!");
                    
                    if (weaponRewardText != null)
                    {
                        StartCoroutine(ShowRewardTextRoutine($"Neue Waffe erhalten: {newWeaponData.weaponName}!"));
                    }
                }
                else
                {
                    Debug.LogWarning($"SpawnManager: Das hinterlegte Prefab für Wave {waveIndex} hat kein GroundItemPickup-Script oder ist keine Waffe!");
                }
            }
        }
    }

    IEnumerator ShowRewardTextRoutine(string message)
    {
        weaponRewardText.text = message;
        weaponRewardText.gameObject.SetActive(true);
        yield return new WaitForSeconds(rewardTextDuration);
        weaponRewardText.gameObject.SetActive(false);
    }

    bool HasEnemiesForWave(int waveIndex)
    {
        List<GameObject> enemies = GetEnemiesForWave(waveIndex);
        return enemies != null && enemies.Count > 0;
    }

    List<GameObject> GetEnemiesForWave(int waveIndex)
    {
        List<GameObject> activeEnemies = enemyPrefabs; // Default

        if (waveConfigs != null && waveConfigs.Count > 0)
        {
            int bestStartWave = -1;
            List<GameObject> customEnemies = null;

            foreach (var config in waveConfigs)
            {
                if (waveIndex >= config.startWave && config.startWave > bestStartWave)
                {
                    bestStartWave = config.startWave;
                    customEnemies = config.allowedEnemies;
                }
            }

            if (customEnemies != null && customEnemies.Count > 0)
            {
                activeEnemies = customEnemies;
            }
        }
        return activeEnemies;
    }

    GameObject GetRandomEnemyPrefab(int waveIndex)
    {
        List<GameObject> currentEnemies = GetEnemiesForWave(waveIndex);

        if (currentEnemies == null || currentEnemies.Count == 0) return null;
        int index = Random.Range(0, currentEnemies.Count);
        return currentEnemies[index];
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;
        float minRadius = Mathf.Max(0f, minSpawnRadius);
        float maxRadius = Mathf.Max(minRadius, maxSpawnRadius);

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minRadius, maxRadius);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        return center + new Vector3(offset.x, offset.y, 0f);
    }

    int GetAliveEnemyCount()
    {
        aliveEnemies.RemoveAll(enemy => enemy == null);
        return aliveEnemies.Count;
    }

    void UpdateUI()
    {
        if (currentWaveText != null)
        {
            currentWaveText.text = $"Wave: {currentWave}";
        }

        if (remainingEnemiesText != null)
        {
            remainingEnemiesText.text = $"Remaining Enemies: {GetAliveEnemyCount()}";
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, minSpawnRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, maxSpawnRadius);
    }
}
