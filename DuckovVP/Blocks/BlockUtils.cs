using System;
using FeatherMod;
using FeatherMod.Items;
using FeatherMod.Utils;
using UnityEngine;

namespace DuckovVP.Blocks;

public class BlockUtils
{
    public static Identifier GetNative(int id)
    {
        if (GameItemLookup.TryGetIdentifier(id, out var r)) return r;
        throw new Exception($"Get Item ${id} failed");
    }
    
    public static void Init(AssetBundle bundle)
    {
        BuildingUtils.RegisterBuilding(new()
        {
            Id = new(ModBehaviour.MODID, "burner"),
            Money = 4500,
            CostItems = new[]{ItemEntry.Of(GetNative(338), 1), ItemEntry.Of(GetNative(340), 3), ItemEntry.Of(GetNative(298), 1)},
            UnlockedByDefault = true,
            Icon = ItemUtils.LoadSpriteFromDir(ModPathResolver.ResolveDirectory(ModBehaviour.MODID)!, "burner.png")
        });

        var modelWrapper = new GameObject();
        modelWrapper.transform.localScale = Vector3.one;
        modelWrapper.transform.localPosition = Vector3.zero;
        var model = UnityEngine.Object.Instantiate(bundle.LoadAsset<GameObject>("dvd_burner"), modelWrapper.transform, false);
        model.transform.localScale = new(1.5f, 1.5f, 1.5f);
        model.transform.localPosition = new(0.25f, 0f, 0.25f);
        model.transform.SetParent(modelWrapper.transform);
        
        BuildingUtils.SetBuildingModel(
            new(ModBehaviour.MODID, "burner"),
            modelWrapper
        );
        
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
            child.transform.localPosition = new(1, 0, 0);
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