using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Slots")]
    public MedicalItemData medicalSlot;
    public WeaponData meleeSlot;
    public WeaponData rangedSlot;

    [HideInInspector]
    public WeaponData equippedWeapon;
    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (meleeSlot != null)
            {
                EquipWeapon(meleeSlot);
            }
            else if (playerController != null)
            {
                EquipWeapon(playerController.GetFallbackMeleeWeapon());
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipWeapon(rangedSlot);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            ConsumeMedicalItem();
        }
    }

    void ConsumeMedicalItem()
    {
        if (medicalSlot == null) return;
        if (playerController == null) return;

        playerController.Heal(medicalSlot.healingAmount);
        medicalSlot = null;
    }

    public void AddMedicalItem(MedicalItemData item)
    {
        if (item == null) return;
        medicalSlot = item;
    }

    public void AddWeapon(WeaponData weapon)
    {
        if (weapon == null) return;

        switch (weapon.weaponType)
        {
            case WeaponType.Melee:
                meleeSlot = weapon;
                EquipWeapon(weapon);
                break;
            case WeaponType.Ranged:
                rangedSlot = weapon;
                EquipWeapon(weapon);
                break;
        }
    }

    public void EquipWeapon(WeaponData weapon)
    {
        equippedWeapon = weapon;
        if (playerController != null)
        {
            playerController.SetEquippedWeapon(weapon);
        }
    }
}
