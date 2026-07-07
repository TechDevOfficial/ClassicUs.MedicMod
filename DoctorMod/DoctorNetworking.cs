using System;
using ClassicUs.Manactor;
using UnityEngine;

namespace ClassicUs.MedicMod
{
    internal static class MedicNetworking
    {
        public const string RequestReviveKey = "classicus.medicmod.RequestRevive";
        public const string BroadcastReviveKey = "classicus.medicmod.BroadcastRevive";

        public static void RequestRevive(byte medicId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client != null && client.AmHost)
            {
                ResolveRevive(medicId, targetId);
            }
            else
            {
                ManactorAPI.SendRpcMethod(RequestReviveKey, medicId, targetId);
            }
        }

        [ManactorRpc(RequestReviveKey)]
        private static void OnRequestRevive(byte senderId, byte medicId, byte targetId)
        {
            var client = AmongUsClient.Instance;
            if (client == null || !client.AmHost) return;
            ResolveRevive(medicId, targetId);
        }

        private static void ResolveRevive(byte medicId, byte targetId)
        {
            PlayerControl target = null;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.Data.PlayerId != targetId) continue;
                target = p;
                break;
            }

            if (target == null || target.Data == null || !target.Data.IsDead) return;

            ManactorAPI.SendRpcMethod(BroadcastReviveKey, targetId);
            ApplyRevive(targetId);
        }

        [ManactorRpc(BroadcastReviveKey)]
        private static void OnBroadcastRevive(byte senderId, byte targetId) => ApplyRevive(targetId);

        private static void ApplyRevive(byte targetId)
        {
            try
            {
                PlayerControl target = null;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p == null || p.Data == null || p.Data.PlayerId != targetId) continue;
                    target = p;
                    break;
                }

                if (target == null || target.Data == null) return;

                target.Data.IsDead = false;
                target.Revive();

                var client = AmongUsClient.Instance;
                if (client != null && client.AmHost && RoleManager.InstanceExists)
                    RoleManager.Instance.AssignRole(target, "CrewmateRole");

                foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
                {
                    if (body == null || body.ParentId != targetId) continue;
                    UnityEngine.Object.Destroy(body.gameObject);
                }
            }
            catch (Exception e)
            {
                MedicAPIPlugin.Log.LogError("ApplyRevive: " + e);
            }
        }
    }
}
