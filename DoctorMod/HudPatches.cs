using System;
using HarmonyLib;

namespace ClassicUs.MedicMod
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.FixedUpdate))]
    internal static class HudManager_FixedUpdate_Patch
    {
        private static void Prefix(HudManager __instance)
        {
            try { ReviveAbilityHolder.Tick(__instance); }
            catch (Exception e) { MedicAPIPlugin.Log.LogError("ReviveAbilityHolder.Tick: " + e); }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    internal static class HudManager_Start_Patch
    {
        private static void Prefix() => ReviveAbilityHolder.Reset();
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.AssignRolesForTeam))]
    internal static class RoleManager_AssignRolesForTeam_Patch
    {
        private static void Prefix()
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;

            try { MedicAPIPlugin.HostBroadcastSettings(); }
            catch (Exception e) { MedicAPIPlugin.Log.LogError("HostBroadcastSettings (AssignRolesForTeam): " + e); }
        }
    }

    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.Start))]
    internal static class RoleManager_Start_MedicAssets_Patch
    {
        private static void Postfix(RoleManager __instance)
        {
            try { MedicAssets.RegisterRoleSprites(__instance); }
            catch (Exception e) { MedicAPIPlugin.Log.LogError("Register Medic role sprites: " + e); }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    internal static class AmongUsClient_OnPlayerJoined_Patch
    {
        private static void Postfix(AmongUsClient __instance)
        {
            if (__instance == null || !__instance.AmHost) return;

            try { MedicAPIPlugin.HostBroadcastSettings(); }
            catch (Exception e) { MedicAPIPlugin.Log.LogError("HostBroadcastSettings (OnPlayerJoined): " + e); }
        }
    }

    [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
    internal static class PingTracker_Update_Patch
    {
        private static void Postfix(PingTracker __instance)
        {
            try
            {
                if (__instance != null && __instance.text != null)
                {
                    var t = __instance.text;
                    if (!t.Text.EndsWith("\nmod by Manu"))
                        t.Text += "\nmod by Manu";
                }
            }
            catch (Exception e)
            {
                MedicAPIPlugin.Log.LogError("PingTracker patch: " + e);
            }
        }
    }
}
