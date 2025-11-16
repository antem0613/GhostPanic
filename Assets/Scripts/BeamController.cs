using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamController : MonoBehaviour
{
    [Header("エフェクト設定")]
    [Tooltip("ヒットエフェクトのプレハブ")]
    public GameObject hitEffectPrefab;
    [Tooltip("ビームが表示される時間（秒）")]
    public float beamDuration = 0.15f; // 少しの間表示させて消す

    [Header("タグ設定")]
    [Tooltip("敵キャラクターに設定されているタグ")]
    public string enemyTag = "Enemy"; // 敵のタグを Inspector で設定

    private LineRenderer lineRenderer;
    private float lifeTimer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lifeTimer = beamDuration;
    }

    /// <summary>
    /// ビームの始点と終点を設定 (Raycastが当たらなかった場合)
    /// </summary>
    public void SetupBeam(Vector3 startPoint, Vector3 endPoint)
    {
        if (lineRenderer == null) return;

        // Line Renderer の頂点数を2に設定
        lineRenderer.positionCount = 2;
        // 始点と終点を設定 (ローカル座標ではなくワールド座標を使う)
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    /// <summary>
    /// ビームの始点と終点、衝突情報を設定 (Raycastが当たった場合)
    /// </summary>
    public void SetupBeam(Vector3 startPoint, Vector3 endPoint, RaycastHit hitInfo)
    {
        SetupBeam(startPoint, endPoint); // まず線を描画

        // ヒットエフェクト生成の判定
        if (hitEffectPrefab != null)
        {
            // 当たったオブジェクトのタグが敵タグと一致するか確認
            if (hitInfo.collider.CompareTag(enemyTag))
            {
                // ヒットエフェクトを衝突地点に生成
                Instantiate(hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            }
        }
    }


    void Update()
    {
        // 生存時間タイマー
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Destroy(gameObject); // 設定時間が経過したらビーム自体を破棄
        }
    }
}