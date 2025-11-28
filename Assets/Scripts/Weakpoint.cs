using UnityEngine;

[RequireComponent(typeof(Collider))] // 当たり判定が必須
public class Weakpoint : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("親オブジェクトにある BossAI スクリプト")]
    public Boss bossAI;

    [Header("設定")]
    [Tooltip("プレイヤーのビーム/弾に設定されているタグ")]
    public string playerBeamTag = "PlayerBeam"; // プレイヤーのビームのタグ名

    void Awake()
    {
        // BossAI の参照を自動で取得 (親オブジェクトにあると仮定)
        if (bossAI == null)
        {
            bossAI = transform.parent.gameObject.GetComponentInParent<Boss>();
        }
        if (bossAI == null)
        {
            Debug.LogError("親オブジェクトに BossAI が見つかりません！", this);
        }
    }

    public void OnHit()
    {
        if (bossAI != null)
        {
            bossAI.OnWeakPointHit();
        }
    }
}