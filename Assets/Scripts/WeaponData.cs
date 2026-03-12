using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged
}

public enum AmmoType
{
    Small,
    Medium,
    Big
}

public class WeaponData
{
    public string weaponName;
    public WeaponType weaponType;
    public Sprite icon;
    public float damage;
    public float attackRate;
    public float fireCooldown = 0.2f;
    public float range;
    public float heartRateIncrease = 10f;
    public float meleeAngle = 90f;
    public float knockback = 2f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public AmmoType ammoType = AmmoType.Small;
    public int magazineSize = 10;
    public float reloadTime = 1.5f;
    public int totalAmmo = 30;
}
