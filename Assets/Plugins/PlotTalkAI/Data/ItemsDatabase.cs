using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Items Database")]
public class ItemsDatabase : ScriptableObject
{
    public List<InventoryItem> items;
}