using System;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ClassicUs.MedicMod
{
    internal static class MedicIntroRegistration
    {
        private static bool _registered;

        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;

            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<MedicIntroOverlay>();
            }
            catch (Exception e)
            {
                MedicAPIPlugin.Log.LogError("MedicIntroOverlay registration failed: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Start))]
    internal static class SplashManager_Start_Patch
    {
        private static bool _introShown;
        private static bool _allowOriginal;

        private static bool Prefix(SplashManager __instance)
        {
            if (_allowOriginal)
            {
                _allowOriginal = false;
                return true;
            }

            if (_introShown) return true;
            _introShown = true;

            try
            {
                MedicIntroRegistration.EnsureRegistered();

                var go = new GameObject("MedicIntroOverlay");
                var overlay = go.AddComponent<MedicIntroOverlay>();
                overlay.OnFinished = () =>
                {
                    _allowOriginal = true;
                    __instance.Start();
                };
                overlay.Begin();
            }
            catch (Exception e)
            {
                MedicAPIPlugin.Log.LogError("MedicIntro splash overlay: " + e);
                return true;
            }

            return false;
        }
    }
}
