using MalbersAnimations.Events;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static Item;

public class Container : NetworkBehaviour
{
    public ItemData[] items;
    public UnityEvent<Item> addItemEvents;
    public List<ulong> observerList = new List<ulong>() { 0 };
    public MEvent observationEvent;

    private void Start()
    {
        if(IsServer)
        {
            AddToObserverListServerRpc(NetworkManager.ServerClientId);
            AddToObserverListServerRpc(OwnerClientId);
        }
    }

    [ServerRpc (RequireOwnership = false)]
    public void RequestItemSwapServerRpc(NetworkBehaviourReference otherContainerRef,int pos1, int pos2,ItemData validationData1,ItemData validationData2)
    {
        Container otherContainer;
        print($"{pos1} {pos2} {validationData1.itemId} {validationData2.itemId} {validationData1.stackSize} {validationData2.stackSize} {gameObject.name}");
        if (otherContainerRef.TryGet(out otherContainer)) // Both Inventory References are valid / not null
        {
            if (ValidateDataSwap(otherContainer, pos1, pos2, validationData1, validationData2)) // The Item Positions are also Valid
            {
                print("Valide");
                // Swap Items here
                // Replicate this Operation on all Observers

                ClientRpcParams clientRpcParams = GameManager.GetClientRpcParams(observerList.ToArray());
                SwapItemsClientRpc(pos1, pos2, validationData1, validationData2, otherContainer, clientRpcParams);
            }
        }
        else
        {
            Debug.LogError("Other Container not Valid / is null");
        }
    }
    [ClientRpc]
    private void SwapItemsClientRpc(int pos1, int pos2, ItemData validationData1, ItemData validationData2, NetworkBehaviourReference otherContainerRef, ClientRpcParams clientRpcParams = default)
    {
        Container otherContainer;
        if (otherContainerRef.TryGet(out otherContainer)) // Both Inventory References are valid / not null
        {
            otherContainer.items[pos2] = validationData1;
            items[pos1] = validationData2;
            observationEvent.Invoke(gameObject);
            otherContainer.observationEvent.Invoke(otherContainer.gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRemoveItemServerRpc(ItemData itemData)
    {
        int amount = itemData.stackSize;
        for (int i = items.Length - 1; i >= 0; i--)
        {
            if (itemData.itemId == items[i].itemId)
            {
                if (items[i].stackSize >= amount)
                {
                    // If stackSize is greater than or equal to amount, subtract amount
                    RemoveItem(new ItemData(items[i].itemId, amount),i);
                    amount = 0;  
                    break;
                }
                else
                {
                    // If stackSize is smaller, subtract stackSize and adjust amount
                    amount -= items[i].stackSize;
                    RemoveItem(new ItemData(items[i].itemId, items[i].stackSize), i);
                }
            }
        }
    }

    [ServerRpc (RequireOwnership =false)]
    public void RequestRemoveItemServerRpc(ItemData itemData,int pos)
    {
        RemoveItem(itemData, pos);
    }

    private void RemoveItem(ItemData itemData, int pos)
    {
        if (ValidateData(this, pos, itemData))
        {
            RemoveItemClientRpc(itemData, pos);
        }
    }

    [ClientRpc]
    private void RemoveItemClientRpc(ItemData data, int pos)
    {
        if (items[pos].stackSize >= data.stackSize) // if The stacksize is sufficent
        {
            items[pos].stackSize -= data.stackSize;
            if(items[pos].stackSize == 0)
            {
                items[pos] = ItemData.Null;
            }
            observationEvent.Invoke(gameObject);
        }
        else // data missmatch must have happend / Invalid Request
        {
            Debug.LogError($"Data Missmatch happened Client: {NetworkManager.LocalClientId} for Removing Items");
            SyncContainerServerRpc(NetworkManager.LocalClientId);
        }
    }

    private bool ValidateDataSwap(Container otherContainer, int pos1,int pos2,ItemData validationData1,ItemData validationData2)
    {
        // Just checks if the item on that position has the same id and stacksize (Very rudementery I know! But shouldnt lead to any bugs (Right?))
        if(items[pos1] == validationData1 && otherContainer.items[pos2] == validationData2) 
        {
            return true;
        }
        return false;
    }

    private static bool ValidateData(Container container,int pos,ItemData validationData)
    {
        if(container.items[pos] == validationData)
        {
            return true;
        }
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddToObserverListServerRpc(ulong id)
    {
        if (!observerList.Contains(id))
        {
            SyncContainerServerRpc(id);
            observerList.Add(id);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveFromObserverListServerRpc(ulong id)
    {
        if (id == NetworkManager.ServerClientId) // the Server is always observing! You cannot get rid of him
            return;
        if (observerList.Contains(id))
        {

            observerList.Remove(id);
        }
    }


    [ServerRpc]
    public void SyncContainerServerRpc(ulong clientId)
    {
        SyncContainerClientRpc(items, GameManager.GetClientRpcParams(clientId));
    }

    [ClientRpc]
    public void SyncContainerClientRpc(ItemData[] serverItems, ClientRpcParams clientRpcParams = default)
    {
        items = serverItems;
        observationEvent.Invoke(gameObject);
    }

    [ServerRpc (RequireOwnership =false)]
    public void AddItemServerRpc(ItemData item)
    {
        ClientRpcParams clientRpcParams = GameManager.GetClientRpcParams(observerList.ToArray());
        AddItemClientRpc(item, clientRpcParams);
    }

    [ClientRpc]
    private void AddItemClientRpc(ItemData itemData,ClientRpcParams clientRpcParams = default)
    {

        Item item = ItemManager.GenerateItem(itemData.itemId);
        item.stackSize = itemData.stackSize;
        if (item != null)
        {
            int itemToStackOnPos;
            if (ItemAlreadyInventoryAndHasSpaceOnStack(itemData.itemId, out itemToStackOnPos))
            {
                items[itemToStackOnPos].stackSize += item.stackSize;
                if (items[itemToStackOnPos].stackSize > items[itemToStackOnPos].MaxStackSize)
                {
                    int rest = items[itemToStackOnPos].stackSize - items[itemToStackOnPos].MaxStackSize;
                    items[itemToStackOnPos].stackSize = items[itemToStackOnPos].MaxStackSize;
                    ItemData restData = new ItemData(itemData.itemId, rest);
                    AddItemClientRpc(restData);
                }
                addItemEvents.Invoke(item);
                if(NetworkManager.LocalClientId == OwnerClientId)
                {
                    NotificationManagerUI.Instance.SetNotification(item);
                }
                observationEvent.Invoke(gameObject);
                return;
            }
            // find space for added Item
            bool spaceFound = false;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == ItemData.Null)
                {
                    items[i] = itemData;
                    spaceFound = true;
                    break;
                }
            }
            if(!spaceFound)
            {
                Debug.LogError("No Space in Container");
            }
            addItemEvents.Invoke(item);
            if (NetworkManager.LocalClientId == OwnerClientId)
            {
                NotificationManagerUI.Instance.SetNotification(item);
            }
            observationEvent.Invoke(gameObject);
            return;
        }
        else
        {
            return;
        }
    }

    private bool ItemAlreadyInventoryAndHasSpaceOnStack(ulong id, out int itemToStackOnId)
    {
        int i = 0;
        foreach (ItemData item in items)
        {
            if (CheckItemId(item.itemId, id))
            {
                print($"{item.stackSize} {ItemManager.GetMaxStackSize(id)}");
                // there is no space left, so it is considered not in inventory for the purpose of stacking
                if (item.stackSize == ItemManager.GetMaxStackSize(id))
                {
                    i++;
                    continue;
                }
                else
                {
                    itemToStackOnId = i;
                    return true;
                }
            }
            i++;
        }
        itemToStackOnId = -1;
        return false;
    }

    public bool HasItemSpaceInInventory(ItemData item)
    {
        int itemToStackOnPos;
        if (ItemAlreadyInventoryAndHasSpaceOnStack(item.itemId, out itemToStackOnPos))
        {
            items[itemToStackOnPos].stackSize += item.stackSize;
            if (items[itemToStackOnPos].stackSize > items[itemToStackOnPos].MaxStackSize)
            {
                int rest = items[itemToStackOnPos].stackSize - items[itemToStackOnPos].MaxStackSize;
                items[itemToStackOnPos].stackSize = items[itemToStackOnPos].MaxStackSize;
                ItemData restData = new ItemData(item.itemId, rest);
                HasItemSpaceInInventory(restData);
            }
            return true;
        }
        // find space for added Item
        bool spaceFound = false;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                spaceFound = true;
                break;
            }
        }
        return spaceFound;
    }

    public int FindItemFromBehind(ItemData item)
    {
        for(int i = items.Length-1;i >= 0;i--)
        {
            if(item.itemId == items[i].itemId)
            {
                return i;
            }
        }
        return -1;
    }

    public int GetAmmountOfItem(ulong id)
    {
        int amount = 0;
        foreach (ItemData item in items)
        {
            if (CheckItemId(item.itemId, id))
            {
                amount += item.stackSize;
            }
        }
        return amount;
    }

    private bool CheckItemId(ulong id1, ulong id2)
    {
        if (id1 == id2)
        {
            return true;
        }
        return false;
    }
}
