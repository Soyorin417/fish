using System;
using System.Collections.Generic;

[Serializable]
public class InventoryItemJson
{
    public string itemId = "";
    public int amount = 0;
}

[Serializable]
public class InventoryJsonRoot
{
    public List<InventoryItemJson> items =
        new List<InventoryItemJson>();
}