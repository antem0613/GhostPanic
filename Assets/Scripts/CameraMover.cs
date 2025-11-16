using Dreamteck.Splines;
using System.Collections;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Events;

// SplineFollowerコンポーネントが必須
[RequireComponent(typeof(SplineFollower))]
public class CameraMover : MonoBehaviour
{
    enum PlayerState
    {
        Idle,
        Moving,
        InCombat,
        WaitingOnEvent
    }
    PlayerState currentState = PlayerState.Idle;

    public enum EncounterType
    {
        None,
        StopAndClear,
        MoveAndSurvive
    }


    [System.Serializable]
    public struct NodeEncounter
    {
        public EncounterType encounterType;

        public float index;

        [Tooltip("このノードで起動するWaveSpawner")]
        public WaveSpawner spawner;

        public float followSpeed;

        public UnityEvent onNodeReachedAction;
    }

    [Header("スプライン設定")]
    [Tooltip("ステージの移動ルートを定義したSplineComputer")]
    public SplineComputer spline;

    [SerializeField]
    float initialFollowSpeed = 10.0f;

    [Header("エンカウンター（戦闘）設定")]
    [Tooltip("停止する地点と、そこで起動するスポナーのリスト")]
    public NodeEncounter[] encounters;

    public bool destroyEnemiesOnArrival = true;
    private SplineFollower splineFollower;
    private int currentEncounterIndex = -1; // 現在のノード番号（最初は-1）

    private double lastStopPercent = 0.0;
    double targetPercent = 0.0;

    WaveSpawner activeMovingSpawner = null;

    void Start()
    {
        splineFollower = GetComponent<SplineFollower>();
        if (spline == null)
        {
            Debug.LogError("Spline Computerが設定されていません！", this);
            return;
        }

        splineFollower.followSpeed = initialFollowSpeed;
        splineFollower.spline = spline;
        splineFollower.follow = true;
        splineFollower.wrapMode = SplineFollower.Wrap.Default; // ループしない
        currentState = PlayerState.Idle;

        // 最初のノードへ移動を開始
        MoveToNextNode();
    }

    /// <summary>
    /// 次のノード（エンカウンター地点）へ移動を開始します
    /// </summary>
    private void MoveToNextNode()
    {
        if(currentState != PlayerState.Idle)
        {
            return;
        }
        currentEncounterIndex++; // 次のノードへ
        currentState = PlayerState.Moving;

        // 全てのノードをクリアした場合
        if (currentEncounterIndex >= encounters.Length)
        {
            Debug.Log($"All node cleared: {lastStopPercent}");
            splineFollower.SetClipRange(lastStopPercent, 1.0);
            splineFollower.Restart();
            
            return;
        }

        NodeEncounter currentEncounter = encounters[currentEncounterIndex];
        float targetNodeIndex = currentEncounter.index;

        if (targetNodeIndex < 0 || targetNodeIndex >= spline.pointCount)
        {
            Debug.LogError($"無効なノード番号 {targetNodeIndex} が指定されました。Splineには {spline.pointCount} 個のポイント(0～{spline.pointCount - 1})しかありません。", this);
            return; // 移動を停止
        }

        targetPercent = targetNodeIndex;
        splineFollower.followSpeed = currentEncounter.followSpeed;

        splineFollower.SetClipRange(lastStopPercent, targetPercent);
        splineFollower.Restart();
        Debug.Log($"ノード {currentEncounterIndex} へ移動開始。タイプ: {currentEncounter.encounterType}, ノード番号: {targetNodeIndex},現在のパーセント: {lastStopPercent}, 目標パーセント: {targetPercent}");

        lastStopPercent = targetPercent;
        splineFollower.onEndReached += OnNodeReached;

        splineFollower.follow = true;
    }

    void OnNodeReached(double percentReached)
    {
        // --- 1. ガード処理 ---
        if (currentState != PlayerState.Moving)
        {
            Debug.LogWarning($"OnNodeReached が不正な状態({currentState})で呼ばれました。無視します。");
            return;
        }

        // --- 2. 基本処理 ---
        splineFollower.onEndReached -= OnNodeReached; // イベント解除
        Debug.Log($"ノードに到着: {currentEncounterIndex} (パーセント: {lastStopPercent})");

        // --- 3. MoveAndSurvive区間の終了処理 ---
        // （もし前の区間がMoveAndSurviveだったら）
        if (activeMovingSpawner != null)
        {
            Debug.Log($"MoveAndSurvive区間が終了。スポナー {activeMovingSpawner.name} を強制停止します。");
            activeMovingSpawner.OnAllWavesCompleted -= OnEnemiesDefeated_Move;
            activeMovingSpawner.ForceStopAndComplete(destroyEnemiesOnArrival);
            activeMovingSpawner = null;
        }

        // --- 4. 最終地点(1.0)に到着した場合 ---
        if (currentEncounterIndex >= encounters.Length)
        {
            Debug.Log("Splineの終点に到着。ゲームクリア。");
            currentState = PlayerState.Idle;
            // (ここでクリア処理を呼ぶ)
            return;
        }

        // --- 5. 到着したノードのタイプ別処理 ---
        NodeEncounter currentEncounter = encounters[currentEncounterIndex];

        currentEncounter.onNodeReachedAction?.Invoke();

        if (currentEncounter.encounterType == EncounterType.StopAndClear)
        {
            HandleStop(currentEncounter);
        }
        else if (currentEncounter.encounterType == EncounterType.MoveAndSurvive)
        {
            HandleMove(currentEncounter);
        }
        else
        {
            HandleNone(currentEncounter);
        }
    }

