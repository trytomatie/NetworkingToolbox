﻿using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "BuildingObject/Building Object")]
public class BuildingObject : ScriptableObject
{
    public enum BuildingType { Other, Floor, Wall, Stair, Plliar}
    // Ids are getting Distributed from the BuildingManager on Start
    [HideInInspector]public ulong buildingId;
    public BuildingType buildingType;
    public string buildingName;
    public GameObject buildingPrefab;
    public Sprite sprite;
    public Item[] buildingMaterials;
    public int[] buildingMaterialAmounts;
}