using System.Collections.Generic;
using Duckov.ItemBuilders;
using Duckov.Utilities;
using FeatherMod;
using FeatherMod.Items;
using FeatherMod.Register;
using FeatherMod.Utils;
using ItemStatsSystem;
using UnityEngine;

namespace DuckovVP.Items;

public class ItemUtils
{
    public static void Init()
    {
        FeatherMod.ItemUtils.RegisterTag(new Identifier(ModBehaviour.MODID, "CD"),
            new TagBuilder()
        );
        
        ItemData dv = new ItemData();
        dv.itemId = 214750;
        dv.localizationKey = "DuckovDV.item.name.dv";
        dv.localizationDesc = "DuckovDV.item.name.dv_Desc";
        dv.weight = 2f;
        dv.value = 2000;
        dv.quality = 5;
        dv.displayQuality = DisplayQuality.Orange;
        dv.spritePath = "dv.png";
        dv.tags = new List<string>();
        dv.tags.Add("GamingConsole");
        dv.consts[ModBehaviour.MODID + "CustomGameCon"] = (true, false);
        dv.slots.Add(new()
        {
            key = "cd",
            requireTags = { new Identifier(ModBehaviour.MODID, "CD").ToString() }
        });
        FeatherMod.ItemUtils.CreateCustomItem(new Identifier(ModBehaviour.MODID, "DuckovVP"), dv);

        ItemData dvd = new ItemData();
        dvd.itemId = 214751;
        dvd.localizationKey = "DuckovDV.item.name.dvd";
        dvd.localizationDesc = "DuckovDV.item.name.dvd_Desc";
        dvd.weight = 0.3f;
        dvd.value = 500;
        dvd.quality = 4;
        dvd.displayQuality = DisplayQuality.Purple;
        dvd.spritePath = "dvd.png";
        dvd.tags = new List<string>();
        dvd.AddTags(new Identifier(ModBehaviour.MODID, "CD"));
        dvd.variables["Path"] = ("", true);
        FeatherMod.ItemUtils.CreateCustomItem(new Identifier(ModBehaviour.MODID, "DVD"), dvd);

        /* foreach (var tagsAllTag in GameplayDataSettings.Tags.allTags)
        {
            Debug.Log(tagsAllTag);
        } */
    }
}