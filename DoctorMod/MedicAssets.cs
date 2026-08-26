using System.Reflection;
using ClassicUs.ManuAPI;
using UnityEngine;

namespace ClassicUs.MedicMod
{
    internal static class MedicAssets
    {
        private static readonly Assembly _assembly = typeof(MedicAssets).Assembly;

        private static readonly LoadableSprite _reviveIcon =
            new(_assembly, "revive_button.png", 100f);

        private static readonly LoadableSprite _introSprite =
            new(_assembly, "Intro.png", 100f);

        public static Sprite LoadReviveSprite(Sprite original) => _reviveIcon.Get() ?? original;
        public static Sprite LoadIntroSprite() => _introSprite.Get();

        private static bool _reviveSpriteRegistered;

        public static void RegisterRoleSprites(RoleManager roleManager)
        {
            if (roleManager == null || _reviveSpriteRegistered) return;

            var revive = _reviveIcon.Get();
            if (revive == null)
            {
                MedicAPIPlugin.Log.LogWarning("reviveSprite asset is null; native MedicRole revive icon will use the game's fallback if available.");
                return;
            }

            // RoleManager.AddSprite wraps a native Dictionary.Add: calling it twice with the
            // same key throws mid-native-call and can leave the IL2CPP heap in a state that
            // segfaults later, so this must never run more than once per process.
            roleManager.AddSprite("reviveSprite", revive);
            _reviveSpriteRegistered = true;
        }
    }
}
