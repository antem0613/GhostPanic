using UnityEngine;

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
        // 1人プレイ、または P2 のTransformが設定されていない場合は、1を返す
        if (!Player.Instance.is2P || Player.Instance.target2 == null)
        {
            return 1;
        }

        // P1 のTransformが設定されていない場合は、2を返す
        if (Player.Instance.target1 == null)
        {
            return 2;
        }

        int hp1 = Player.Instance.HP1;
        int hp2 = Player.Instance.HP2;

        if (hp1 < 0) hp1 = 0;
        if (hp2 < 0) hp2 = 0;

        int totalHP = hp1 + hp2;

        if (totalHP <= 0)
        {
            return (Random.Range(0, 2) == 0) ? 1 : 2;
        }
        else
        {
            int randomValue = Random.Range(0, totalHP);

            if (randomValue < hp1)
            {
                return 1;
            }
            else
            {
                return 2;
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

        if (_hp <= 0)
        {
            Die();
        }
        else
        {
            ChangeState(BossState.Cooldown);
        }
    }

    void Die()
    {
        Debug.Log("ボスを撃破！");
        Player.Instance.GameClear();
        Destroy(gameObject);
    }
}
