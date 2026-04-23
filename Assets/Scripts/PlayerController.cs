using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public enum FacingDirection
    {
        Left,
        Right
    }

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
    [Header("Default Fists (Melee)")]
    public string fistsWeaponName = "Fists";
    public float fistsDamage = 10f;
    public float fistsFireCooldown = 0.35f;
    public float fistsRange = 1.1f;
    public float fistsHeartRateIncrease = 4f;
    public float fistsMeleeAngle = 90f;
    public float fistsKnockback = 1.5f;
    public TextMeshProUGUI bulletsText;
    public TextMeshProUGUI reloadText;
    
    [Header("Aim Settings")]
    [Tooltip("The range of the aiming gizmo.")]
    public float aimRange = 5f;
    [Tooltip("The angle cone for aiming/shooting.")]
    public float rangedAimConeAngle = 180f;
    
    public float projectileSpawnOffset = 0.45f;
    private float lastAttackTime = -Mathf.Infinity;
    private int currentAmmo = 0;
    private int currentTotalAmmo = 0;
    private bool isReloading = false;
    private float reloadEndTime = 0f;
    private FacingDirection facingDirection = FacingDirection.Right;
    private WeaponData fistsWeapon;

    private PlayerInventory inventory;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHeartRate = restingHeartRate;
        currentHeartHealth = Mathf.Clamp(currentHeartHealth, 0f, maxHeartHealth);
        inventory = GetComponent<PlayerInventory>();
        RefreshFistsWeaponData();
    }

    void Update()
    {
        if (isDead) return;

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        moveInput = moveInput.normalized;

        isMoving = moveInput.magnitude > 0.1f;
        isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);
        UpdateFacingDirection();
        HandleReloadTimer();

        HandleAttackInput();
        UpdateHeartRate();
        UpdateSpeedMultiplier();
        UpdateBulletsUI();
        UpdateReloadUI();

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
        PlayerAdrenaline adrenaline = GetComponent<PlayerAdrenaline>();
        if (adrenaline != null && adrenaline.isFrenzyActive)
        {
            currentHeartRate = minHeartRate; // Heart rate stays at minimum during Wahnmodus
            return;
        }

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
        PlayerAdrenaline adrenaline = GetComponent<PlayerAdrenaline>();
        if (adrenaline != null && adrenaline.isFrenzyActive)
        {
            speedMultiplier = adrenaline.speedMultiplier; // Nutze den Speed aus dem Wahnmodus
            return;
        }

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
        currentHeartRate = Mathf.Clamp(currentHeartRate + amount, minHeartRate, maxHeartRate);
    }

    public void DecreaseHeartRate(float amount)
    {
        currentHeartRate = Mathf.Clamp(currentHeartRate - amount, minHeartRate, maxHeartRate);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        PlayerAdrenaline adrenaline = GetComponent<PlayerAdrenaline>();
        if (adrenaline != null)
        {
            if (adrenaline.isFrenzyActive) return; // Invincible during Wahnmodus!
            adrenaline.TakeDamage(amount); // Fügt dem Balken Adrenalin hinzu
        }

        currentHeartHealth = Mathf.Clamp(currentHeartHealth - amount, 0f, maxHeartHealth);
        SpawnDamageParticles();
    }

    void SpawnDamageParticles()
    {
        GameObject particlesObj = new GameObject("PlayerDamageParticles");
        particlesObj.transform.position = transform.position;
        ParticleSystem ps = particlesObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        // Grüne Partikel, aber kleiner
        main.startColor = new Color(0f, 0.8f, 0.2f); 
        main.startLifetime = Random.Range(0.2f, 0.4f);
        main.startSpeed = Random.Range(1.5f, 3.5f);
        main.startSize = Random.Range(0.05f, 0.15f); // Kleiner als beim Enemey (0.1 bis 0.3)
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = 1f;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[]{ new ParticleSystem.Burst(0f, (short)Random.Range(8, 15)) });
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0f, 0.8f, 0.2f), 0.0f), new GradientColorKey(new Color(0f, 0.4f, 0.1f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        
        SpriteRenderer playerRenderer = GetComponentInChildren<SpriteRenderer>();
        if (playerRenderer != null)
        {
            renderer.sortingOrder = playerRenderer.sortingOrder + 10;
        }

        Destroy(particlesObj, 1.0f);
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;
        currentHeartHealth = Mathf.Clamp(currentHeartHealth + amount, 0f, maxHeartHealth);
    }

    public void AddAmmo(int amount)
    {
        if (amount <= 0) return;

        // Wenn eine Schusswaffe ausgerüstet ist, füge die Munition direkt hinzu
        if (equippedWeapon != null && equippedWeapon.weaponType == WeaponType.Ranged)
        {
            currentTotalAmmo += amount;
            equippedWeapon.totalAmmo = currentTotalAmmo;
            Debug.Log($"Got {amount} ammo! Total is now: {currentTotalAmmo}");
        }
        else 
        {
            // Wenn keine Schusswaffe in der Hand, checke das Inventar
            if (inventory != null && inventory.rangedSlot != null)
            {
                inventory.rangedSlot.totalAmmo += amount;
                Debug.Log($"Got {amount} ammo for stored ranged weapon! Total is now: {inventory.rangedSlot.totalAmmo}");
            }
        }
    }

    public void SetEquippedWeapon(WeaponData weapon)
    {
        if (equippedWeapon != null && equippedWeapon.weaponType == WeaponType.Ranged)
        {
            equippedWeapon.totalAmmo = currentTotalAmmo;
            equippedWeapon.currentMagazineAmmo = currentAmmo;
        }

        equippedWeapon = weapon;
        if (equippedWeapon != null && equippedWeapon.weaponType == WeaponType.Ranged)
        {
            if (equippedWeapon.currentMagazineAmmo == -1)
            {
                equippedWeapon.currentMagazineAmmo = equippedWeapon.magazineSize;
            }
            currentAmmo = equippedWeapon.currentMagazineAmmo;
            currentTotalAmmo = equippedWeapon.totalAmmo;
            isReloading = false;
        }
        else
        {
            currentAmmo = 0;
            currentTotalAmmo = 0;
            isReloading = false;
        }
    }

    public WeaponData GetFallbackMeleeWeapon()
    {
        RefreshFistsWeaponData();
        return fistsWeapon;
    }

    void HandleAttackInput()
    {
        if (equippedWeapon == null)
        {
            SetEquippedWeapon(GetFallbackMeleeWeapon());
        }

        if (equippedWeapon == null) return;
        if (isDead) return;

        if (equippedWeapon.weaponType == WeaponType.Ranged && Input.GetKeyDown(KeyCode.R))
        {
            StartReload();
        }

            if (Input.GetMouseButton(0))
        {
            Attack();
        }
    }

    void RefreshFistsWeaponData()
    {
        if (fistsWeapon == null)
        {
            fistsWeapon = new WeaponData();
        }

        fistsWeapon.weaponName = fistsWeaponName;
        fistsWeapon.weaponType = WeaponType.Melee;
        fistsWeapon.damage = fistsDamage;
        fistsWeapon.fireCooldown = fistsFireCooldown;
        fistsWeapon.range = fistsRange;
        fistsWeapon.heartRateIncrease = fistsHeartRateIncrease;
        fistsWeapon.meleeAngle = fistsMeleeAngle;
        fistsWeapon.knockback = fistsKnockback;
    }

    void Attack()
    {
        PlayerAdrenaline adrenaline = GetComponent<PlayerAdrenaline>();
        float fireCooldown = equippedWeapon.fireCooldown;
        bool isFrenzy = (adrenaline != null && adrenaline.isFrenzyActive);

        if (isFrenzy)
        {
            fireCooldown /= adrenaline.fireRateMultiplier; // Faster fire rate during frenzy
        }

        if (equippedWeapon.weaponType == WeaponType.Ranged)
        {
            if (isReloading) return;
            if (currentAmmo <= 0) return;

            Vector2 shootDirection;
            if (!TryGetShootDirection(out shootDirection)) return;

            if (Time.time - lastAttackTime < fireCooldown) return;
            lastAttackTime = Time.time;
            
            if (!isFrenzy) currentHeartRate += equippedWeapon.heartRateIncrease;
            
            RangedAttack(shootDirection);
            return;
        }

        if (Time.time - lastAttackTime < fireCooldown) return;
        lastAttackTime = Time.time;
        
        if (!isFrenzy) currentHeartRate += equippedWeapon.heartRateIncrease;
        
        MeleeAttack();
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

    void RangedAttack(Vector2 shootDirection)
    {
        if (equippedWeapon.projectilePrefab == null)
        {
            Debug.LogWarning("Ranged weapon has no projectile prefab!");
            return;
        }
        currentAmmo = Mathf.Max(0, currentAmmo - 1);

        Vector3 spawnPosition = transform.position + (Vector3)(shootDirection * projectileSpawnOffset);
        GameObject projectile = Instantiate(equippedWeapon.projectilePrefab, spawnPosition, Quaternion.identity);
        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.linearVelocity = shootDirection * equippedWeapon.projectileSpeed;
        }

        Bullets bullet = projectile.GetComponent<Bullets>();
        if (bullet != null)
        {
            bullet.SetOwner(gameObject);
            bullet.bulletType = Bullets.BulletType.Player;
            bullet.bulletDamage = equippedWeapon.damage;
            bullet.bulletSpeed = equippedWeapon.projectileSpeed;
            bullet.ammoType = equippedWeapon.ammoType;
            bullet.SetDirection(shootDirection);
            bullet.IgnoreShooterColliders(GetComponentsInChildren<Collider2D>());
        }

        Debug.Log($"Ranged attack! Projectile fired toward {shootDirection}");
    }

    void UpdateFacingDirection()
    {
        if (moveInput.sqrMagnitude < 0.0001f) return;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            facingDirection = moveInput.x > 0f ? FacingDirection.Right : FacingDirection.Left;
        }
    }

    Vector2 GetFacingVector()
    {
        switch (facingDirection)
        {
            case FacingDirection.Left:
                return Vector2.left;
            case FacingDirection.Right:
                return Vector2.right;
            default:
                return Vector2.right;
        }
    }

    bool TryGetShootDirection(out Vector2 shootDirection)
    {
        Vector2 facing = GetFacingVector();
        Camera cam = Camera.main;
        if (cam == null)
        {
            shootDirection = facing;
            return true;
        }

        Vector2 toMouse = (Vector2)(cam.ScreenToWorldPoint(Input.mousePosition) - transform.position);
        if (toMouse.sqrMagnitude < 0.0001f)
        {
            shootDirection = facing;
            return true;
        }

        Vector2 mouseDir = toMouse.normalized;
        float halfCone = rangedAimConeAngle * 0.5f;
        float angleToFacing = Mathf.Abs(Vector2.SignedAngle(facing, mouseDir));

        if (angleToFacing > halfCone)
        {
            shootDirection = Vector2.zero;
            return false;
        }

        shootDirection = mouseDir;
        return true;
    }

    void StartReload()
    {
        if (equippedWeapon == null) return;
        if (equippedWeapon.weaponType != WeaponType.Ranged) return;
        if (isReloading) return;
        if (currentAmmo >= equippedWeapon.magazineSize) return;
        if (currentTotalAmmo <= 0) return;

        isReloading = true;
        reloadEndTime = Time.time + Mathf.Max(0f, equippedWeapon.reloadTime);
    }

    void HandleReloadTimer()
    {
        if (!isReloading) return;
        if (Time.time < reloadEndTime) return;

        isReloading = false;
        if (equippedWeapon != null && equippedWeapon.weaponType == WeaponType.Ranged)
        {
            int needed = equippedWeapon.magazineSize - currentAmmo;
            int canLoad = Mathf.Min(needed, currentTotalAmmo);
            currentAmmo += canLoad;
            currentTotalAmmo -= canLoad;
        }
    }

    void UpdateBulletsUI()
    {
        if (bulletsText == null) return;

        if (equippedWeapon == null || equippedWeapon.weaponType != WeaponType.Ranged)
        {
            bulletsText.text = "";
            return;
        }

        bulletsText.text = $"{currentAmmo}/{equippedWeapon.magazineSize} | {currentTotalAmmo}";
    }

    void UpdateReloadUI()
    {
        if (reloadText == null) return;

        if (!isReloading)
        {
            reloadText.text = "";
            return;
        }

        float remaining = Mathf.Max(0f, reloadEndTime - Time.time);
        reloadText.text = $"Reloading... {remaining:F1}s";
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector2 facing = GetFacingVector();

        // Wenn wir im Playmode sind und die FacingDirection geupdatet wird, 
        // nehmen wir das echte getFacingVector, ausserhalb nehmen wir standardmäßig rechts.
        if (!Application.isPlaying) 
        {
            facing = Vector2.right; // Default beim Editieren
        }

        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.2f);
        
        float halfCone = rangedAimConeAngle * 0.5f;
        Vector3 playerPos = transform.position;

        // Die Startrichtung ist die FacingDirection rotiert um die Hälfte des Winkels nach unten
        Vector3 startDirection = Quaternion.Euler(0, 0, -halfCone) * (Vector3)facing;

        // Zeichne den gefüllten Kreisbogen für die Zielreichweite (Aim Range) und Winkel (Angle)
        UnityEditor.Handles.DrawSolidArc(playerPos, Vector3.forward, startDirection, rangedAimConeAngle, aimRange);
        
        // Zeichne einen Umriss
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireArc(playerPos, Vector3.forward, startDirection, rangedAimConeAngle, aimRange);

        // Linien zum Zentrum
        Vector3 endDirection = Quaternion.Euler(0, 0, halfCone) * (Vector3)facing;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(playerPos, playerPos + startDirection * aimRange);
        Gizmos.DrawLine(playerPos, playerPos + endDirection * aimRange);
    }
#endif
}
