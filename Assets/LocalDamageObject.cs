using MalbersAnimations.Weapons;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LocalDamageObject : MonoBehaviour
{
    public NetworkObject source;
    public MWeapon weapon;
    public virtual void ApplyDamage(Transform other)
    {
        if (other.GetComponent<ResourceController>() != null)
        {
            int damage = (int)weapon.MaxDamage;
            other.GetComponent<ResourceController>().hp.Value -= damage;
            other.GetComponent<ResourceController>().PlayFeedbackServerRpc(damage);
        }
    }
}
