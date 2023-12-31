using MalbersAnimations.Weapons;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LocalDamageObject : MonoBehaviour
{
    public MWeapon weapon;
    public GameObject sourceObject;
    private Collider col;
    private void Start()
    {
        col = GetComponent<Collider>();
    }
    public virtual void ApplyDamage(Collider other)
    {

        ApplyDamage(other.transform);
    }
    
    public virtual void ApplyDamage(Transform other)
    {
        print(NetworkManager.Singleton.LocalClientId + "   " + sourceObject.GetComponent<NetworkObject>().OwnerClientId);
        if(NetworkManager.Singleton.LocalClientId == sourceObject.GetComponent<NetworkObject>().OwnerClientId)
        {

        }
        if (other.GetComponent<ResourceController>() != null)
        {
            var source = sourceObject.GetComponent<NetworkObject>();
            int damage = (int)Random.Range(weapon.MinDamage,weapon.MaxDamage+1);
            int elementId = weapon.element?.ID ?? 0;
            ResourceController rc = other.GetComponent<ResourceController>();
            Vector3 objectSpawnPoint = col.ClosestPoint(other.transform.position);
            objectSpawnPoint.y = transform.position.y;
            rc.PlayFeedbackServerRpc(damage,elementId,source.OwnerClientId, objectSpawnPoint);
            // Redundant, it also beeing calculated by the server, consider doing this only on the client
            if (rc.needWeaknessForEffectiveDamage && rc.weakness != null && rc.weakness.ID == elementId)
            {
                // Weakness is hit
            }
            else if (rc.needWeaknessForEffectiveDamage)
            {
                // Weakness is not hit
                damage = 0;
            }
            other.GetComponent<ResourceController>().PlayFeedback(damage);
        }
    }


    public virtual void SetWeaponHitbox(GameObject go)
    {
        print(go.name);
        if(go.GetComponent<MMelee>() != null)
        {
            weapon = go.GetComponent<MMelee>();
        }
    }
}
