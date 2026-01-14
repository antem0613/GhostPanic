using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public enum SpawnTriggerType
        {
            OnDelay,
            OnDistance
        }
        public SpawnTriggerType triggerType = SpawnTriggerType.OnDelay;
        public GameObject enemyPrefab;
        public Transform spawnPoint;
        public float spawnDelay;
        public float spawnDistance = 20.0f;
        public float Offset = 15.0f;
    }

    [System.Serializable]
    public class Wave
    {
        public float delayBeforeThisWave;

        public EnemySpawnInfo[] enemiesToSpawn;
    }

    public Wave[] waves;
    public bool startOnAwake = false;

    public event Action<WaveSpawner> OnAllWavesCompleted;

    public Transform playerTransform;

    private int currentWaveIndex = 0;
    private int enemiesAliveInWave = 0;
    private bool isSpawning = false;
    private List<EnemySpawnInfo> pendingEnemies = new List<EnemySpawnInfo>();
    private List<EnemySpawnManager> spawnedEnemies = new List<EnemySpawnManager>();
    Coroutine spawnWaveCoroutineHandle;


    void Start()
    {
        if (isSpawning || waves.Length == 0)
        {
            return;
        }

        if (playerTransform == null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    public void StartWaveSequence()
    {
        if (isSpawning || waves.Length == 0)
        {
            return;
        }

        isSpawning = true;
        spawnWaveCoroutineHandle = StartCoroutine(SpawnWaveCoroutine());
    }

    void SpawnEnemy(EnemySpawnInfo enemyInfo)
    {
        if (enemyInfo.spawnPoint == null || enemyInfo.enemyPrefab == null)
        {
            Debug.LogError("SpawnInfoにPrefabまたはSpawnPointが設定されていません。", this);
            enemiesAliveInWave--;
            return;
        }

        Vector3 targetPosition = enemyInfo.spawnPoint.position;
        Vector3 direction = (targetPosition - playerTransform.position).normalized;
        Vector3 startPosition = targetPosition + direction * enemyInfo.Offset;

        GameObject enemyGO = Instantiate(
            enemyInfo.enemyPrefab,
            startPosition,
            enemyInfo.spawnPoint.rotation
        );

        Enemy enemy = enemyGO.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(targetPosition);
        }
        else
        {
            Debug.LogWarning($"プレハブ {enemyInfo.enemyPrefab.name} に EnemyAI がありません！ 初期移動が実行されません。", this);
        }

        EnemySpawnManager enemySpawn = enemyGO.GetComponent<EnemySpawnManager>();
        if (enemySpawn != null)
        {
            enemySpawn.OnDied += OnDied;
            spawnedEnemies.Add(enemySpawn);
            enemySpawn.StartPhaseIn();
        }
        else
        {
            Debug.LogError($"プレハブ {enemyInfo.enemyPrefab.name} に EnemyPhaseIn がありません！", this);
            enemiesAliveInWave--;
        }
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        Debug.Log("ウェーブシーケンス開始");
        currentWaveIndex = 0;

        while (currentWaveIndex < waves.Length)
        {
            Wave currentWave = waves[currentWaveIndex];
            yield return new WaitForSeconds(currentWave.delayBeforeThisWave);

            Debug.Log($"ウェーブ  ( {currentWaveIndex} ) を開始します");

            pendingEnemies.Clear();
            pendingEnemies.AddRange(currentWave.enemiesToSpawn);

            enemiesAliveInWave = currentWave.enemiesToSpawn.Length;
            float waveStartTime = Time.time;

            while (enemiesAliveInWave > 0)
            {
                if (pendingEnemies.Count > 0)
                {
                    for (int i = pendingEnemies.Count - 1; i >= 0; i--)
                    {
                        EnemySpawnInfo pendingEnemy = pendingEnemies[i];
                        bool shouldSpawn = false;

                        switch (pendingEnemy.triggerType)
                        {
                            case EnemySpawnInfo.SpawnTriggerType.OnDelay:
                                if (Time.time >= waveStartTime + pendingEnemy.spawnDelay)
                                {
                                    shouldSpawn = true;
                                }
                                break;

                            case EnemySpawnInfo.SpawnTriggerType.OnDistance:
                                if (playerTransform != null && pendingEnemy.spawnPoint != null)
                                {
                                    float dist = Vector3.Distance(playerTransform.position, pendingEnemy.spawnPoint.position);
                                    if (dist <= pendingEnemy.spawnDistance)
                                    {
                                        shouldSpawn = true;
                                    }
                                }
                                break;
                        }

                        if (shouldSpawn)
                        {
                            SpawnEnemy(pendingEnemy);
                            pendingEnemies.RemoveAt(i);
                        }
                    }
                }

                yield return null;
            }

            Debug.Log($"ウェーブ  ( {currentWaveIndex} ) をクリア！");
            currentWaveIndex++;
        }

        Debug.Log("全てのウェーブをクリアしました！");
        isSpawning = false;
        spawnWaveCoroutineHandle = null;
        OnAllWavesCompleted?.Invoke(this);
    }

    private void OnDied(EnemySpawnManager enemy)
    {
        enemy.OnDied -= OnDied;

        enemiesAliveInWave--;

        Debug.Log($"敵が死亡。残り: {enemiesAliveInWave} 体");
    }

    public void ForceStopAndComplete(bool destroyRemainingEnemies)
    {
        if (spawnWaveCoroutineHandle != null)
        {
            StopCoroutine(spawnWaveCoroutineHandle);
            spawnWaveCoroutineHandle = null;
        }

        isSpawning = false;
        enemiesAliveInWave = 0;
        pendingEnemies.Clear();

        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                enemy.OnDied -= OnDied;

                if (destroyRemainingEnemies)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }
        spawnedEnemies.Clear();

        OnAllWavesCompleted?.Invoke(this);
    }

    void OnDrawGizmos()
    {
        if (SceneView.lastActiveSceneView == null) return;

        var defaultZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Vector3 cameraForward = SceneView.lastActiveSceneView.camera.transform.forward;

        Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
        Handles.DrawSolidDisc(transform.position, cameraForward, 1f);

        if (waves == null) return;

        Handles.color = new Color(0f, 1f, 1f, 0.7f);

        foreach (var wave in waves)
        {
            if (wave.enemiesToSpawn == null) continue;

            foreach (var enemyInfo in wave.enemiesToSpawn)
            {
                if (enemyInfo != null && enemyInfo.spawnPoint != null)
                {
                    Handles.DrawSolidDisc(enemyInfo.spawnPoint.position, cameraForward, 0.5f);
                }
            }
        }
    }

    void OnDestroy()
    {
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                enemy.OnDied -= OnDied;
            }
        }
    }
}