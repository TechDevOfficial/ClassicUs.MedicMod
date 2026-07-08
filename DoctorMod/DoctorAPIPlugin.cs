using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ClassicUs.Manactor;
using ClassicUs.ManuAPI;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ClassicUs.MedicMod
{
    [BepInPlugin(Guid, "Classic Us Medic Mod", Version)]
    [BepInDependency(ManactorPlugin.Guid)]
    [BepInDependency(ManuAPIPlugin.Guid)]
    public class MedicAPIPlugin : BasePlugin
    {
        public const string Guid = "classicus.medicmod";
        public const string Version = "1.0.1";
        public const string ModName = "ClassicUsMedicMod";

        public const string RpcSyncSettingsKey = "classicus.medicmod.SyncSettings";

        public static ManualLogSource Log;

        private static ConfigEntry<bool> _cfgEnabled;
        private static ConfigEntry<int> _cfgCount;
        private static ConfigEntry<float> _cfgRoleChance;
        private static ConfigEntry<float> _cfgCooldown;
        private static ConfigEntry<float> _cfgRange;

        public static bool ActiveEnabled = true;
        public static int ActiveCount = 1;
        public static float ActiveRoleChance = 100f;
        public static float ActiveCooldown = 20f;
        public static float ActiveRange = 1.5f;

        public static bool IsTypeReady;
        private static bool _classInjectorAttempted;

        public override void Load()
        {
            Log = base.Log;

            _cfgEnabled = Config.Bind("Game", "EnableMedic", true,
                "Enables the Medic role: a crewmate that can revive dead bodies.");
            _cfgCount = Config.Bind("Game", "MedicCount", 1,
                new ConfigDescription("How many Medics to assign per match.", new AcceptableValueRange<int>(0, 3)));
            _cfgRoleChance = Config.Bind("Game", "MedicRoleChance", 100f,
                new ConfigDescription("Chance that a selected candidate becomes Medic.", new AcceptableValueRange<float>(0f, 100f)));
            _cfgCooldown = Config.Bind("Game", "MedicReviveCooldown", 20f,
                new ConfigDescription("Cooldown of the revive button (seconds).", new AcceptableValueRange<float>(5f, 60f)));
            _cfgRange = Config.Bind("Game", "MedicReviveRange", 1.5f,
                new ConfigDescription("How close the Medic must be to a dead body to revive it.", new AcceptableValueRange<float>(0.5f, 3f)));

            ManactorAPI.Register(ModName, Version);
            ManactorAPI.RegisterRpcMethods(this);
            ManactorAPI.RegisterRpcMethods(typeof(MedicNetworking));

            RoleRegistry.Register(
                new MedicRoleDescriptor(),
                () => IsTypeReady,
                EnsureIl2CppTypeRegistered,
                () => Il2CppType.Of<MedicModRole>());

            SettingsMenuAPI.Register(5, builder =>
            {
                builder.AddToggle("MedicAPIToggle", "Enable Medic",
                    () => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? _cfgEnabled.Value : ActiveEnabled,
                    val => { _cfgEnabled.Value = val; Save(); });

                builder.AddNumeric("MedicAPICount", "Medic Count", 1f, 0f, 3f, "0",
                    () => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? _cfgCount.Value : ActiveCount,
                    val => { _cfgCount.Value = (int)val; Save(); });

                builder.AddNumeric("MedicAPIRoleChance", "Medic Role Chance", 5f, 0f, 100f, "0\\%",
                    () => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? _cfgRoleChance.Value : ActiveRoleChance,
                    val => { _cfgRoleChance.Value = val; Save(); });

                builder.AddNumeric("MedicAPICooldown", "Revive Cooldown", 5f, 5f, 60f, "0s",
                    () => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? _cfgCooldown.Value : ActiveCooldown,
                    val => { _cfgCooldown.Value = val; Save(); });

                builder.AddNumeric("MedicAPIRange", "Revive Range", 0.1f, 0.5f, 3f, "0.0",
                    () => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost ? _cfgRange.Value : ActiveRange,
                    val => { _cfgRange.Value = val; Save(); });

                builder.ExpandScroller(5f);
            });

            ModBadgeAPI.RegisterLoadedModBadge("MedicMod", Version, new Color(0.3f, 0.9f, 0.9f, 1f));
            ModBadgeAPI.RegisterPrelobbyTag("Medic Mod", "#4DE6E6");

            new Harmony(Guid).PatchAll();

            Log.LogInfo("Classic Us Medic Mod loaded.");
        }

        private static void Save()
        {
            _cfgEnabled.ConfigFile.Save();
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                HostBroadcastSettings();
        }

        public static void EnsureIl2CppTypeRegistered()
        {
            if (_classInjectorAttempted) return;
            _classInjectorAttempted = true;

            ManactorAPI.RegisterIl2CppType(() =>
            {
                try
                {
                    ClassInjector.RegisterTypeInIl2Cpp<MedicModRole>();
                    IsTypeReady = true;
                    Log.LogInfo("MedicModRole type registered in IL2CPP.");
                }
                catch (System.Exception e)
                {
                    Log.LogError("MedicModRole registration failed: " + e);
                }
            });
        }

        public static void HostBroadcastSettings()
        {
            ActiveEnabled = _cfgEnabled.Value;
            ActiveCount = _cfgCount.Value;
            ActiveRoleChance = _cfgRoleChance.Value;
            ActiveCooldown = _cfgCooldown.Value;
            ActiveRange = _cfgRange.Value;

            ManactorAPI.SendRpcMethod(RpcSyncSettingsKey, ActiveEnabled, (byte)ActiveCount, ActiveRoleChance,
                ActiveCooldown, ActiveRange);

            Log.LogInfo($"MedicMod settings sent: enabled={ActiveEnabled} count={ActiveCount} chance={ActiveRoleChance} " +
                        $"cooldown={ActiveCooldown} range={ActiveRange}");
        }

        [ManactorRpc(RpcSyncSettingsKey)]
        private static void OnSyncSettingsRpc(byte senderId, bool enabled, byte count, float roleChance,
            float cooldown, float range)
        {
            ActiveEnabled = enabled;
            ActiveCount = count;
            ActiveRoleChance = roleChance;
            ActiveCooldown = cooldown;
            ActiveRange = range;
            Log.LogInfo($"MedicMod settings received: enabled={ActiveEnabled} count={ActiveCount} chance={ActiveRoleChance} " +
                        $"cooldown={ActiveCooldown} range={ActiveRange}");
        }

        public static bool IsMedic(PlayerControl p)
        {
            if (p == null || p.Data == null || p.Data.myRole == null) return false;
            try { return p.Data.myRole.GetIl2CppType().Name == "MedicModRole"; }
            catch { return false; }
        }
    }
}
