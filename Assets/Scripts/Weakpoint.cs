using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Weakpoint : MonoBehaviour
{
    public Boss bossAI;
    public string playerBeamTag = "PlayerBeam";

    void Awake()
    {
        if (bossAI == null)
        {
            bossAI = transform.parent.gameObject.GetComponentInParent<Boss>();
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