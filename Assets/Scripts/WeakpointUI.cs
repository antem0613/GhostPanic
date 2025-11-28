using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Dictionary を使うために必要

public class WeakpointUI : MonoBehaviour // (Imageコンポーネントは不要)
{
    // --- シングルトンパターン ---
    public static WeakpointUI Instance { get; private set; }
    // ---

    [Header("設定")]
    [Tooltip("弱点マーカーとして生成するUI Imageのプレハブ")]
    public GameObject weakPointMarkerPrefab; // <-- ★これが新しい設定項目です

    [Header("参照")]
    [Tooltip("メインカメラ (設定しない場合は Camera.main を使用)")]
    public Camera mainCamera;

    [Header("オフセット")]
    [Tooltip("3D座標からスクリーン座標へのオフセット (UIの微調整用)")]
    public Vector2 screenOffset = Vector2.zero;

    // --- 内部変数 ---
    // 追従対象(Key)と、生成したUIマーカー(Value)を紐付ける辞書
    private Dictionary<Transform, GameObject> markerInstances = new Dictionary<Transform, GameObject>();

    void Awake()
    {
        // --- シングルトン設定 ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        // ---

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (weakPointMarkerPrefab == null)
        {
            Debug.LogError("WeakPointUI: 弱点マーカーのプレハブが設定されていません！", this);
        }
    }

    /// <summary>
    /// 【公開】追従するターゲットをリストに追加し、マーカーを生成します
    /// </summary>
    public void AddTarget(Transform newTarget)
    {
        if (weakPointMarkerPrefab == null) return;

        // 既に追加されている場合は何もしない
        if (markerInstances.ContainsKey(newTarget)) return;

        // 1. マーカープレハブを生成し、このマネージャーの子にする
        GameObject newMarker = Instantiate(weakPointMarkerPrefab, this.transform);

        // 2. 辞書に追加
        markerInstances.Add(newTarget, newMarker);

        Debug.Log($"WeakPointUI がターゲットを追加: {newTarget.name}");
    }

    /// <summary>
    /// 【公開】ターゲットをリストから削除し、マーカーを破棄します
    /// </summary>
    public void RemoveTarget(Transform targetToRemove)
    {
        // 辞書に存在するか確認
        if (markerInstances.ContainsKey(targetToRemove))
        {
            // 1. 対応するUIマーカーを破棄
            Destroy(markerInstances[targetToRemove]);

            // 2. 辞書から削除
            markerInstances.Remove(targetToRemove);

            Debug.Log($"WeakPointUI がターゲットを削除: {targetToRemove.name}");
        }
    }

    void LateUpdate()
    {
        if (markerInstances.Count == 0) return;

        // 登録されている全てのマーカーの位置を更新
        foreach (var pair in markerInstances)
        {
            Transform target = pair.Key;
            GameObject marker = pair.Value;
            RectTransform markerRect = marker.GetComponent<RectTransform>(); // 毎回取得 (非効率だが確実)

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                // ターゲットが無効（破壊されたなど）ならマーカーを非表示
                if (marker.activeSelf) marker.SetActive(false);
                continue;
            }

            // 3Dワールド座標を2Dスクリーン座標に変換
            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

            if (screenPos.z < 0)
            {
                // カメラの後ろにある場合は非表示
                if (marker.activeSelf) marker.SetActive(false);
            }
            else
            {
                // カメラの前にある場合は表示
                if (!marker.activeSelf) marker.SetActive(true);

                // スクリーン座標にオフセットを加えてUIの位置を更新
                markerRect.position = new Vector2(screenPos.x, screenPos.y) + screenOffset;
            }
        }
    }

    // (OnDestroy はシングルトン解除処理)
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}