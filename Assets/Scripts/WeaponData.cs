using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged
}

public class WeaponData
{
    public string weaponName;
    public WeaponType weaponType;
    public Sprite icon;
    public float damage;
    public float attackRate;
    public float range;
    public float heartRateIncrease = 10f;
    public float meleeAngle = 90f;
    public float knockback = 2f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public int magazineSize = 10;
    public float reloadTime = 1.5f;
}
