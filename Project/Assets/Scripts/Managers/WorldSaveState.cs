﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using Unity.Netcode;
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

    public string DirectoryTempSaveDataPath()
    {
        return Path.Combine(DirectoryPath(), "TempSaveData");
    }

    public void SaveWorld()
    {
        ResourceController[] resources = FindObjectsOfType<ResourceController>();
        ResourceObjectData[] resourcesSaveData = ResourceObjectData.ConvertResources(resources);
        // Get the Current Event Time from the Reference
        foreach(PlacedObjectData data in placedObjects)
        {
            data.eventCountdown = data.reference.coutdownEventCurrentTime;
        }
        SaveData saveData = new SaveData()
        {
            placedObjects = placedObjects,
            resources = resourcesSaveData.ToList(),
            dayTimeSeconds = (long)GameManager.Instance.currentTime.TotalSeconds
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

            // Place all the Objects
            foreach(PlacedObjectData placedObject in placedObjects)
            {
                GameManager.Instance.PlaceBuildingAfterLoadingWorld(placedObject);
            }
            // Replace resources Prior in Scene with Resources stored in the Save
            ResourceController[] scenePlacedResources = GameObject.FindObjectsOfType<ResourceController>();
            foreach(ResourceController rc in scenePlacedResources)
            {
                rc.DestroyWithoutTrace();
            }
            foreach(ResourceObjectData savedResources in resourceObjects)
            {
                GameManager.Instance.SpawnResource(savedResources);
            }

            // Set Day Time
            DayNightController.SetTime(TimeSpan.FromSeconds(saveData.dayTimeSeconds));

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
public class PlacedObjectData
{
    [JsonIgnore] public BuildingObjectHandler reference;
    public ulong buildingId;
    public SerializedVector3 position;
    public SerializedVector3 rotation;
    public SerializedVector3 scale;
    public int state;
    public int secondaryState;
    public int eventCountdown;

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

    public static implicit operator SerializedVector3(Vector3 obj)
    {
        return new SerializedVector3(obj.x, obj.y, obj.z);
    }

    public static implicit operator Vector3(SerializedVector3 obj)
    {
        return new Vector3(obj.x, obj.y, obj.z);
    }

}

[Serializable]
public struct ResourceObjectData
{
    public int resource_id;
    public SerializedVector3 positon;
    public SerializedVector3 roation;
    public SerializedVector3 scale;
    public int hp;
    public int maxhp;

    public static implicit operator ResourceObjectData(ResourceController obj)
    {
        return new ResourceObjectData()
        {
            resource_id = obj.resourceId,
            positon = obj.root.position,
            roation = obj.root.eulerAngles,
            scale = obj.root.localScale,
            hp = obj.hp.Value,
            maxhp = obj.maxHp
        };
    }

    public static ResourceObjectData[] ConvertResources(ResourceController[] array)
    {
        List<ResourceController> validatedArray = new List<ResourceController>();
        foreach(ResourceController rc in array)
        {
            if(rc.root != null)
            {
                validatedArray.Add(rc);
            }
        }
        ResourceObjectData[] result = new ResourceObjectData[validatedArray.Count];
        for(int i = 0; i < validatedArray.Count;i++)
        {
            result[i] = validatedArray[i];
        }
        return result;
    }

}

[Serializable]
public struct SaveData
{
    public List<PlacedObjectData> placedObjects;
    public List<ResourceObjectData> resources;
    public long dayTimeSeconds;
}