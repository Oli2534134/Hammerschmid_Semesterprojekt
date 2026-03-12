using UnityEngine;

public class Enemy : MonoBehaviour
{
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

    [Header("Weapon System")]
    public GroundItemPickup[] weaponPool;
    [Range(0f, 1f)]
    public float weaponSpawnChance = 0.7f;
    public GameObject weaponPickupPrefab;
    public float heldWeaponScale = 2f;

    private WeaponData equippedWeapon;
    private PlayerController targetPlayer;
    private float lastAttackTime = -Mathf.Infinity;
    private SpriteRenderer weaponSpriteRenderer;

    void Start()
    {
        SetupEnemy();
        targetPlayer = FindFirstObjectByType<PlayerController>();
        EquipRandomWeapon();

    }

    void Update()
    {
        if (health <= 0f)
        {
            Die();
            return;
        }

        if (targetPlayer == null) return;

        Vector2 enemyPosition = transform.position;
        Vector2 playerPosition = targetPlayer.transform.position;
        float distanceToPlayer = Vector2.Distance(enemyPosition, playerPosition);

        if (distanceToPlayer > attackRange)
        {
            Vector2 direction = (playerPosition - enemyPosition).normalized;
            float step = speed * Time.deltaTime;
            float maxMove = distanceToPlayer - attackRange;
            float moveDistance = Mathf.Min(step, maxMove);
            transform.position = enemyPosition + direction * moveDistance;
        }
        else
        {
            TryAttackPlayer();
        }
    }

    void SetupEnemy()
    {
        float defaultHealth = health;
        float defaultSpeed = speed;
        float defaultDamage = damage;

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
                break;
        }

        if (health <= 0f) health = defaultHealth;
        if (speed <= 0f) speed = defaultSpeed;
        if (damage <= 0f) damage = defaultDamage;
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
        targetPlayer.TakeDamage(effectiveDamage);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"Enemy took {amount} damage! Health: {health}");
    }

    void Die()
    {
        if (equippedWeapon != null)
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
                pickup.reloadTime = equippedWeapon.reloadTime;
            }
        }

        Debug.Log("Enemy died!");
        Destroy(gameObject);
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
        if (Random.value > weaponSpawnChance) return;

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