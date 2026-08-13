using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootItemData> items;
    public int minRolls = 2;
    public int maxRolls = 5;

    public List<(LootItemData data, int amount)> RollLoot()
    {
        var result = new List<(LootItemData, int)>();
        int rolls = Random.Range(minRolls, maxRolls + 1);

        float totalWeight = 0f;
        foreach (var item in items) totalWeight += item.weight;

        for (int i = 0; i < rolls; i++)
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var item in items)
            {
                cumulative += item.weight;
                if (roll <= cumulative)
                {
                    int amount = Random.Range(item.minAmount, item.maxAmount + 1);
                    result.Add((item, amount));
                    break;
                }
            }
        }
        return result;
    }
}