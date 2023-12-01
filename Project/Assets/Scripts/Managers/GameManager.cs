﻿using MalbersAnimations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static Item;

public class GameManager : NetworkBehaviour
{
    private static GameManager instance;
    public GameUI gameUI;
    public MInput inputManager;
    public NetworkPrefabsList networkPrefabsList;
    public Gradient myColor;
    public Gradient otherPlayerColor;
    public string selectedPlayer;
    public Dictionary<ulong, GameObject> playerList = new Dictionary<ulong, GameObject>();
    public string relayCode = "";
    public WorldSaveState worldSaveState;
    public List<BuildingBeacon> buildingBeacons;
    public UnityEvent disconnectEvent;
    public PlayerSaveData playerSaveData;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetSelectedPlayer(string value)
    {
        selectedPlayer = value;
    }

    public void LoadPlayerData(PlayerCustomization customization)
    {
        playerSaveData.customization = customization;
        playerSaveData.LoadPlayerData(selectedPlayer);
    }

    private void Start()
    {
        RegisterNetworkPrefabs();
    }

    public void Disconnect()
    {
        disconnectEvent.Invoke();
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        LoadingScreenManager.Instance.CallLoadingScreen();
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        LoadingScreenManager.Instance.DismissLoadingScreen();
        asyncLoad.allowSceneActivation = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnDroppedItemServerRpc(ulong itemId,int amount,Vector3 position)
    {
        Item item = ItemManager.GenerateItem(itemId);
        GameObject droppedItem = Instantiate(item.droppedObject,position,Quaternion.identity);
        droppedItem.GetComponent<NetworkObject>().Spawn(true);
        Interactable_DroppedItem interactable_DroppedItem = droppedItem.GetComponentInChildren<Interactable_DroppedItem>() ?? null;
        if(interactable_DroppedItem != null)
        {
            interactable_DroppedItem.itemDropId.Value = itemId;
            interactable_DroppedItem.stackSize = amount;
            interactable_DroppedItem.SpawnParametersClientRpc((Random.onUnitSphere + new Vector3(0,0.75f,0)) .normalized, 3,item.itemId);
        }

    }
    [ServerRpc(RequireOwnership = false)]
    public void SpawnDroppedItemServerRpc(ulong itemId, int amount, Vector3 position, Vector3 directon, float spawnVelocity)
    {
        Item item = ItemManager.GenerateItem(itemId);
        GameObject droppedItem = Instantiate(item.droppedObject, position, Quaternion.identity);
        droppedItem.GetComponent<NetworkObject>().Spawn(true);
        Interactable_DroppedItem interactable_DroppedItem = droppedItem.GetComponentInChildren<Interactable_DroppedItem>() ?? null;
        if (interactable_DroppedItem != null)
        {
            interactable_DroppedItem.itemDropId.Value = itemId;
            interactable_DroppedItem.stackSize = amount;
            interactable_DroppedItem.SpawnParametersClientRpc(directon, spawnVelocity, item.itemId);
        }

    }




    [ServerRpc (RequireOwnership = false)]
    public void PlaceBuildingServerRpc(ulong buildingId, Vector3 position, Quaternion rotation, bool flip)
    {
        Vector3 scale = new Vector3(1, 1, 1 * Random.Range(0.99990000f, 1f));
        if (flip)
        {
            scale = new Vector3(-1, 1, 1 * Random.Range(0.99990000f,1f));
        }
        BuildingObject buildingObject = BuildingObjectManager.GenerateBuildingObject(buildingId);
        GameObject prefab = buildingObject.buildingPrefab;
        GameObject spawnedPrefab = Instantiate(prefab, position, rotation);
        spawnedPrefab.transform.localScale = scale;
        spawnedPrefab.GetComponent<NetworkObject>().Spawn(true);

        PlacedObjectData data = new PlacedObjectData()
        {
            buildingId = buildingObject.buildingId,
            position = new SerializedVector3(position.x,position.y,position.z),
            rotation = new SerializedVector3(rotation.eulerAngles.x, rotation.eulerAngles.y, rotation.eulerAngles.z),
            scale = new SerializedVector3(scale.x, scale.y, scale.z),
        };
        if(spawnedPrefab.transform.root.GetComponent<BuildingObjectHandler>() != null)
        {
            spawnedPrefab.transform.root.GetComponent<BuildingObjectHandler>().data = data;
        }

        worldSaveState.AddPlacedObject(data);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnBuildingServerRpc(ulong networkId,ulong clientThatDespawned)
    {
        NetworkObject building = NetworkManager.SpawnManager.SpawnedObjects[networkId];
        BuildingObjectHandler boh = building.GetComponent<BuildingObjectHandler>() ?? null;
        BuildingObject buildingObject = BuildingObjectManager.GenerateBuildingObject(boh.data.buildingId);
        if(boh != null)
        { 
            for(int i = 0; i < buildingObject.buildingMaterials.Length;i++)
            {
                SpawnDroppedItemServerRpc(buildingObject.buildingMaterials[i].itemId, buildingObject.buildingMaterialAmounts[0], building.transform.position + new Vector3(0,1,0));
            }

        }
        building.Despawn(true);
        
    }

    private void RegisterNetworkPrefabs()
    {
        var prefabs = networkPrefabsList.PrefabList.Select(x => x.Prefab);
        foreach (var prefab in prefabs)
        {
            NetworkManager.Singleton.AddNetworkPrefab(prefab);
        }
    }

    public static ClientRpcParams GetClientRpcParams(ulong id)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { id }
            }
        };
        return clientRpcParams;
    }

    public static ClientRpcParams GetClientRpcParams(ulong[] ids)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = ids.ToList().AsReadOnly()
            }
        };
        return clientRpcParams;
    }

    public virtual void DiscoverItem(Item item)
    {
        Instance.playerSaveData.DiscoverItem(item);
    }

    public static GameManager Instance { get => instance; set => instance = value; }

}
