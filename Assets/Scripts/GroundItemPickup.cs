using UnityEngine;

public class GroundItemPickup : MonoBehaviour
{
    public enum ItemType { None, Weapon, Medical }

    [Header("Item Type")]
    public ItemType itemType = ItemType.None;

    [Header("Weapon Properties")]
    public WeaponType weaponType = WeaponType.Melee;
    public string weaponName = "";
    public Sprite weaponIcon;
    public float damage = 10f;
    public float attackRate = 1f;
    public float fireCooldown = 0.2f;
    public float range = 1f;
    public float heartRateIncrease = 10f;
    public float meleeAngle = 90f;
    public float knockback = 2f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public AmmoType ammoType = AmmoType.Small;
    public int magazineSize = 10;
    public float reloadTime = 1.5f;

    [Header("Ammo Reserves")]
    public int smallAmmoReserve = 60;
    public int mediumAmmoReserve = 40;
    public int bigAmmoReserve = 20;

    [Header("Medical Properties")]
    public string medicalName = "";
    public float healingAmount = 25f;

    [Header("Input")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Visual")]
    public float worldIconScale = 2f;

    private bool playerInRange;
    private PlayerInventory playerInventory;
    private SpriteRenderer worldSpriteRenderer;

    void Awake()
    {
        worldSpriteRenderer = GetComponent<SpriteRenderer>();
        if (worldSpriteRenderer != null)
        {
            transform.localScale = Vector3.one * worldIconScale;
        }
    }

    void Start()
    {
        RefreshVisual();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = other.GetComponentInParent<PlayerInventory>();
        }
        if (inventory == null) return;

        playerInRange = true;
        playerInventory = inventory;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = other.GetComponentInParent<PlayerInventory>();
        }
        if (inventory == null) return;

        if (inventory == playerInventory)
        {
            playerInRange = false;
            playerInventory = null;
        }
    }

    void Update()
    {
        if (!playerInRange || playerInventory == null) return;
        if (!Input.GetKeyDown(pickupKey)) return;

        if (itemType == ItemType.Weapon)
        {
            var weapon = CreateWeaponData();
            if (weapon != null)
            {
                WeaponData previousSameType = weapon.weaponType == WeaponType.Melee
                    ? playerInventory.meleeSlot
                    : playerInventory.rangedSlot;
                bool dropPreviousWeapon = previousSameType != null;
                if (dropPreviousWeapon && previousSameType.weaponName == "Fists")
                {
                    dropPreviousWeapon = false;
                }

                playerInventory.AddWeapon(weapon);

                if (dropPreviousWeapon)
                {
                    ApplyWeaponData(previousSameType);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            return;
        }

        if (itemType == ItemType.Medical)
        {
            var medical = CreateMedicalData();
            if (medical != null)
            {
                playerInventory.AddMedicalItem(medical);
                Destroy(gameObject);
            }
            
        }
    }

    WeaponData CreateWeaponData()
    {
        var weapon = new WeaponData();
        weapon.weaponName = weaponName;
        weapon.weaponType = weaponType;
        weapon.icon = weaponIcon;
        weapon.damage = damage;
        weapon.attackRate = attackRate;
        weapon.fireCooldown = fireCooldown;
        weapon.range = range;
        weapon.heartRateIncrease = heartRateIncrease;
        weapon.meleeAngle = meleeAngle;
        weapon.knockback = knockback;
        weapon.projectilePrefab = projectilePrefab;
        weapon.projectileSpeed = projectileSpeed;
        weapon.ammoType = ammoType;
        weapon.magazineSize = magazineSize;
        weapon.reloadTime = reloadTime;
        switch (ammoType)
        {
            case AmmoType.Small:  weapon.totalAmmo = smallAmmoReserve;  break;
            case AmmoType.Medium: weapon.totalAmmo = mediumAmmoReserve; break;
            case AmmoType.Big:    weapon.totalAmmo = bigAmmoReserve;    break;
        }
        return weapon;
    }

    void ApplyWeaponData(WeaponData weapon)
    {
        if (weapon == null) return;

        itemType = ItemType.Weapon;
        weaponType = weapon.weaponType;
        weaponName = weapon.weaponName;
        weaponIcon = weapon.icon;
        damage = weapon.damage;
        attackRate = weapon.attackRate;
        fireCooldown = weapon.fireCooldown;
        range = weapon.range;
        heartRateIncrease = weapon.heartRateIncrease;
        meleeAngle = weapon.meleeAngle;
        knockback = weapon.knockback;
        projectilePrefab = weapon.projectilePrefab;
        projectileSpeed = weapon.projectileSpeed;
        ammoType = weapon.ammoType;
        magazineSize = weapon.magazineSize;
        reloadTime = weapon.reloadTime;
        switch (weapon.ammoType)
        {
            case AmmoType.Small:  smallAmmoReserve  = weapon.totalAmmo; break;
            case AmmoType.Medium: mediumAmmoReserve = weapon.totalAmmo; break;
            case AmmoType.Big:    bigAmmoReserve    = weapon.totalAmmo; break;
        }
        RefreshVisual();
    }

    MedicalItemData CreateMedicalData()
    {
        var medical = new MedicalItemData();
        medical.itemName = medicalName;
        medical.healingAmount = healingAmount;
        return medical;
    }

    void RefreshVisual()
    {
        if (worldSpriteRenderer == null) return;

        if (itemType == ItemType.Weapon)
        {
            worldSpriteRenderer.sprite = weaponIcon;
        }
    }
}
