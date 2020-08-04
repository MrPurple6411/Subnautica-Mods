﻿using HarmonyLib;

namespace UnKnownName
{
    [HarmonyPatch(typeof(ScannerTool), nameof(ScannerTool.OnHover))]
    public class ScannerTool_OnHover
    {
        [HarmonyPostfix]
        public static void Postfix(ScannerTool __instance)
        {
            PDAScanner.ScanTarget scanTarget = PDAScanner.scanTarget;
#if SUBNAUTICA
            PDAScanner.Result result = PDAScanner.CanScan();
#elif BELOWZERO
            PDAScanner.Result result = PDAScanner.CanScan();
#endif
            PDAScanner.EntryData entryData = PDAScanner.GetEntryData(PDAScanner.scanTarget.techType);

            if ((entryData != null && (KnownTech.Contains(entryData.blueprint) || KnownTech.Contains(entryData.key))) || PDAScanner.ContainsCompleteEntry(scanTarget.techType) || __instance.energyMixin.charge <= 0f || !scanTarget.isValid || result != PDAScanner.Result.Scan || !GameModeUtils.RequiresBlueprints())
            {
                return;
            }
#if SUBNAUTICA
            HandReticle.main.SetInteractText(Main.config.UnKnownLabel, false, HandReticle.Hand.None);
#elif BELOWZERO
            HandReticle.main.SetText(HandReticle.TextType.Hand, Main.config.UnKnownLabel, true, GameInput.Button.None);
#endif
        }

    }

}