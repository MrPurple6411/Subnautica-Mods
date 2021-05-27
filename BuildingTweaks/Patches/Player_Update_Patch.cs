﻿using System.Linq;

namespace BuildingTweaks.Patches
{
    using HarmonyLib;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
#if BZ
    using SMLHelper.V2.Handlers;
    using System.Collections.Generic;
    using System.Linq;
#endif

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class Player_Update_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if(Input.GetKeyDown(Main.Config.AttachToTargetToggle))
            {
                ProcessMSG($"Attach as target override = {Main.Config.AttachToTarget}", false);
                Main.Config.AttachToTarget = !Main.Config.AttachToTarget;
                ProcessMSG($"Attach as target override = {Main.Config.AttachToTarget}", true);
            }

            if(Input.GetKeyDown(Main.Config.FullOverrideToggle))
            {
                ProcessMSG($"Full Override = {Main.Config.FullOverride}", false);
                Main.Config.FullOverride = !Main.Config.FullOverride;
                if (Builder.prefab != null && !Builder.canPlace)
                {
                    var value = Main.Config.FullOverride? Builder.placeColorAllow: Builder.placeColorDeny;
                    var components = Builder.ghostModel.GetComponents<IBuilderGhostModel>();
                    foreach (var builderGhostModel in components)
                        builderGhostModel.UpdateGhostModelColor(true, ref value);
                    Builder.ghostStructureMaterial.SetColor(ShaderPropertyID._Tint, value);
                }
                ProcessMSG($"Full Override = {Main.Config.FullOverride}", true);
            }

            if (Builder.prefab == null)
                ClearMsgs();
            else if (Input.GetMouseButtonDown(2))
                Builder_Update_Patches.Freeze = !Builder_Update_Patches.Freeze;

            var waterPark = __instance.currentWaterPark;

            if(waterPark != null && waterPark.GetComponentInParent<Creature>() != null)
            {
                var vector3 = __instance.currentWaterPark.transform.position;
                __instance.SetPosition(vector3);

                var msg3 = $"Press {GameInput.GetBinding(GameInput.GetPrimaryDevice(), GameInput.Button.Exit, GameInput.BindingSet.Primary)} to exit waterpark if you cant reach the exit.";

                if (!GameInput.GetButtonDown(GameInput.Button.Exit))
                {
                    ProcessMSG(msg3, true);
                    return;
                }
                
                Collider[] hitColliders = { };
                Physics.OverlapSphereNonAlloc(__instance.transform.position, 3f, hitColliders, 1,
                    QueryTriggerInteraction.UseGlobal);
                var diveHatch = hitColliders
                    .Select(hitCollider => hitCollider.gameObject.GetComponentInParent<UseableDiveHatch>())
                    .FirstOrDefault(hatch => hatch != null && hatch.isForWaterPark);

                if (diveHatch != null)
                {
                    diveHatch.StartCinematicMode(diveHatch.enterCinematicController, __instance);
                    if (diveHatch.enterCustomGoalText != "" && (!diveHatch.customGoalWithLootOnly ||
                                                                Inventory.main.GetTotalItemCount() > 0))
                    {
                        Debug.Log("OnCustomGoalEvent(" + diveHatch.enterCustomText);
                        GoalManager.main.OnCustomGoalEvent(diveHatch.enterCustomGoalText);
                    }

                    if (diveHatch.secureInventory)
                    {
                        Inventory.Get().SecureItems(true);
                    }
                }

                ProcessMSG(msg3, false);

            }

#if SN1
            var currentSubRoot = __instance.GetCurrentSub();
            if(currentSubRoot != null && currentSubRoot is BaseRoot && __instance.playerController.velocity.y < -20f)
            {
                var componentInChildren = currentSubRoot.gameObject.GetComponentInChildren<RespawnPoint>();
                if(componentInChildren)
                {
                    __instance.SetPosition(componentInChildren.GetSpawnPosition());
                    return;
                }
            }

            var escapePod = __instance.currentEscapePod;
            if (escapePod == null || !(__instance.playerController.velocity.y < -20f)) return;
            var transform = escapePod.playerSpawn.transform;
            __instance.SetPosition(transform.position, transform.rotation);
#elif BZ
            var interiorSpace = __instance.currentInterior;
            if (interiorSpace != null && __instance.playerController.velocity.y < -20f)
            {
                var respawnPoint = interiorSpace.GetRespawnPoint();
                if (respawnPoint)
                {
                    __instance.SetPosition(respawnPoint.GetSpawnPosition());
                    return;
                }
            }
#endif
        }

        private static void ClearMsgs()
        {
            ProcessMSG($"Attach as target override = {Main.Config.AttachToTarget}", false);
            ProcessMSG($"Full Override = {Main.Config.FullOverride}", false);
            Main.Config.AttachToTarget = false;
            Main.Config.FullOverride = false;
            ProcessMSG($"Attach as target override = {Main.Config.AttachToTarget}", false);
            ProcessMSG($"Full Override = {Main.Config.FullOverride}", false);
        }

        private static void ProcessMSG(string msg, bool active)
        {
            var message = ErrorMessage.main.GetExistingMessage(msg);
            if(active)
            {
                if(message != null)
                {
                    message.messageText = msg;
                    message.entry.text = msg;
                    if(message.timeEnd <= Time.time + 1f)
                        message.timeEnd += Time.deltaTime;
                }
                else
                {
                    ErrorMessage.AddMessage(msg);
                }
            }
            else if(message != null && message.timeEnd > Time.time)
            {
                message.timeEnd = Time.time;
            }
        }
    }
}
