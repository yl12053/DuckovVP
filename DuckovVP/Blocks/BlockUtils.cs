using System;
using FeatherMod;
using UnityEngine;

namespace DuckovVP.Blocks;

public class BlockUtils
{
    public static void Init()
    {
        BuildingUtils.RegisterBuilding(new()
        {
            Id = new(ModBehaviour.MODID, "burner"),
            Money = 4500,
            CostItems = new[]{ItemEntry.Of("duckov:Wood", 1)},
            UnlockedByDefault = true
        });

        var funcContainer =
            BuildingUtils.GetFunctionContainer(BuildingUtils.GetBuildingPrefab(new(ModBehaviour.MODID, "burner")));
        if (funcContainer == null) return;
        var name = "BurnerInteract";
        var p = funcContainer.transform.Find(name);
        if (p != null)
        {
            var h = p.GetComponent<BurnerInteract>();
            if (h != null) return;
        }

        try
        {
            var child = new GameObject(name);
            child.transform.SetParent(funcContainer.transform, false);
            child.transform.localPosition = new(-1, 0, 0);
            child.layer = funcContainer.layer;

            var collider = child.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one;

            child.AddComponent<BurnerInteract>();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}