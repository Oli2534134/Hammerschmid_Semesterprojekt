using UnityEngine;

public class Enemy : MonoBehaviour
{
    [System.Serializable]
    public class RandomItemDrop
    {
        public GroundItemPickup pickupPrefab;
        [Range(0f, 1f)]
        public float dropChance = 0.35f;
    }

    public enum EnemyType
    {
        Melee,
        Ranged,
        Tank
    }

    public EnemyType type;

    public float health;
    public float speed;
    public float damage;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;
    public float attackDelayAfterEnteringRange = 1f;

    [Header("Weapon System")]
    public GroundItemPickup[] weaponPool;
    [Range(0f, 1f)]
    public float weaponSpawnChance = 0.7f;
    [Range(0f, 1f)]
    [Tooltip("Chance (0 bis 1), mit der die getragene Waffe beim Tod gedroppt wird.")]
    public float weaponDropChance = 0.3f;
    public GameObject weaponPickupPrefab;
    public float heldWeaponScale = 2f;

    [Header("Random Heal Drops")]
    public RandomItemDrop[] randomItemDrops;

    [Header("Ammo Reward on Kill")]
    [Range(0f, 1f)]
    public float ammoRewardChance = 0.4f;
    public int minAmmoReward = 5;
    public int maxAmmoReward = 15;

    private WeaponData equippedWeapon;
    private PlayerController targetPlayer;
    private float lastAttackTime = -Mathf.Infinity;
    private SpriteRenderer weaponSpriteRenderer;
    private float timeInAttackRange = 0f;

    public Animator animator;
    public bool hasCrowbar;
    private UnityEngine.Rendering.SortingGroup sortingGroup;
    private Rigidbody2D rb;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        // Machen den Zombie schwer, damit der Spieler ihn nicht herumschieben kann
        rb.mass = 200f;
        rb.linearDamping = 0f;

        sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sortingGroup == null)
        {
            sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
        }
        
        SetupEnemy();
        targetPlayer = FindFirstObjectByType<PlayerController>();
        EquipRandomWeapon();
        
        if (animator != null)
        {
            animator.SetBool("hasCrowbar", hasCrowbar);
        }
    }

    void FixedUpdate()
    {
        if (health <= 0f)
        {
            Die();
            return;
        }

        if (targetPlayer == null)
        {
            if (animator != null) animator.SetBool("isWalking", false);
            return;
        }

        // Fix Layer-Flickering anhand der Y-Position (Y-Sorting)
        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = Mathf.RoundToInt(rb.position.y * -100f);
        }

        Vector2 enemyPosition = rb.position;
        Vector2 playerPosition = targetPlayer.transform.position;
        float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition);

        // Gegner in Blickrichtung des Spielers spiegeln
        if (playerPosition.x > enemyPosition.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (playerPosition.x < enemyPosition.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        if (distanceToPlayer > attackRange)
        {
            Vector2 direction = (playerPosition - enemyPosition).normalized;
            rb.linearVelocity = direction * speed; // Bewegung sicher über Velocity
            timeInAttackRange = 0f;

            if (animator != null) animator.SetBool("isWalking", true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Anhalten

            if (animator != null) animator.SetBool("isWalking", false);

            timeInAttackRange += Time.fixedDeltaTime;
            if (timeInAttackRange >= attackDelayAfterEnteringRange)
            {
                TryAttackPlayer();
            }
        }
    }

    void SetupEnemy()
    {
        float defaultHealth = health;
        float defaultSpeed = speed;
        float defaultDamage = damage;
        float defaultAttackCooldown = attackCooldown <= 0f || attackCooldown > 5f ? 1.5f : attackCooldown;

        switch (type)
        {
            case EnemyType.Melee:
                defaultHealth = 100;
                defaultSpeed = 4;
                defaultDamage = 10;
                break;

            case EnemyType.Ranged:
                defaultHealth = 70;
                defaultSpeed = 3;
                defaultDamage = 15;
                break;

            case EnemyType.Tank:
                defaultHealth = 200;
                defaultSpeed = 1.5f;
                defaultDamage = 25;
                if (attackCooldown > 5f || attackCooldown <= 0f) defaultAttackCooldown = 4f;
                break;
        }

        if (health <= 0f) health = defaultHealth;
        if (speed <= 0f) speed = defaultSpeed;
        if (damage <= 0f) damage = defaultDamage;
        
        // Zwinge fehlerhafte Inspector-Werte (z.B. > 5 Sekunden) auf normale Raten herunter
        attackCooldown = defaultAttackCooldown;
    }

    void TryAttackPlayer()
    {
        float effectiveCooldown = attackCooldown;
        float effectiveDamage = damage;

        if (equippedWeapon != null)
        {
            effectiveCooldown = 1f / equippedWeapon.attackRate;
            effectiveDamage = equippedWeapon.damage;
        }

        if (Time.time - lastAttackTime < effectiveCooldown) return;
        lastAttackTime = Time.time;
        
        if (animator != null)
        {
            StartCoroutine(PunchAnimationRoutine());
        }
        
        targetPlayer.TakeDamage(effectiveDamage);
    }

    private System.Collections.IEnumerator PunchAnimationRoutine()
    {
        animator.SetBool("isPunching", true);
        // Warte für eine halbe Sekunde (oder die Länge deiner Animation)
        yield return new WaitForSeconds(0.5f); 
        animator.SetBool("isPunching", false);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"Enemy took {amount} damage! Health: {health}");
        SpawnBloodParticles();
    }

    void SpawnBloodParticles()
    {
        GameObject bloodObj = new GameObject("BloodParticles");
        bloodObj.transform.position = transform.position;
        ParticleSystem ps = bloodObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startColor = new Color(0.8f, 0f, 0f); // Dark red
        main.startLifetime = Random.Range(0.2f, 0.5f);
        main.startSpeed = Random.Range(2f, 5f);
        main.startSize = Random.Range(0.1f, 0.3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = 1f;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[]{ new ParticleSystem.Burst(0f, (short)Random.Range(10, 20)) });
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.8f, 0f, 0f), 0.0f), new GradientColorKey(new Color(0.5f, 0f, 0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // Die Partikel sicher auf einen Layer über dem Gegner zeichnen lassen
        if (sortingGroup != null)
        {
            renderer.sortingOrder = sortingGroup.sortingOrder + 10;
        }
        else
        {
            renderer.sortingOrder = 32000;
        }

        Destroy(bloodObj, 1f);
    }

    void Die()
    {
        if (targetPlayer != null)
        {
            PlayerAdrenaline adrenaline = targetPlayer.GetComponent<PlayerAdrenaline>();
            if (adrenaline != null)
            {
                adrenaline.OnEnemyKill();
            }
        }

        TryDropRandomHealItem();

        if (equippedWeapon != null && Random.value <= weaponDropChance)
        {
            GroundItemPickup pickup = CreateDropPickup();
            if (pickup != null)
            {
                pickup.itemType = GroundItemPickup.ItemType.Weapon;
                pickup.weaponType = equippedWeapon.weaponType;
                pickup.weaponName = equippedWeapon.weaponName;
                pickup.weaponIcon = equippedWeapon.icon;
                pickup.damage = equippedWeapon.damage;
                pickup.attackRate = equippedWeapon.attackRate;
                pickup.range = equippedWeapon.range;
                pickup.heartRateIncrease = equippedWeapon.heartRateIncrease;
                pickup.meleeAngle = equippedWeapon.meleeAngle;
                pickup.knockback = equippedWeapon.knockback;
                pickup.projectilePrefab = equippedWeapon.projectilePrefab;
                pickup.projectileSpeed = equippedWeapon.projectileSpeed;
                pickup.ammoType = equippedWeapon.ammoType;
                pickup.magazineSize = equippedWeapon.magazineSize;
                pickup.currentMagazineAmmo = equippedWeapon.currentMagazineAmmo;
                pickup.reloadTime = equippedWeapon.reloadTime;
            }
        }

        Debug.Log("Enemy died!");
        Destroy(gameObject);
    }

    void TryDropRandomHealItem()
    {
        if (randomItemDrops == null || randomItemDrops.Length == 0) return;

        var validDrops = new System.Collections.Generic.List<GroundItemPickup>();
        for (int i = 0; i < randomItemDrops.Length; i++)
        {
            RandomItemDrop entry = randomItemDrops[i];
            if (entry == null || entry.pickupPrefab == null) continue;
            if (entry.pickupPrefab.itemType != GroundItemPickup.ItemType.Medical) continue;
            if (Random.value <= Mathf.Clamp01(entry.dropChance))
            {
                validDrops.Add(entry.pickupPrefab);
            }
        }

        if (validDrops.Count == 0) return;

        GroundItemPickup chosen = validDrops[Random.Range(0, validDrops.Count)];
        Instantiate(chosen.gameObject, transform.position, Quaternion.identity);
    }

    GroundItemPickup CreateDropPickup()
    {
        GameObject dropObject = null;

        if (weaponPickupPrefab != null)
        {
            dropObject = Instantiate(weaponPickupPrefab, transform.position, Quaternion.identity);
            GroundItemPickup prefabPickup = dropObject.GetComponent<GroundItemPickup>();
            if (prefabPickup != null)
            {
                return prefabPickup;
            }

            Destroy(dropObject);
        }

        dropObject = new GameObject("DroppedWeaponPickup");
        dropObject.transform.position = transform.position;

        var spriteRenderer = dropObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 1;

        var collider = dropObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        return dropObject.AddComponent<GroundItemPickup>();
    }

    void EquipRandomWeapon()
    {
        if (weaponPool == null || weaponPool.Length == 0) return;
        
        bool getsWeapon = Random.value <= weaponSpawnChance;
        if (type == EnemyType.Melee || type == EnemyType.Ranged) // "enemy(melee and small)"
        {
            hasCrowbar = getsWeapon;
            if (animator != null)
            {
                animator.SetBool("hasCrowbar", hasCrowbar);
            }
        }
        
        if (!getsWeapon) return;

        WeaponType desiredType = (type == EnemyType.Ranged) ? WeaponType.Ranged : WeaponType.Melee;
        
        // Filter weapons that match enemy type
        var validWeapons = new System.Collections.Generic.List<GroundItemPickup>();
        foreach (var pickup in weaponPool)
        {
            if (pickup != null && pickup.itemType == GroundItemPickup.ItemType.Weapon && pickup.weaponType == desiredType)
            {
                validWeapons.Add(pickup);
            }
        }

        if (validWeapons.Count == 0) return;

        GroundItemPickup randomPickup = validWeapons[Random.Range(0, validWeapons.Count)];

        equippedWeapon = new WeaponData();
        equippedWeapon.weaponName = randomPickup.weaponName;
        equippedWeapon.weaponType = randomPickup.weaponType;
        equippedWeapon.icon = randomPickup.weaponIcon;
        equippedWeapon.damage = randomPickup.damage;
        equippedWeapon.attackRate = randomPickup.attackRate;
        equippedWeapon.range = randomPickup.range;
        equippedWeapon.heartRateIncrease = randomPickup.heartRateIncrease;
        equippedWeapon.meleeAngle = randomPickup.meleeAngle;
        equippedWeapon.knockback = randomPickup.knockback;
        equippedWeapon.projectilePrefab = randomPickup.projectilePrefab;
        equippedWeapon.projectileSpeed = randomPickup.projectileSpeed;
        equippedWeapon.ammoType = randomPickup.ammoType;
        equippedWeapon.magazineSize = randomPickup.magazineSize;
        equippedWeapon.reloadTime = randomPickup.reloadTime;
    }
}