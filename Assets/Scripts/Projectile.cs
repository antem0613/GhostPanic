using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public float maxLifetime = 5f;
    public int damageAmount = 10;
    public string playerTag = "Player";

    private float lifetimeTimer;

    void Start()
    {
        lifetimeTimer = maxLifetime;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            DestroyProjectile();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Projectile hit player: {other.name}");

            int targetPlayerId = 1;

            if (other.name.Contains("2"))
            {
                targetPlayerId = 2;
            }

            Player.Instance.TakeDamage(targetPlayerId, damageAmount);
            Debug.Log($"Applied {damageAmount} damage to Player {targetPlayerId}");

            DestroyProjectile();
        }
    }

    public void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}