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


    public void StartPhaseIn()
    {
        if (enemy)
        {
            enemy.enabled = true;
            enemy.ActivateAI();
        }
    }

    public void HandleDeath()
    {
        OnDied?.Invoke(this);

        Destroy(gameObject);
    }
}