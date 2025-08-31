using System.Collections.Generic;
using UnityEngine;

public class CharacterInventory : MonoBehaviour
{
    public List<InventoryEntry> Inventory = new();
    public int InventorySlots = 9;
    public int MaxStackSize = 99;

    public void AddInventoryEntry(InventoryEntry entry)
    {
        var copy = new InventoryEntry(entry);
        copy.MaxAmount = MaxStackSize;
        if (Inventory.Count < InventorySlots)
        {
            var existingEntryCol = Inventory.FindAll(e => e.ID == copy.ID);
            foreach (var existingEntry in existingEntryCol)
            {
                if (existingEntry != null)
                {
                    Debug.Log(existingEntry.Name + " adding amount " + copy.Amount);
                    existingEntry.Amount += copy.Amount;
                    break;
                }
            }

            if (existingEntryCol.Count == 0)
            {
                Debug.Log("New inventory entry: " + copy.Name + " amount: " + copy.Amount);
                Inventory.Add(copy);
            }
        }
        else
        {
            Debug.Log("Inventory full!");
        }
    }
}

[System.Serializable]
public enum ResourceType
{
    None,
    Wood,
    Stone,
    Food,
    Metal
}

[System.Serializable]
public class InventoryEntry
{
    public string ID;
    public string Name;
    public ResourceType ResourceType;
    public int Amount;
    public int MaxAmount;
    public Sprite Icon;
    public Color Color = Color.white;

    public InventoryEntry(string id, string name, ResourceType resourceType, int amount, int maxAmount, Sprite icon, Color color)
    {
        ID = id;
        Name = name;
        ResourceType = resourceType;
        Amount = amount;
        MaxAmount = maxAmount;
        Icon = icon;
        Color = color;
    }

    public InventoryEntry(InventoryEntry entry)
    {
        ID = entry.ID;
        Name = entry.Name;
        ResourceType = entry.ResourceType;
        Amount = entry.Amount;
        MaxAmount = entry.MaxAmount;
        Icon = entry.Icon;
        Color = entry.Color;
    }
}