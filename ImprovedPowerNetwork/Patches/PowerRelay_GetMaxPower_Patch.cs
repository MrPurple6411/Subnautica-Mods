﻿namespace ImprovedPowerNetwork.Patches
{
    using HarmonyLib;

    [HarmonyPatch(typeof(PowerRelay), nameof(PowerRelay.GetMaxPower))]
    public static class PowerRelay_GetMaxPower_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(PowerRelay __instance, ref float __result)
        {
            var powerInterface = __instance.inboundPowerSources.Find((x) => x is BaseInboundRelay || x is OtherConnectionRelay);

            if(powerInterface != null)
            {
                PowerControl powerControl = null;
                switch(powerInterface)
                {
                    case BaseInboundRelay baseConnectionRelay:
                        powerControl = baseConnectionRelay.gameObject.GetComponent<PowerControl>();
                        break;
                    case OtherConnectionRelay otherConnectionRelay:
                        powerControl = otherConnectionRelay.gameObject.GetComponent<PowerControl>();
                        break;
                }

                var endRelay = powerControl.powerRelay.GetEndpoint();

                var endPower = endRelay.GetMaxPower();
                var powerHere = powerInterface.GetMaxPower();

                if(endPower > powerHere)
                {
                    __result += endPower - powerHere;
                }
            }
        }
    }
}
