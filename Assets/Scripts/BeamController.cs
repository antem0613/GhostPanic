using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamController : MonoBehaviour
{
    [Header("エフェクト設定")]
    [Tooltip("ヒットエフェクトのプレハブ")]
    public GameObject hitEffectPrefab;
    [Tooltip("ビームが表示される時間")]
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


    public void SetupBeam(Vector3 startPoint, Vector3 endPoint)
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    public void SetupBeam(Vector3 startPoint, Vector3 endPoint, RaycastHit hitInfo)
    {
        SetupBeam(startPoint, endPoint);

        if (hitEffectPrefab != null)
        {
            if (hitInfo.collider.CompareTag(enemyTag))
            {
                Instantiate(hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            }
        }
    }


    void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }
}