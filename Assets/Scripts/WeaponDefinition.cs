using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "New Weapon", menuName = "Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    public AssetReference prefab;
    public int goldPrice;
    public int gemsPrice;
    public int minLevel;
}
