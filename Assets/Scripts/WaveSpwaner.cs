using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    // --- Inspector設定用の内部クラス ---

    [System.Serializable]
    public class EnemySpawnInfo
    {
        public enum SpawnTriggerType
        {
            [Tooltip("Wave開始からの秒数で出現")]
            OnDelay,
            [Tooltip("Playerとの距離で出現 (MoveAndSurvive向き)")]
            OnDistance
        }

        [Tooltip("出現のトリガータイプ")]
        public SpawnTriggerType triggerType = SpawnTriggerType.OnDelay;

        [Tooltip("出現させる敵のプレハブ")]
        public GameObject enemyPrefab;

        [Tooltip("出現させる場所（Transform）")]
        public Transform spawnPoint;

        [Tooltip("ウェーブ開始からこの敵が出現するまでの遅延")]
        public float spawnDelay;

        [Tooltip("タイプが OnDistance の場合: Playerとの距離がこの値以下で出現")]
        public float spawnDistance = 20.0f;

        public float Offset = 15.0f;
    }

    [System.Serializable]
    public class Wave
    {
        [Tooltip("前のウェーブが完了してからこのウェーブが始まるまでの待機時間")]
        public float delayBeforeThisWave;

        [Tooltip("このウェーブで出現する敵のリスト")]
        public EnemySpawnInfo[] enemiesToSpawn;
    }

    // --- Public Fields ---
    [Tooltip("このスポナーが管理するウェーブのリスト")]
    public Wave[] waves;

    [Tooltip("スポーン開始時に自動で起動するか？（falseの場合、外部からTriggerWaves()を呼ぶ）")]
    public bool startOnAwake = false;

    public event Action<WaveSpawner> OnAllWavesCompleted;

    public Transform playerTransform;

    // --- Private Fields ---
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

    /// <summary>
    /// 外部からウェーブのシーケンスを開始するためのトリガー
    /// </summary>
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
            enemiesAliveInWave--; // スポーン失敗。進行不能にならないようカウントを減らす
            return;
        }

        Vector3 targetPosition = enemyInfo.spawnPoint.position;
        Vector3 direction = (targetPosition - playerTransform.position).normalized;
        Vector3 startPosition = targetPosition + direction * enemyInfo.Offset;

        Debug.Log($"スポーン: {enemyInfo.enemyPrefab.name} at {enemyInfo.spawnPoint.position}");
        GameObject enemyGO = Instantiate(
            enemyInfo.enemyPrefab,
            startPosition, // targetPosition から変更
            enemyInfo.spawnPoint.rotation // 回転はspawnPointに合わせる
        );

        Enemy enemy = enemyGO.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(targetPosition); // ここで目的地を渡す
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
            enemiesAliveInWave--; // スクリプト欠如。カウントを減らす
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

            // 1. このウェーブの敵を "pending" (待機中) リストにコピー
            pendingEnemies.Clear();
            pendingEnemies.AddRange(currentWave.enemiesToSpawn);

            // 2. このウェーブの生存数をセット
            enemiesAliveInWave = currentWave.enemiesToSpawn.Length;
            float waveStartTime = Time.time;

            // 3. このウェーブが完了するまで (全滅するまで) 毎フレーム監視
            //    (ForceStopAndCompleteが呼ばれると、このコルーチン自体が止まる)
            while (enemiesAliveInWave > 0)
            {
                // 4. スポーン待機中の敵がいたら、トリガーをチェック
                if (pendingEnemies.Count > 0)
                {
                    // 逆からイテレート (リストから削除してもインデックスがズレないように)
                    for (int i = pendingEnemies.Count - 1; i >= 0; i--)
                    {
                        EnemySpawnInfo pendingEnemy = pendingEnemies[i];
                        bool shouldSpawn = false;

                        // 5. トリガー条件をチェック
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

                        // 6. スポーン条件を満たしたらスポーン実行
                        if (shouldSpawn)
                        {
                            SpawnEnemy(pendingEnemy);
                            pendingEnemies.RemoveAt(i);
                        }
                    }
                }

                yield return null; // 次のフレームまで待機
            }

            // (このウェーブの敵が全滅した)
            Debug.Log($"ウェーブ  ( {currentWaveIndex} ) をクリア！");
            currentWaveIndex++;
        }

        Debug.Log("全てのウェーブをクリアしました！");
        isSpawning = false;
        spawnWaveCoroutineHandle = null;
        OnAllWavesCompleted?.Invoke(this); // 正常完了
    }

    /// <summary>
    /// 敵が死亡した時に EnemyPhaseIn から呼ばれる
    /// </summary>
    private void OnDied(EnemySpawnManager enemy)
    {
        // イベントの購読を解除（メモリリーク防止）
        enemy.OnDied -= OnDied;

        enemiesAliveInWave--;

        Debug.Log($"敵が死亡。残り: {enemiesAliveInWave} 体");
    }

    public void ForceStopAndComplete(bool destroyRemainingEnemies)
    {
        Debug.Log($"Spawner {gameObject.name} が外部から強制終了されました。");

        // 1. 進行中のスポーンコルーチンを停止
        if (spawnWaveCoroutineHandle != null)
        {
            StopCoroutine(spawnWaveCoroutineHandle);
            spawnWaveCoroutineHandle = null;
        }

        // 2. 状態をリセット
        isSpawning = false;
        enemiesAliveInWave = 0;
        pendingEnemies.Clear();

        // 3. 残っている敵を処理
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                // 死亡通知の購読を解除（必須）
                enemy.OnDied -= OnDied;

                if (destroyRemainingEnemies)
                {
                    // （オプション）敵AIに死亡処理をさせずに即時破壊
                    Destroy(enemy.gameObject);
                }
            }
        }
        spawnedEnemies.Clear(); // リストをクリア

        // 4. 強制的に完了イベントを発行
        // これにより、SplineCameraMover が次のノードへ移動を開始します。
        OnAllWavesCompleted?.Invoke(this);
    }

    void OnDrawGizmos()
    {
        if (SceneView.lastActiveSceneView == null) return;

        var defaultZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Vector3 cameraForward = SceneView.lastActiveSceneView.camera.transform.forward;

        Handles.color = new Color(1f, 0.5f, 0f, 0.8f); // オレンジ色
        Handles.DrawSolidDisc(transform.position, cameraForward, 1f); // 半径0.5m

        // (オプション) アイコンを表示する場合 (見やすくなります)
        // Gizmos.DrawIcon(transform.position, "sv_icon_dot10_pix32_dm", true);

        // --- 2. 関連する全てのスポーン地点 (spawnPoint) ---
        if (waves == null) return;

        // スポーン地点の色を設定
        Handles.color = new Color(0f, 1f, 1f, 0.7f); // シアン色

        // 全てのWaveの、全てのEnemyInfoをループ
        foreach (var wave in waves)
        {
            if (wave.enemiesToSpawn == null) continue;

            foreach (var enemyInfo in wave.enemiesToSpawn)
            {
                // spawnPointが設定されている場合のみ
                if (enemyInfo != null && enemyInfo.spawnPoint != null)
                {
                    // スポーン地点に小さなソリッドスフィアを描画
                    Handles.DrawSolidDisc(enemyInfo.spawnPoint.position, cameraForward, 0.5f); // 半径0.3m
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