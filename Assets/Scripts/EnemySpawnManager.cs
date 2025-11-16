using UnityEngine;
using System.Collections;

public class EnemySpawnManager : MonoBehaviour
{
    private Renderer enemyRenderer;
    private Enemy enemy;

    public event System.Action<EnemySpawnManager> OnDied;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (enemy)
        {
            enemy.enabled = false;
        }
    }

    void Start()
    {

    }

    /// <summary>
    /// フェーズイン処理を開始します（WaveSpawnerから呼ばれます）
    /// </summary>
    public void StartPhaseIn()
    {
        if (enemy)
        {
            enemy.enabled = true; // コンポーネントを有効化
            enemy.ActivateAI();   // AIに行動開始を指示
        }
    }

    /// <summary>
    /// 敵が倒された時に呼ばれる想定のメソッド
    /// </summary>
    public void HandleDeath()
    {
        // Spawnerに死亡を通知
        OnDied?.Invoke(this);

        // 死亡エフェクトなどを再生

        // オブジェクトを破壊
        Destroy(gameObject);
    }
}