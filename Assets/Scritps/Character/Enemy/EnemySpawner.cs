using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Settings")]
    public NetworkEnemy enemyPrefab;
    public int maxEnemies = 5;
    public float spawnRadius = 10f;
    public float spawnInterval = 5f;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private float nextSpawnTime = 0f;
    private List<NetworkEnemy> activeEnemies = new List<NetworkEnemy>();

    public override void FixedUpdateNetwork()
    {
        // Only Host/Server spawns enemies
        if (Runner == null || !Runner.IsServer) return;

        // Check if should spawn new enemy
        if (Time.time >= nextSpawnTime && activeEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        // Clean up dead enemies
        activeEnemies.RemoveAll(e => e == null || e.IsDead);
    }

    private void SpawnEnemy()
    {
        if (Runner == null || !Runner.IsServer)
        {
            Debug.LogWarning("[EnemySpawner] Runner is not server or null. Cannot spawn.");
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();

        NetworkEnemy enemy = Runner.Spawn(enemyPrefab, spawnPosition, Quaternion.identity, PlayerRef.None);
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
            Debug.Log($"[EnemySpawner] Spawned enemy at {spawnPosition}");
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Runner.Spawn returned null.");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Use predefined spawn points
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint.position;
        }
        else
        {
            // Random position in radius
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw spawn radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Draw spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.5f);
                }
            }
        }
    }
}