using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    public string name;
    public string description;
    public Sprite icon;

    public InventoryItem(string name, string description, Sprite icon)
    {
        this.name = name;
        this.description = description;
        this.icon = icon;
    }
}