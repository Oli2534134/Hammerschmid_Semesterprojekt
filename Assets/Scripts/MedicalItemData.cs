using UnityEngine;

public enum MedicalEffectType
{
    Health,
    HeartRate
}

public class MedicalItemData
{
    public string itemName;
    public Sprite icon;
    public MedicalEffectType effectType = MedicalEffectType.Health;
    public float effectAmount = 25f;
}
