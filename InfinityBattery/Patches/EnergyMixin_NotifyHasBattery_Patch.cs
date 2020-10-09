﻿using HarmonyLib;
using InfinityBattery.MonoBehaviours;
using System.Linq;

namespace InfinityBattery.Patches
{
    [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.NotifyHasBattery))]
    public static class EnergyMixin_NotifyHasBattery_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(EnergyMixin __instance, InventoryItem item)
        {
            if(item != null)
            {
                TechType techType = item.item.GetTechType();

                if (techType == Main.InfinityPack.ItemPrefab.TechType)
                {
                    EnergyMixin.BatteryModels? i = __instance.batteryModels.Where((x) => x.techType == techType)?.First();

                    if (i.HasValue)
                    {
                        i.Value.model.EnsureComponent<InfinityBehaviour>();
                    }
                }
            }
        }
    }
}