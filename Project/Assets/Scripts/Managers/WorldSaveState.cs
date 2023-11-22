﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public class WorldSaveState : MonoBehaviour
{
    [SerializeField] private List<PlacedObjectData> placedObjects = new List<PlacedObjectData>();
    public string worldName = "Test";

    public List<string> FindSavedWorlds()
    {
        if (Directory.Exists(DirectoryPath()))
        {
            List<string> worldNames = new List<string>();
            string[] files = Directory.GetFiles(DirectoryPath());
            foreach (var file in files)
            {
                worldNames.Add(Path.GetFileNameWithoutExtension(file));
            }
            return worldNames;
        }
        else
        {
            Debug.Log("No Files found");
            return null;
        }
    }
    public string DirectoryPath()
    {
        return Path.Combine(Application.persistentDataPath, "CozySaveData");
    }

    public void SaveWorld()
    {
        SaveData saveData = new SaveData()
        {
            placedObjects = placedObjects
        };
        Directory.CreateDirectory(DirectoryPath());
        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
        string filePath = Path.Combine(DirectoryPath(), worldName + ".json");
        File.WriteAllText(filePath, json);
        print($"PlacedObjectData list saved to {filePath}");
    }

    public void LoadWorld()
    {
        string filePath = Path.Combine(DirectoryPath(), worldName + ".json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);

            List<PlacedObjectData> placedObjects = saveData.placedObjects;
            List<ResourceObjectData> resourceObjects = saveData.resources;

            foreach(PlacedObjectData placedObject in placedObjects)
            {
                GameManager.Instance.PlaceBuildingServerRpc(placedObject.buildingId, new Vector3(placedObject.position.x, placedObject.position.y, placedObject.position.z), 
                    Quaternion.Euler(placedObject.rotation.x, placedObject.rotation.y, placedObject.rotation.z), false);
            }

        }
        else
        {
            Debug.LogWarning($"File not found: {filePath}");
        }
    }

    public void AddPlacedObject(PlacedObjectData data)
    {
        placedObjects.Add(data);
    }

    public void RemovePlacedObject(PlacedObjectData data)
    {
        placedObjects.Remove(data);
    }
}

[Serializable]
public struct PlacedObjectData
{
    public ulong buildingId;
    public SerializedVector3 position;
    public SerializedVector3 rotation;
    public SerializedVector3 scale;
    public int state;
    public int secondaryState;

    public ulong[] itemContainer1;
    public int[] itemContainer1Amounts;

    public ulong[] itemContainer2;
    public int[] itemContainer2Amounts;
}

[Serializable]
public struct SerializedVector3
{
    public float x;
    public float y;
    public float z;

    public SerializedVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

}

[Serializable]
public struct ResourceObjectData
{
    public ResourceController resource;
}

[Serializable]
public struct SaveData
{
    public List<PlacedObjectData> placedObjects;
    public List<ResourceObjectData> resources;
}