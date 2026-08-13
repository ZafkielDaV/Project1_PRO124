using UnityEngine;

public enum LootType { Coin, HealthPotion, ManaPotion, RareItem }

[CreateAssetMenu(fileName = "NewLootItem", menuName = "Loot/Loot Item")]
public class LootItemData : ScriptableObject
{
    public LootType type;
    public GameObject prefab;      // prefab world-drop (sprite + rigidbody2d)
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0, 100)] public float weight = 10f; // trọng số random
}