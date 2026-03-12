using UnityEngine;

public class Bullets : MonoBehaviour
{
    public enum BulletType
    {
        Player,
        Enemy
    }

    public BulletType bulletType = BulletType.Player;
    public AmmoType ammoType = AmmoType.Small;
    public float bulletSpeed = 10f;
    public float bulletDamage = 20f;
    public float lifeTime = 4f;

    private Vector2 moveDirection = Vector2.right;
    private Rigidbody2D rb;
    private Collider2D bulletCollider;
    private Vector2 lastPosition;
    private GameObject owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bulletCollider = GetComponent<Collider2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    public void SetOwner(GameObject bulletOwner)
    {
        owner = bulletOwner;
    }

    public void IgnoreShooterColliders(Collider2D[] shooterColliders)
    {
        if (bulletCollider == null) return;
        if (shooterColliders == null || shooterColliders.Length == 0) return;

        for (int i = 0; i < shooterColliders.Length; i++)
        {
            Collider2D shooterCollider = shooterColliders[i];
            if (shooterCollider == null) continue;
            Physics2D.IgnoreCollision(bulletCollider, shooterCollider, true);
        }
    }

    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            moveDirection = direction.normalized;
        }

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * bulletSpeed;
        }
    }

    void Start()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * bulletSpeed;
        }

        lastPosition = transform.position;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (rb == null)
        {
            transform.Translate((Vector3)(moveDirection * bulletSpeed * Time.deltaTime), Space.World);
        }

        SweepForHits();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject otherObject)
    {
        if (otherObject == null) return;
        if (owner != null && otherObject.transform.root == owner.transform.root) return;

        if (bulletType == BulletType.Player)
        {
            Enemy enemy = otherObject.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = otherObject.GetComponentInParent<Enemy>();
            }
            if (enemy != null)
            {
                enemy.TakeDamage(bulletDamage);
                Destroy(gameObject);
            }

            return;
        }

        if (bulletType == BulletType.Enemy)
        {
            PlayerController player = otherObject.GetComponent<PlayerController>();
            if (player == null)
            {
                player = otherObject.GetComponentInParent<PlayerController>();
            }
            if (player != null)
            {
                player.TakeDamage(bulletDamage);
                Destroy(gameObject);
            }
        }
    }

    void SweepForHits()
    {
        Vector2 currentPosition = transform.position;
        Vector2 delta = currentPosition - lastPosition;
        float distance = delta.magnitude;

        if (distance <= 0.0001f)
        {
            return;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(lastPosition, delta.normalized, distance);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null) continue;
            if (hitCollider == bulletCollider) continue;

            HandleHit(hitCollider.gameObject);
            if (this == null) return;
        }

        lastPosition = currentPosition;
    }
}
