using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class HomingProjectile : MonoBehaviour
{
    private Transform target;

    public int damageAmount = 10;
    public string playerTag = "Player";
    public float maxLifetime = 10f;

    public float curveDuration = 1.0f;
    public Vector3 curveForceDirection = new Vector3(0, 1, 0);
    public float curveForceStrength = 5f;
    public float initialSpeed = 20f;

    public float homingSpeed = 15f;
    public float rotationSpeed = 10f;

    private Rigidbody rb;
    private float stateTimer;

    private enum ProjectileState
    {
        Curving,
        Homing
    }
    private ProjectileState currentState;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = false;

        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        currentState = ProjectileState.Curving;
        stateTimer = curveDuration;

        rb.linearVelocity = transform.forward * initialSpeed;

        Destroy(gameObject, maxLifetime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        switch (currentState)
        {
            case ProjectileState.Curving:
                stateTimer -= Time.fixedDeltaTime;

                Vector3 force = transform.TransformDirection(curveForceDirection.normalized) * curveForceStrength;
                rb.AddForce(force, ForceMode.Acceleration);

                rb.linearVelocity = transform.forward * initialSpeed;

                if (stateTimer <= 0f)
                {
                    currentState = ProjectileState.Homing;
                }
                break;

            case ProjectileState.Homing:
                Vector3 direction = (target.position - transform.position).normalized;

                transform.position += direction * homingSpeed * Time.deltaTime;

                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            int targetPlayerId = 1;
            if (Player.Instance.is2P && other.transform == Player.Instance.target2)
            {
                targetPlayerId = 2;
            }

            Player.Instance.TakeDamage(targetPlayerId, damageAmount);

            DestroyProjectile();
        }
        else if (!other.isTrigger && !other.CompareTag("Enemy"))
        {
            DestroyProjectile();
        }
    }

    void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}