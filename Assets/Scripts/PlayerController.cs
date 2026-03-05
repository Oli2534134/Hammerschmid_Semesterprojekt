using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f;

    [Header("Heart Health")]
    public float maxHeartHealth = 100f;
    public float currentHeartHealth = 100f;

    [Header("Heartbeat")]
    public float restingHeartRate = 60f;
    public float maxHeartRate = 200f;
    public float minHeartRate = 60f;
    public float heartRateIncrease = 20f;
    public float heartRateDecrease = 15f;
    public float sprintHeartRateMultiplier = 2.5f;

    [Header("Optimal Heart Rate Window")]
    [Tooltip("Lower bound of the optimal heart rate zone (normal speed).")]
    public float optimalHeartRateLow = 70f;
    [Tooltip("Upper bound of the optimal heart rate zone (normal speed).")]
    public float optimalHeartRateHigh = 120f;
    [Tooltip("Minimum speed multiplier when heart rate is at the extreme (min or max).")]
    public float minSpeedMultiplier = 0.3f;

    [HideInInspector]
    public float currentHeartRate;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDead = false;
    private bool isMoving = false;
    private bool isSprinting = false;
    [HideInInspector]
    public float speedMultiplier = 1f;

    public TextMeshProUGUI heartRateText;

    public TextMeshProUGUI heartHealth;

    [Header("Weapon System")]
    [HideInInspector]
    public WeaponData equippedWeapon;
    public float heldWeaponScale = 2.2f;
    private float lastAttackTime = -Mathf.Infinity;
    private SpriteRenderer weaponSpriteRenderer;

    private PlayerInventory inventory;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHeartRate = restingHeartRate;
        currentHeartHealth = Mathf.Clamp(currentHeartHealth, 0f, maxHeartHealth);
        inventory = GetComponent<PlayerInventory>();

        // Create weapon visual child object
        var weaponChild = new GameObject("WeaponVisual");
        weaponChild.transform.SetParent(transform);
        weaponChild.transform.localPosition = new Vector3(0.5f, 0, 0);
        weaponChild.transform.localScale = Vector3.one * heldWeaponScale;
        weaponSpriteRenderer = weaponChild.AddComponent<SpriteRenderer>();
        weaponSpriteRenderer.sortingOrder = 10;
    }

    void Update()
    {
        if (isDead) return;

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        isMoving = moveInput.magnitude > 0.1f;
        isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);

        HandleAttackInput();
        UpdateHeartRate();
        UpdateSpeedMultiplier();

        if (currentHeartRate >= maxHeartRate)
        {
            Die("Heart rate too high!");
        }

        if (currentHeartHealth <= 0f)
        {
            Die("Heart health reached zero!");
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        float currentSpeed = moveSpeed * speedMultiplier;
        if (isSprinting) currentSpeed *= sprintMultiplier;
        rb.linearVelocity = moveInput * currentSpeed;

        heartRateText.text = $"{currentHeartRate:F0} BPM";
        heartHealth.text = $"Heart Health: {currentHeartHealth:F0}/{maxHeartHealth:F0}";
    }

    void UpdateHeartRate()
    {
        if (isMoving)
        {
            float increase = heartRateIncrease;
            if (isSprinting) increase *= sprintHeartRateMultiplier;
            currentHeartRate += increase * Time.unscaledDeltaTime;
        }
        else
            currentHeartRate -= heartRateDecrease * Time.unscaledDeltaTime;

        currentHeartRate = Mathf.Clamp(currentHeartRate, minHeartRate, maxHeartRate);
    }

    void UpdateSpeedMultiplier()
    {
        if (currentHeartRate < optimalHeartRateLow)
        {
            // Below optimal window — slow down the further below
            float t = (optimalHeartRateLow - currentHeartRate) / (optimalHeartRateLow - minHeartRate);
            speedMultiplier = Mathf.Lerp(1f, minSpeedMultiplier, t);
        }
        else if (currentHeartRate > optimalHeartRateHigh)
        {
            // Above optimal window — slow down the further above
            float t = (currentHeartRate - optimalHeartRateHigh) / (maxHeartRate - optimalHeartRateHigh);
            speedMultiplier = Mathf.Lerp(1f, minSpeedMultiplier, t);
        }
        else
        {
            // Inside optimal window — normal speed
            speedMultiplier = 1f;
        }
    }

    void Die(string reason)
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        speedMultiplier = 0f;
        Debug.Log($"Player died! {reason}");
    }

    public void IncreaseHeartRate(float amount)
    {
        currentHeartRate += amount;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHeartHealth = Mathf.Clamp(currentHeartHealth - amount, 0f, maxHeartHealth);
    }

    public void SetEquippedWeapon(WeaponData weapon)
    {
        equippedWeapon = weapon;
        if (weaponSpriteRenderer != null)
        {
            weaponSpriteRenderer.sprite = weapon != null ? weapon.icon : null;
            weaponSpriteRenderer.enabled = weaponSpriteRenderer.sprite != null;
        }
    }

    void HandleAttackInput()
    {
        if (equippedWeapon == null) return;
        if (isDead) return;

        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    void Attack()
    {
        if (Time.time - lastAttackTime < (1f / equippedWeapon.attackRate)) return;
        lastAttackTime = Time.time;

        currentHeartRate += equippedWeapon.heartRateIncrease;

        switch (equippedWeapon.weaponType)
        {
            case WeaponType.Melee:
                MeleeAttack();
                break;
            case WeaponType.Ranged:
                RangedAttack();
                break;
        }
    }

    void MeleeAttack()
    {
        Vector3 attackPos = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, equippedWeapon.range);

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(equippedWeapon.damage);
                Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    enemyRb.linearVelocity += knockbackDir * equippedWeapon.knockback;
                }
            }
        }

        Debug.Log($"Melee attack! Damage: {equippedWeapon.damage}");
    }

    void RangedAttack()
    {
        if (equippedWeapon.projectilePrefab == null)
        {
            Debug.LogWarning("Ranged weapon has no projectile prefab!");
            return;
        }

        Vector2 mousePos = Input.mousePosition;
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        Vector2 direction = (worldPos - (Vector2)transform.position).normalized;

        GameObject projectile = Instantiate(equippedWeapon.projectilePrefab, transform.position, Quaternion.identity);
        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.linearVelocity = direction * equippedWeapon.projectileSpeed;
        }

        Debug.Log($"Ranged attack! Projectile fired toward {direction}");
    }
}