    void HandleNone(NodeEncounter encounter)
    {
        Debug.Log(encounter.onNodeReachedAction.GetPersistentEventCount());
        if (encounter.onNodeReachedAction.GetPersistentEventCount() > 0)
        {
            currentState = PlayerState.WaitingOnEvent;
            Debug.Log($"ノード {currentEncounterIndex} (None) に到着。イベント待機中。");
        }
        else
        {
            currentState = PlayerState.Idle;
            Debug.Log($"ノード {currentEncounterIndex} (None) に到着。次のノードへ移動します。");
            MoveToNextNode();
        }
    }

    public void OnEventCompleted()
    {
        if (currentState == PlayerState.WaitingOnEvent)
        {
            Debug.Log("イベント完了のシグナルを受信。次のノードへ移動します。");
            currentState = PlayerState.Idle; // 状態をリセット
            MoveToNextNode(); // 次のノードへ
        }
        else
        {
            Debug.LogWarning($"SignalEventComplete が呼ばれましたが、待機状態(WaitingOnEvent)ではありません。 (現在の状態: {currentState})");
        }
    }

    void HandleStop(NodeEncounter encounter)
    {
        currentState = PlayerState.InCombat;

        Debug.Log($"ノード {encounter.index} (StopAndClear) に到着。戦闘開始。");

        WaveSpawner currentSpawner = encounter.spawner;
        if (currentSpawner != null)
        {
            // 全滅したら OnEnemiesDefeated が呼ばれるように登録
            currentSpawner.OnAllWavesCompleted += OnEnemiesDefeated_Stop;
            // 到着してからスポーン開始
            currentSpawner.StartWaveSequence();
        }
        else
        {
            // スポナーがない場合は、すぐに次へ
            Debug.LogWarning($"StopAndClear ノード {encounter.index} にスポナーがありません。スキップします。");
            currentState = PlayerState.Idle;
            MoveToNextNode();
        }
    }

    void HandleMove(NodeEncounter encounter)
    {
        Debug.Log($"ノード {encounter.index} (MoveAndSurvive) に到着。ウェーブを強制終了します。");

        WaveSpawner currentSpawner = encounter.spawner;
        if (currentSpawner != null)
        {
            activeMovingSpawner = currentSpawner; // このスポナーを「移動中アクティブ」に設定
            activeMovingSpawner.OnAllWavesCompleted += OnEnemiesDefeated_Move;
            activeMovingSpawner.StartWaveSequence();
        }
        else
        {
            Debug.LogWarning($"MoveAndSurvive ノード {currentEncounterIndex} にスポナーがありませんでした。スキップします。");
        }

        currentState = PlayerState.Idle;
        MoveToNextNode();
    }

    void OnEnemiesDefeated_Stop(WaveSpawner completedSpawner)
    {
        // --- ガード処理 ---
        if (currentState != PlayerState.InCombat) return;
        if (completedSpawner != encounters[currentEncounterIndex].spawner)
        {
            Debug.LogWarning("OnEnemiesDefeated_StopAndClear: スポナー不一致。無視。");
            if (completedSpawner != null) completedSpawner.OnAllWavesCompleted -= OnEnemiesDefeated_Stop;
            return;
        }

        Debug.Log($"ノード {currentEncounterIndex} の敵を全滅させました。");

        completedSpawner.OnAllWavesCompleted -= OnEnemiesDefeated_Stop;

        currentState = PlayerState.Idle; // 状態を戻す
        MoveToNextNode(); // 次のノードへ
    }

    void OnEnemiesDefeated_Move(WaveSpawner completedSpawner)
    {
        // カメラは移動中のため、進行には影響しない
        Debug.Log($"MoveAndSurviveのウェーブが早期に完了しました: {completedSpawner.name}");
        completedSpawner.OnAllWavesCompleted -= OnEnemiesDefeated_Move;
        if (activeMovingSpawner == completedSpawner)
        {
            activeMovingSpawner = null;
        }
    }

    // オブジェクト破棄時にイベント購読を解除（安全対策）
    void OnDestroy()
    {
        if (splineFollower != null)
        {
            splineFollower.onEndReached -= OnNodeReached;
        }

        // アクティブなスポナーのイベントを解除
        if (activeMovingSpawner != null)
        {
            activeMovingSpawner.OnAllWavesCompleted -= OnEnemiesDefeated_Move;
        }

        // 戦闘中だった場合のイベントを解除
        if (currentState == PlayerState.InCombat && currentEncounterIndex < encounters.Length)
        {
            WaveSpawner spawner = encounters[currentEncounterIndex].spawner;
            if (spawner != null)
            {
                spawner.OnAllWavesCompleted -= OnEnemiesDefeated_Stop;
            }
        }
    }
}