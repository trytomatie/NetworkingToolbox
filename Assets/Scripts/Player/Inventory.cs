using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Inventory : NetworkBehaviour
{

    public Item[] items = new Item[maxItemSlots];
    [SerializeField] private const int maxItemSlots = 40;

    private void Start()
    {
        if (!IsOwner)
            return;

    }

    [ClientRpc]
    public void AddItemClientRPC(ulong id, int stackSize, ClientRpcParams clientRpcParams = default)
    {
        AddItem(id, stackSize);
    }
    private bool AddItem(ulong id,int stackSize)
    {
        Item item = ItemManager.GenerateItem(id);
        item.stackSize = stackSize;
        if(item != null)
        {
            Item itemToStackOn;
            if (ItemAlreadyInventoryAndHasSpaceOnStack(id, out itemToStackOn))
            {
                itemToStackOn.stackSize += item.stackSize;
                if(itemToStackOn.stackSize > itemToStackOn.maxStackSize)
                {
                    int rest = itemToStackOn.stackSize - itemToStackOn.maxStackSize;
                    itemToStackOn.stackSize = itemToStackOn.maxStackSize;
                    AddItem(id, rest);
                }
                InventoryManagerUI.Instance.RefreshUI();
                return true;
            }
            // find space for added Item
            bool spaceFound = false;
            for(int i = 0; i < maxItemSlots - 1;i++)
            {
                if(items[i] == null)
                {
                    items[i] = item;
                    spaceFound = true;
                    break;
                }
            }
            InventoryManagerUI.Instance.RefreshUI();
            return spaceFound;
        }
        else
        {
            return false;
        }
    }

    public void RemoveItem(ulong id, int amount)
    {
        for(int slot = 0; slot < maxItemSlots;slot++)
        {
            if (items[slot] != null)
            {
                print(items[slot].itemId);
                if (items[slot].itemId == id)
                {
                    if (amount > items[slot].stackSize)
                    {
                        amount -= items[slot].stackSize;
                        items[slot] = null;
                    }
                    else if (amount < items[slot].stackSize)
                    {
                        items[slot].stackSize -= amount;
                        amount = 0;
                    }
                    else if (amount == items[slot].stackSize)
                    {
                        items[slot] = null;
                    }
                }
            }
        }
        InventoryManagerUI.Instance.RefreshUI();
    }


    private bool ItemAlreadyInventoryAndHasSpaceOnStack(ulong id, out Item itemToStackOn)
    {
        foreach(Item item in items)
        {
            if(CheckItemId(item,id))
            {
                // there is no space left, so it is considered not in inventory for the purpose of stacking
                if(item.stackSize == item.maxStackSize)
                {
                    continue;
                }
                else
                {
                    itemToStackOn = item;
                    return true;
                }
            }
        }
        itemToStackOn = null;
        return false;
    }

    private bool CheckItemId(Item item,ulong idToCheck)
    {
        if(item == null || item.itemId != idToCheck)
        {
            return false;
        }
        return true;
    }

    private bool CheckItemName(Item item, string nameToCheck)
    {
        if (item == null || item.itemName != nameToCheck)
        {
            return false;
        }
        return true;
    }

    public bool SwapItemPlaces(int item1Pos,int item2Pos)
    {
        bool item1Present =  items[item1Pos] != null;
        bool item2Present = items[item2Pos] != null;

        if (item1Present || item2Present)
        {

            if (item1Present && item2Present)
            {
                Item item1 = items[item1Pos];
                Item item2 = items[item2Pos];
                items[item1Pos] = null;
                items[item2Pos] = null;
                items[item2Pos] = item1;
                items[item1Pos] = item2;
            }
            else
            if(item1Present && !item2Present)
            {
                Item item1 = items[item1Pos];
                items[item1Pos] = null;
                items[item2Pos] = item1;
            }
            else
            if (!item1Present && item2Present)
            {
                Item item2 = items[item2Pos];
                items[item2Pos] = null;
                items[item1Pos] = item2;
            }
            InventoryManagerUI.Instance.RefreshUI();
            return true;

        }

        return false;
    }

    public int GetAmmountOfItem(ulong id)
    {
        int amount = 0;
        foreach(Item item in items)
        {
            if(CheckItemId(item,id))
            {
                amount += item.stackSize;
            }
        }
        return amount;
    }

    public int GetAmmountOfItem(string itemName)
    {
        int amount = 0;
        foreach (Item item in items)
        {
            if (CheckItemName(item,itemName))
            {
                amount += item.stackSize;
            }
        }
        return amount;
    }
}
