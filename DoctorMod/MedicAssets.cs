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

        public static void RegisterRoleSprites(RoleManager roleManager)
        {
            if (roleManager == null) return;

            var revive = _reviveIcon.Get();
            if (revive == null)
            {
                MedicAPIPlugin.Log.LogWarning("reviveSprite asset is null; native MedicRole revive icon will use the game's fallback if available.");
                return;
            }

            roleManager.AddSprite("reviveSprite", revive);
        }
    }
}
