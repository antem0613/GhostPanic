using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using NUnit.Framework.Constraints;

public class Boss : MonoBehaviour
{
    enum BossState
    {
        Attacking,
        Cooldown
    }

    BossState state;
    [SerializeField]
    int HP,HP2P;
    int _hp;
    [SerializeField]
    float cooldownDuration = 2f;
    [SerializeField]
    GameObject[] weakPoints;
    [SerializeField]
    GameObject[] weakPointUI;
    [SerializeField]
    int weakpointDamage;
    float stateTimer;
    [SerializeField]
    float meleeRange;
    [SerializeField]
    int meleeDamage;
    [SerializeField]
    float interval;
    [SerializeField]
    GameObject beamPrefab;
    [SerializeField]
    Transform nozzle;
    float attackTimer;
    Animator animator;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HP = Player.Instance.is2P ? HP2P : HP;
        _hp = HP;
        attackTimer = interval;
        animator = GetComponent<Animator>();

        ChangeState(BossState.Attacking);
    }

    // Update is called once per frame
    void Update()
    {
        if(Player.Instance.target1 == null)
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case BossState.Attacking:
                Attack();
                break;
            case BossState.Cooldown:
                Cooldown(); 
                break;
        }
    }

    void Attack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
        {
            return ;
        }

        Melee();

        attackTimer = interval;
    }

    void Cooldown()
    {
        if (stateTimer <= 0)
        {
            ChangeState(BossState.Attacking);
        }
    }

    void Melee()
    {
        int targetId = GetWeightedTargetPlayerId();
        animator.SetInteger("Target", targetId);
        animator.SetTrigger("Melee");
        Player.Instance.TakeDamage(targetId, meleeDamage);
    }

    void Beam()
    {
        if (beamPrefab == null || nozzle == null) return;

        animator.SetTrigger("Attack");

        int targetId = GetWeightedTargetPlayerId();
        Transform targetTransform = (targetId == 1) ? Player.Instance.target1 : Player.Instance.target2;

        if (targetTransform == null)
        {
            Debug.LogError($"PerformRangedAttack: ターゲット {targetId} のTransformが null です。攻撃を中止します。");
            return;
        }

        GameObject particleGO = Instantiate(
             beamPrefab,
             nozzle.position,
             nozzle.rotation // ボスの銃口の向きに発射
         );

        HomingProjectile proj = particleGO.GetComponent<HomingProjectile>();
        if (proj != null)
        {
            // 追尾対象のターゲット(プレイヤー)を Projectile に教える
            proj.SetTarget(targetTransform);
        }
        else
        {
            // Projectile.cs がない場合のフォールバック (直線的に飛ぶ)
            Vector3 direction = (targetTransform.position - nozzle.position).normalized;
            particleGO.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void ChangeState(BossState newState) 
    {
        state = newState;
        
        switch(newState)
        {
            case BossState.Attacking:
                break;
            case BossState.Cooldown:
                stateTimer = cooldownDuration;
                break;
        }
    }

    int GetWeightedTargetPlayerId()
    {
        // 1人プレイ、または P2 のTransformが設定されていない場合は、P1 (ID:1) を返す
        if (!Player.Instance.is2P || Player.Instance.target2 == null)
        {
            return 1;
        }

        // P1 のTransformが設定されていない場合は、P2 (ID:2) を返す
        if (Player.Instance.target1 == null)
        {
            return 2;
        }

        // --- 2人プレイの場合 ---
        int hp1 = Player.Instance.HP1;
        int hp2 = Player.Instance.HP2;

        // HPはマイナスにならないよう 0 以上にクランプ
        if (hp1 < 0) hp1 = 0;
        if (hp2 < 0) hp2 = 0;

        int totalHP = hp1 + hp2;

        if (totalHP <= 0)
        {
            // 両者HPゼロならランダム
            return (Random.Range(0, 2) == 0) ? 1 : 2;
        }
        else
        {
            // HPが多い方を狙いやすくする
            // Random.Range(0, totalHP) は 0 から (totalHP - 1) までの整数を返す
            int randomValue = Random.Range(0, totalHP);

            if (randomValue < hp1)
            {
                return 1; // 乱数が 0 〜 (hp1 - 1) の範囲なら P1
            }
            else
            {
                return 2; // 乱数が hp1 〜 (totalHP - 1) の範囲なら P2
            }
        }
    }

    public void TakeDamage(int damage)
    {
        animator.SetTrigger("GetDamage");
        _hp -= damage;
        animator.SetInteger("HP", _hp);
        if (_hp <= 0)
        {
            Die();
        }
    }

    public void OnWeakPointHit()
    {
        animator.SetTrigger("GetWeakpoint");
        if (state == BossState.Cooldown)
        {
            return;
        }

        _hp -= weakpointDamage;

        // (ヒットエフェクト、サウンド再生など)

        if (_hp <= 0)
        {
            Die();
        }
        else
        {
            // ダメージを受けたら即座に硬直状態へ
            ChangeState(BossState.Cooldown);
        }
    }

    void Die()
    {
        Debug.Log("ボスを撃破！");
        // (死亡演出、ドロップアイテム、リザルト画面遷移など)
        Player.Instance.GameClear();
        Destroy(gameObject);
    }
}
