using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General")]
    public string weaponName;
    public WeaponType weaponType;
    public Sprite icon;
    public float damage;
    public float attackRate;
    public float range;
    public float heartRateIncrease = 10f;

    [Header("Melee")]
    public float meleeAngle = 90f;
    public float knockback = 2f;

    [Header("Ranged")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public int magazineSize = 10;
    public float reloadTime = 1.5f;
}
