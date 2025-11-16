using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Projectile's forward speed")]
    public float speed = 15f;
    [Tooltip("Maximum time the projectile exists before being destroyed (seconds)")]
    public float maxLifetime = 5f;

    [Header("Damage Settings")]
    [Tooltip("Damage dealt on collision with player")]
    public int damageAmount = 10;
    [Tooltip("Tag of the player object(s)")]
    public string playerTag = "Player"; // Make sure your player GameObject has this tag

    private float lifetimeTimer;

    void Start()
    {
        // Initialize timer
        lifetimeTimer = maxLifetime;
    }

    void Update()
    {
        // --- Movement ---
        // Move the projectile forward based on its local forward direction
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // --- Lifetime Check ---
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            DestroyProjectile();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the collided object has the player tag
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Projectile hit player: {other.name}");

            int targetPlayerId = 1; // Default to player 1

            // Example: If player objects are named "Player1" and "Player2"
            if (other.name.Contains("2"))
            {
                targetPlayerId = 2;
            }

            Player.Instance.TakeDamage(targetPlayerId, damageAmount);
            Debug.Log($"Applied {damageAmount} damage to Player {targetPlayerId}");

            // --- Destroy Projectile on Impact ---
            DestroyProjectile();
        }
    }

    /// <summary>
    /// Handles destroying the projectile and optionally spawning an impact effect.
    /// </summary>
    public void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}