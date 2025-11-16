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
        Idle,       // 初期位置で待機中（攻撃方法を決定する）
        Charging,   // プレイヤーへ接近中
        Retreating  // 初期位置へ後退中
    }

    public BehaviorType behaviorType = BehaviorType.Static;

    private enum EnemyState
    {
        [Tooltip("フェーズイン完了待ち")]
        Pending,
        [Tooltip("指定された初期位置へ移動中")]
        InitialMove,
        [Tooltip("通常の行動パターン（Static, Strafe, Charge）を実行中")]
        Active
    }
    private EnemyState currentState = EnemyState.Pending;
    public float initialMoveSpeed = 10.0f;
    public float rotationSpeed = 5.0f;

    private Transform playerTransform; // プレイヤー（カメラ）のTransform
    private bool isAIActive = false;   // AIが現在アクティブか

    public float attackInterval = 3.0f;
    public float attackRange = 50.0f;
    private float attackTimer; // 次の攻撃までのタイマー

    public float strafeSpeed = 3.0f;
    public float strafeDistance = 5.0f;
    private Vector3 initialPosition; // スポーンした初期位置
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
        currentState = EnemyState.InitialMove; // 状態を「初期移動」にセット
    }

    void Awake()
    {
        // プレイヤー（メインカメラ）を自動で取得
        if (playerTransform == null)
        {
            playerTransform = Camera.main.transform;
        }

        // 連携スクリプトを取得
        enemySpawnManager = GetComponent<EnemySpawnManager>();
        animator = GetComponent<Animator>();
        animator.SetInteger("HP", HP);

        // 各種初期化
        initialPosition = transform.position;
        attackTimer = attackInterval; // 開始直後に攻撃しないようタイマーをセット

        if (playerTransform != null)
        {
            // 1. プレイヤーへの方向ベクトルを計算
            Vector3 directionToPlayer = playerTransform.position - transform.position;

            if (directionToPlayer.sqrMagnitude > 0.001f) // ゼロベクトル回避
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
        // AIが非アクティブ、またはプレイヤーが見つからない場合は何もしない
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

        // initialTargetPosition に向かって移動
        float step = initialMoveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, initialTargetPosition, step);

        // 到着判定
        if (Vector3.Distance(transform.position, initialTargetPosition) < 0.1f)
        {
            transform.position = initialTargetPosition; // 座標を確定
            currentState = EnemyState.Active; // 通常AIに切り替え
            Debug.Log(gameObject.name + " が所定の位置に到着。AIをActiveに切り替え。");
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

        // 選択された行動タイプに応じて、毎フレーム処理を切り替える
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
        // その場に留まる（ゴーストらしく上下にゆっくり浮遊する例）
        transform.position = initialPosition + new Vector3(0, Mathf.Sin(Time.time * 1.5f) * 0.25f, 0);

        // 攻撃タイマーを処理
        HandleAttackTimer();
    }

    void HandleStrafeBehavior()
    {
        // 初期位置を基準に、左右（ローカル座標のright）に移動
        Vector3 localMove = transform.right * (strafeSpeed * strafeDirection * Time.deltaTime);
        transform.Translate(localMove, Space.World);

        // 初期位置からの距離を計算（X-Z平面で計算）
        Vector3 posDelta = transform.position - initialPosition;
        posDelta.y = 0; // 高低差は無視

        // 設定した移動距離（strafeDistance）に達したら方向転換
        if (posDelta.magnitude > strafeDistance)
        {
            strafeDirection *= -1; // 方向を反転
            // 行き過ぎた分を補正
            transform.position = initialPosition + (posDelta.normalized * strafeDistance) + new Vector3(0, transform.position.y - initialPosition.y, 0);
        }

        // 攻撃タイマーを処理
        HandleAttackTimer();
    }

    void HandleChargeBehavior()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, Player.Instance.chargeTarget.position);

        if (distanceToPlayer > chargeAttackDistance)
        {
            // プレイヤーに向かって前進（突進）
            // Space.World を指定することで、オブジェクトの向きに関わらずワールド座標基準で移動
            Vector3 direction = (Player.Instance.chargeTarget.position - transform.position).normalized;
            transform.Translate(direction * chargeSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            // 攻撃距離に到達したら、突進攻撃（自爆）を実行
            DoChargeAttack();
        }
    }

    void HandleHitAndRunBehavior()
    {
        // playerTransform が無いと行動できない
        if (playerTransform == null) return;

        // HitAndRunの内部状態に応じて処理を分岐
        switch (hitAndRunState)
        {
            // --- 待機状態 (元の位置にいる) ---
            case HitAndRunState.Idle:
                // 攻撃タイマーを処理 (Static/Strafeと同じ)
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    ChooseHitAndRunAttack(); // 時間が来たら攻撃方法を選択
                    attackTimer = attackInterval; // タイマーリセット
                }
                break;

            // --- 接近状態 ---
            case HitAndRunState.Charging:
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position) - meleeAttackRange;

                // 攻撃範囲(meleeAttackRange)より遠い場合 -> 接近
                if (distanceToPlayer > 0.1)
                {
                    Vector3 direction = (playerTransform.position - transform.position).normalized;
                    transform.Translate(direction * chargeSpeed * Time.deltaTime, Space.World);
                }
                // 攻撃範囲に入った場合 -> 近接攻撃して後退開始
                else
                {
                    animator.SetTrigger("Melee");
                    DoMeleeAttack();
                    hitAndRunState = HitAndRunState.Retreating;
                    Debug.Log($"{gameObject.name} が近接攻撃を実行。後退開始。");
                }
                break;

            // --- 後退状態 (元の位置に戻る) ---
            case HitAndRunState.Retreating:
                // initialPosition は ActivateAI() で設定された初期位置
                float distanceToHome = Vector3.Distance(transform.position, initialPosition);

                // ほぼ元の位置に戻るまで
                if (distanceToHome > 0.1f)
                {
                    Vector3 directionToHome = (initialPosition - transform.position).normalized;
                    transform.Translate(directionToHome * retreatSpeed * Time.deltaTime, Space.World);
                }
                // 元の位置に到着
                else
                {
                    transform.position = initialPosition; // 座標を確定
                    hitAndRunState = HitAndRunState.Idle; // 待機状態に戻る
                    animator.SetBool("Moving", false);
                    Debug.Log($"{gameObject.name} が元の位置に帰還。待機状態へ。");
                }
                break;
        }
    }

    void ChooseHitAndRunAttack()
    {
        // meleeAttackChance の確率で近接攻撃を、それ以外で遠距離攻撃を選択
        if (Random.Range(0f, 1f) < meleeAttackChance)
        {
            // 1. 近接攻撃を選択
            Debug.Log($"{gameObject.name} が近接突進を選択。");
            hitAndRunState = HitAndRunState.Charging; // 状態を「接近」に変更
            animator.SetBool("Moving", true);
        }
        else
        {
            // 2. 遠距離攻撃を選択
            Debug.Log($"{gameObject.name} が遠距離攻撃を選択。");
            // 既存の遠距離攻撃メソッドをそのまま呼び出す
            DoPeriodicAttack();
        }
    }

    void DoMeleeAttack()
    {
        Debug.Log(gameObject.name + " が近接攻撃！");

        // --- ターゲットプレイヤーの選択 (DoChargeAttack/DoPeriodicAttack と同じロジック) ---
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
                if (Random.Range(0, totalHP) >= hp1) // HP1の範囲外なら
                {
                    targetPlayerId = 2;
                    targetTransform = Player.Instance.target2;
                }
            }
            else
            {
                // 両者HPゼロならランダム
                if (Random.Range(0, 2) == 1)
                {
                    targetPlayerId = 2;
                    targetTransform = Player.Instance.target2;
                }
            }
        }

        if (targetTransform == null)
        {
            Debug.LogError("近接攻撃: ターゲットが見つかりません。");
            return;
        }

        // --- ダメージ処理の実行 ---
        // Player.TakeDamage を呼び出し
        Player.Instance.TakeDamage(targetPlayerId, meleeAttackDamage); // 設定した近接ダメージ
        Debug.Log($"プレイヤー{targetPlayerId} に {meleeAttackDamage} の近接ダメージを与えました。");

        // (自爆はしない)
    }

    void DoPeriodicAttack()
    {
        if (attackParticlePrefab == null || particleSpawnPoint == null || playerTransform == null)
        {
            Debug.LogWarning($"攻撃設定が不完全なため、{gameObject.name} は攻撃を実行できません。");
            return; // 攻撃を実行せずに終了
        }

        animator.SetTrigger("Attack");

        Debug.Log(gameObject.name + " が遠距離攻撃を実行！");

        Transform targetTransform = null;
        bool isTwoPlayer = Player.Instance.is2P;
        Transform target1 = Player.Instance.target1; // プレイヤー1のTransform
        Transform target2 = Player.Instance.target2; // プレイヤー2のTransform
        int hp1 = Player.Instance.HP1;
        int hp2 = Player.Instance.HP2;

        if (!isTwoPlayer)
        {
            // --- 1人プレイの場合 ---
            targetTransform = target1;
            if (targetTransform == null)
            {
                Debug.LogError("Player.target1 が設定されていません！");
                return; // ターゲットがなければ攻撃中断
            }
            Debug.Log($"{gameObject.name} が Player 1 をターゲットに攻撃。");
        }
        else
        {
            // --- 2人プレイの場合 ---
            if (target1 == null || target2 == null)
            {
                Debug.LogError("Player.target1 または Player.target2 が設定されていません！");
                return; // ターゲットがなければ攻撃中断
            }

            int totalHP = hp1 + hp2;

            int randomValue = Random.Range(0, totalHP); // 0 から (totalHP - 1) までの整数
            
            if (randomValue < hp1)
            {
                targetTransform = target1; // HP1の範囲内ならP1
            }
            else
            {
                targetTransform = target2; // それ以外ならP2
            }
            
            Debug.Log($"{gameObject.name} が Player {(targetTransform == target1 ? 1 : 2)} をターゲットに攻撃 (HPバイアス - HP1:{hp1}, HP2:{hp2})。");
        }

        // 2. パーティクルプレハブを発生場所(particleSpawnPoint)に生成
        GameObject particleGO = Instantiate(
            attackParticlePrefab,
            particleSpawnPoint.position,
            particleSpawnPoint.rotation // 初期回転は発生場所に合わせる
        );

        // 3. 生成したパーティクルをプレイヤーの方向に向ける
        Vector3 directionToTarget = targetTransform.position - particleSpawnPoint.position;
        if (directionToTarget.sqrMagnitude > 0.001f) // ゼロベクトル回避
        {
            particleGO.transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
    }

    void DoChargeAttack()
    {
        animator.SetTrigger("Attack");
        int targetPlayerId = 1; // デフォルトはプレイヤー1 (画面右側)

        // Playerクラスの静的変数HP1, HP2にアクセスできると仮定
        // (もしPlayerクラスが静的でない場合、GameManagerなどを経由して取得してください)
        int hp1 = Player.Instance.HP1;
        int hp2 = Player.Instance.HP2;
        bool player2Exists = hp2 > 0; // HP2が0より大きい場合、プレイヤー2が存在すると判断

        if (player2Exists)
        {
            int totalHP = hp1 + hp2;
            if (totalHP > 0) // 両プレイヤーのHPが0の場合はデフォルト(P1)のまま
            {
                // HPの合計値を最大値とする乱数を生成
                int randomValue = Random.Range(0, totalHP); // 0 から (totalHP - 1) までの整数

                // 乱数がHP1未満ならP1を、そうでなければP2をターゲットにする
                // (HPが高い方が選択される確率が高くなる)
                if (randomValue < hp1)
                {
                    targetPlayerId = 1;
                }
                else
                {
                    targetPlayerId = 2; // プレイヤー2 (画面左側)
                }
                Debug.Log($"ターゲット選択 (HPバイアス): Player {targetPlayerId} (HP1: {hp1}, HP2: {hp2})");
            }
            else
            {
                Debug.Log($"ターゲット選択: Player {targetPlayerId} (両者HPゼロ)");
            }
        }
        else
        {
            Debug.Log($"ターゲット選択: Player {targetPlayerId} (プレイヤー2不在)");
        }

        // --- 2. ダメージ処理の実行 ---
        // Playerクラスの静的なTakeDamageメソッドを呼び出すと仮定
        // (引数は targetPlayer = ターゲットID, damage = ダメージ量)
        Player.Instance.TakeDamage(targetPlayerId, chargeAttackDamage);
        Debug.Log($"プレイヤー{targetPlayerId} に {chargeAttackDamage} ダメージを与えました。");


        // --- 3. 自身の破壊（死亡）処理 ---
        // AIを停止 (重要: 破壊前に他の行動をしないように)
        isAIActive = false;
        currentState = EnemyState.Pending; // 状態を戻す (必須ではないが念のため)

        // EnemyPhaseInスクリプトの死亡処理を呼び出して、WaveSpawnerに死亡を通知
        if (enemySpawnManager != null)
        {
            // HandleDeath() が GameObject の Destroy も行う想定
            enemySpawnManager.HandleDeath();
        }
        else
        {
            // EnemyPhaseIn が見つからなかった場合のフォールバック
            Destroy(gameObject);
        }
    }

    void HandleAttackTimer()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            // 射程距離内かチェック
            if (Vector3.Distance(transform.position, playerTransform.position) <= attackRange)
            {
                DoPeriodicAttack();
            }
            // タイマーをリセット
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
