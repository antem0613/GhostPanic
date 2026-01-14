using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [SerializeField]
    int HP;
    [SerializeField]
    int scoreValue;

    public enum BehaviorType
    {
        Static,
        Strafe,
        Charge,
        HitAndRun
    }

    enum HitAndRunState
    {
        Idle,       // 初期位置で待機中
        Charging,   // プレイヤーへ接近中
        Retreating  // 初期位置へ後退中
    }

    public BehaviorType behaviorType = BehaviorType.Static;

    private enum EnemyState
    {
        Pending,
        InitialMove,
        Active
    }
    private EnemyState currentState = EnemyState.Pending;
    public float initialMoveSpeed = 10.0f;
    public float rotationSpeed = 5.0f;

    private Transform playerTransform;
    private bool isAIActive = false;

    public float attackInterval = 3.0f;
    public float attackRange = 50.0f;
    private float attackTimer;

    public float strafeSpeed = 3.0f;
    public float strafeDistance = 5.0f;
    private Vector3 initialPosition;
    private int strafeDirection = 1; // 1:右, -1:左

    public GameObject attackParticlePrefab;
    public Transform particleSpawnPoint;

    public float chargeSpeed = 8.0f;
    public float chargeAttackDistance = 3.0f;
    public int chargeAttackDamage = 1;

    public float meleeAttackRange = 3.0f;
    public int meleeAttackDamage = 15;
    public float retreatSpeed = 5.0f;
    [Range(0f, 1f)]
    public float meleeAttackChance = 0.5f;
    HitAndRunState hitAndRunState = HitAndRunState.Idle;

    EnemySpawnManager enemySpawnManager;
    Animator animator;
    Vector3 initialTargetPosition;

    public void Initialize(Vector3 targetPosition)
    {
        initialTargetPosition = targetPosition;
        currentState = EnemyState.InitialMove;
    }

    void Awake()
    {
        if (playerTransform == null)
        {
            playerTransform = Camera.main.transform;
        }

        enemySpawnManager = GetComponent<EnemySpawnManager>();
        animator = GetComponent<Animator>();
        animator.SetInteger("HP", HP);

        initialPosition = transform.position;
        attackTimer = attackInterval; // 開始直後に攻撃しないようタイマーをセット

        if (playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

                transform.rotation = targetRotation;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isAIActive || playerTransform == null)
        {
            return;
        }

        switch (currentState)
        {
            case EnemyState.InitialMove:
                HandleInitialMove();
                break;

            case EnemyState.Active:
                HandleActiveBehavior();
                break;
        }
    }

    public void ActivateAI()
    {
        isAIActive = true;
        initialPosition = initialTargetPosition;
    }

    void HandleInitialMove()
    {
        if (playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        float step = initialMoveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, initialTargetPosition, step);

        if (Vector3.Distance(transform.position, initialTargetPosition) < 0.1f)
        {
            transform.position = initialTargetPosition;
            currentState = EnemyState.Active;
            Debug.Log(gameObject.name + " が位置に到着");
        }
    }

    void HandleActiveBehavior()
    {
        if (playerTransform != null)
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;

            directionToPlayer.y = 0;

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        switch (behaviorType)
        {
            case BehaviorType.Static:
                HandleStaticBehavior();
                break;

            case BehaviorType.Strafe:
                HandleStrafeBehavior();
                break;

            case BehaviorType.Charge:
                HandleChargeBehavior();
                break;
            case BehaviorType.HitAndRun:
                HandleHitAndRunBehavior();
                break;
        }
    }

    void HandleStaticBehavior()
    {
        transform.position = initialPosition + new Vector3(0, Mathf.Sin(Time.time * 1.5f) * 0.25f, 0);

        HandleAttackTimer();
    }

    void HandleStrafeBehavior()
    {
        Vector3 localMove = transform.right * (strafeSpeed * strafeDirection * Time.deltaTime);
        transform.Translate(localMove, Space.World);

        Vector3 posDelta = transform.position - initialPosition;
        posDelta.y = 0;

        if (posDelta.magnitude > strafeDistance)
        {
            strafeDirection *= -1;
            transform.position = initialPosition + (posDelta.normalized * strafeDistance) + new Vector3(0, transform.position.y - initialPosition.y, 0);
        }

        HandleAttackTimer();
    }

    void HandleChargeBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, Player.Instance.chargeTarget.position);

        if (distanceToPlayer > chargeAttackDistance)
        {
            // プレイヤーに向かって前進
            Vector3 direction = (Player.Instance.chargeTarget.position - transform.position).normalized;
            transform.Translate(direction * chargeSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            // 攻撃距離に到達したら、突進攻撃を実行
            DoChargeAttack();
        }
    }

    void HandleHitAndRunBehavior()
    {
        if (playerTransform == null) return;

        switch (hitAndRunState)
        {
            case HitAndRunState.Idle:
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    ChooseHitAndRunAttack();
                    attackTimer = attackInterval;
                }
                break;

            case HitAndRunState.Charging:
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position) - meleeAttackRange;

                // 攻撃範囲より遠い場合 接近
                if (distanceToPlayer > 0.1)
                {
                    Vector3 direction = (playerTransform.position - transform.position).normalized;
                    transform.Translate(direction * chargeSpeed * Time.deltaTime, Space.World);
                }
                // 攻撃範囲に入った場合 近接攻撃して後退開始
                else
                {
                    animator.SetTrigger("Melee");
                    DoMeleeAttack();
                    hitAndRunState = HitAndRunState.Retreating;
                    Debug.Log($"{gameObject.name} が近接攻撃を実行");
                }
                break;

            case HitAndRunState.Retreating:
                float distanceToHome = Vector3.Distance(transform.position, initialPosition);

                if (distanceToHome > 0.1f)
                {
                    Vector3 directionToHome = (initialPosition - transform.position).normalized;
                    transform.Translate(directionToHome * retreatSpeed * Time.deltaTime, Space.World);
                }
                else
                {
                    transform.position = initialPosition;
                    hitAndRunState = HitAndRunState.Idle;
                    animator.SetBool("Moving", false);
                }
                break;
        }
    }

    void ChooseHitAndRunAttack()
    {
        // meleeAttackChance の確率で近接攻撃を、それ以外で遠距離攻撃を選択
        if (Random.Range(0f, 1f) < meleeAttackChance)
        {
            //近接攻撃
            Debug.Log($"{gameObject.name} が近接突進");
            hitAndRunState = HitAndRunState.Charging;
            animator.SetBool("Moving", true);
        }
        else
        {
            //遠距離攻撃
            Debug.Log($"{gameObject.name} が遠距離攻撃");
            DoPeriodicAttack();
        }
    }

    void DoMeleeAttack()
    {
        Debug.Log(gameObject.name + " が近接攻撃");

        int targetPlayerId = 1;
        Transform targetTransform = Player.Instance.target1; // デフォルトはP1

        bool isTwoPlayer = Player.Instance.is2P;
        int hp1 = Player.Instance.HP1;
        int hp2 = Player.Instance.HP2;

        if (isTwoPlayer && Player.Instance.target2 != null)
        {
            int totalHP = hp1 + hp2;
            if (totalHP > 0)
            {
                if (Random.Range(0, totalHP) >= hp1)
                {
                    targetPlayerId = 2;
                    targetTransform = Player.Instance.target2;
                }
            }
            else
            {
                if (Random.Range(0, 2) == 1)
                {
                    targetPlayerId = 2;
                    targetTransform = Player.Instance.target2;
                }
            }
        }

        if (targetTransform == null)
        {
            return;
        }

        Player.Instance.TakeDamage(targetPlayerId, meleeAttackDamage);
        Debug.Log($"プレイヤー{targetPlayerId} に {meleeAttackDamage} のダメージ");
    }

    void DoPeriodicAttack()
    {
        if (attackParticlePrefab == null || particleSpawnPoint == null || playerTransform == null)
        {
            return;
        }

        animator.SetTrigger("Attack");

        Debug.Log(gameObject.name + " が遠距離攻撃");

        Transform targetTransform = null;
        bool isTwoPlayer = Player.Instance.is2P;
        Transform target1 = Player.Instance.target1;
        Transform target2 = Player.Instance.target2;
        int hp1 = Player.Instance.HP1;
        int hp2 = Player.Instance.HP2;

        if (!isTwoPlayer)
        {
            //1人プレイの場合
            targetTransform = target1;
            if (targetTransform == null)
            {
                return;
            }
            Debug.Log($"{gameObject.name} が Player 1 をターゲットに攻撃");
        }
        else
        {
            // --- 2人プレイの場合 ---
            if (target1 == null || target2 == null)
            {
                return;
            }

            int totalHP = hp1 + hp2;

            int randomValue = Random.Range(0, totalHP);
            
            if (randomValue < hp1)
            {
                targetTransform = target1;
            }
            else
            {
                targetTransform = target2;
            }
            
            Debug.Log($"{gameObject.name} が Player {(targetTransform == target1 ? 1 : 2)} をターゲットに攻撃 (HPバイアス - HP1:{hp1}, HP2:{hp2})。");
        }

        GameObject particleGO = Instantiate(
            attackParticlePrefab,
            particleSpawnPoint.position,
            particleSpawnPoint.rotation
        );

        Vector3 directionToTarget = targetTransform.position - particleSpawnPoint.position;
        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            particleGO.transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
    }

    void DoChargeAttack()
    {
        animator.SetTrigger("Attack");
        int targetPlayerId = 1;

        int hp1 = Player.Instance.HP1;
        int hp2 = Player.Instance.HP2;
        bool player2Exists = hp2 > 0;

        if (player2Exists)
        {
            int totalHP = hp1 + hp2;
            if (totalHP > 0)
            {
                int randomValue = Random.Range(0, totalHP);

                if (randomValue < hp1)
                {
                    targetPlayerId = 1;
                }
                else
                {
                    targetPlayerId = 2;
                }
                Debug.Log($"ターゲット選択 (HPバイアス): Player {targetPlayerId} (HP1: {hp1}, HP2: {hp2})");
            }
        }

        Player.Instance.TakeDamage(targetPlayerId, chargeAttackDamage);
        Debug.Log($"プレイヤー{targetPlayerId} に {chargeAttackDamage} ダメージ");

        isAIActive = false;
        currentState = EnemyState.Pending;

        if (enemySpawnManager != null)
        {
            enemySpawnManager.HandleDeath();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void HandleAttackTimer()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            if (Vector3.Distance(transform.position, playerTransform.position) <= attackRange)
            {
                DoPeriodicAttack();
            }
            attackTimer = attackInterval;
        }
    }

    public void TakeDamage(int damage)
    {
        animator.SetTrigger("GetDamage");
        HP -= damage;
        animator.SetInteger("HP", HP);
        if (HP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Player.Instance.AddScore(scoreValue);
        enemySpawnManager.HandleDeath();
        Destroy(gameObject);
    }
}
