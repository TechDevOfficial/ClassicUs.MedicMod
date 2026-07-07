using ClassicUs.ManuAPI;
using UnityEngine;

namespace ClassicUs.MedicMod
{
    internal class ReviveAbility : CustomAbility
    {
        private static DeadBody _highlightedBody;
        private static SpriteRenderer _highlightedRenderer;
        private static Material _highlightedMaterial;
        private static Color _originalRendererColor;
        private static bool _hadOutline;
        private static float _originalOutline;
        private static bool _hadOutlineColor;
        private static Color _originalOutlineColor;
        private static bool _hadOutlineWidth;
        private static float _originalOutlineWidth;
        private static GameObject _highlightObject;
        private static SpriteRenderer _highlightRenderer;

        protected override string Name => "ReviveButton";
        protected override float Cooldown => MedicAPIPlugin.ActiveCooldown;

        protected override Sprite CreateIcon(Sprite original) => MedicAssets.LoadReviveSprite(original);

        protected override bool IsVisible()
        {
            var local = PlayerControl.LocalPlayer;
            bool visible = MedicAPIPlugin.IsMedic(local) && local.Data != null && !local.Data.IsDead;
            UpdateTargetHighlight(visible ? local : null);
            return visible;
        }

        protected override bool CanActivate()
        {
            var local = PlayerControl.LocalPlayer;
            return MedicAPIPlugin.IsMedic(local) && local.Data != null && !local.Data.IsDead
                   && FindNearbyBody(local) != null;
        }

        protected override void OnActivate()
        {
            var local = PlayerControl.LocalPlayer;
            if (!MedicAPIPlugin.IsMedic(local) || local.Data == null || local.Data.IsDead) return;

            var body = FindNearbyBody(local);
            if (body == null) return;

            MedicNetworking.RequestRevive(local.Data.PlayerId, body.ParentId);
        }

        private static DeadBody FindNearbyBody(PlayerControl local)
        {
            if (local == null) return null;

            Vector2 myPos = local.GetTruePosition();
            DeadBody closest = null;
            float closestDist = MedicAPIPlugin.ActiveRange;

            foreach (var body in Object.FindObjectsOfType<DeadBody>())
            {
                if (body == null) continue;
                float dist = Vector2.Distance(myPos, body.TruePosition);
                if (dist > closestDist) continue;

                closestDist = dist;
                closest = body;
            }

            return closest;
        }

        private static void UpdateTargetHighlight(PlayerControl local)
        {
            var target = FindNearbyBody(local);
            if (target == null || target.MyRend == null)
            {
                ClearTargetHighlight();
                return;
            }

            if (_highlightedBody != target)
                CreateTargetHighlight(target);

            ApplyTargetOutline(target);

            if (_highlightObject == null || _highlightRenderer == null || target.MyRend == null) return;

            _highlightRenderer.sprite = target.MyRend.sprite;
            _highlightRenderer.flipX = target.MyRend.flipX;
            _highlightRenderer.flipY = target.MyRend.flipY;
            _highlightRenderer.sortingLayerID = target.MyRend.sortingLayerID;
            _highlightRenderer.sortingOrder = target.MyRend.sortingOrder - 1;

            float pulse = 0.55f + 0.25f * Mathf.Sin(Time.time * 7f);
            _highlightRenderer.color = new Color(0.3f, 0.95f, 1f, pulse);

            var source = target.MyRend.transform;
            var highlightTransform = _highlightObject.transform;
            highlightTransform.position = source.position + new Vector3(0f, 0f, 0.02f);
            highlightTransform.rotation = source.rotation;
            highlightTransform.localScale = source.lossyScale * 1.18f;
        }

        private static void CreateTargetHighlight(DeadBody target)
        {
            ClearTargetHighlight();

            _highlightedBody = target;
            _highlightedRenderer = target.MyRend;
            if (_highlightedRenderer == null) return;

            _highlightedMaterial = _highlightedRenderer.material;
            _originalRendererColor = _highlightedRenderer.color;
            CacheOutlineState(_highlightedMaterial);

            if (CanUseMaterialOutline(_highlightedMaterial))
                return;

            _highlightObject = new GameObject("MedicReviveTargetHighlight");
            _highlightRenderer = _highlightObject.AddComponent<SpriteRenderer>();

            if (target.MyRend != null)
            {
                _highlightRenderer.sprite = target.MyRend.sprite;
                _highlightRenderer.material = target.MyRend.material;
            }
        }

        private static void CacheOutlineState(Material material)
        {
            _hadOutline = material != null && material.HasProperty("_Outline");
            _originalOutline = _hadOutline ? material.GetFloat("_Outline") : 0f;
            _hadOutlineColor = material != null && material.HasProperty("_OutlineColor");
            _originalOutlineColor = _hadOutlineColor ? material.GetColor("_OutlineColor") : Color.clear;
            _hadOutlineWidth = material != null && material.HasProperty("_OutlineWidth");
            _originalOutlineWidth = _hadOutlineWidth ? material.GetFloat("_OutlineWidth") : 0f;
        }

        private static bool CanUseMaterialOutline(Material material)
        {
            return material != null && material.HasProperty("_Outline") && material.HasProperty("_OutlineColor");
        }

        private static void ApplyTargetOutline(DeadBody target)
        {
            if (_highlightedRenderer == null || _highlightedMaterial == null || !CanUseMaterialOutline(_highlightedMaterial))
                return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
            Color outlineColor = Color.Lerp(new Color(0.05f, 0.75f, 1f, 1f), Color.white, pulse * 0.45f);

            _highlightedMaterial.SetFloat("_Outline", 1f);
            _highlightedMaterial.SetColor("_OutlineColor", outlineColor);
            if (_highlightedMaterial.HasProperty("_OutlineWidth"))
                _highlightedMaterial.SetFloat("_OutlineWidth", 0.08f + 0.03f * pulse);

            _highlightedRenderer.color = Color.Lerp(_originalRendererColor, new Color(0.65f, 1f, 1f, _originalRendererColor.a), 0.25f + 0.15f * pulse);
        }

        private static void ClearTargetHighlight()
        {
            if (_highlightedRenderer != null)
                _highlightedRenderer.color = _originalRendererColor;

            if (_highlightedMaterial != null)
            {
                if (_hadOutline)
                    _highlightedMaterial.SetFloat("_Outline", _originalOutline);

                if (_hadOutlineColor)
                    _highlightedMaterial.SetColor("_OutlineColor", _originalOutlineColor);

                if (_hadOutlineWidth)
                    _highlightedMaterial.SetFloat("_OutlineWidth", _originalOutlineWidth);
            }

            _highlightedBody = null;
            _highlightedRenderer = null;
            _highlightedMaterial = null;
            _highlightRenderer = null;
            _hadOutline = false;
            _hadOutlineColor = false;
            _hadOutlineWidth = false;

            if (_highlightObject != null)
            {
                Object.Destroy(_highlightObject);
                _highlightObject = null;
            }
        }

        public static void ClearHighlight() => ClearTargetHighlight();
    }

    internal static class ReviveAbilityHolder
    {
        private static readonly ReviveAbility _ability = new();
        public static void Tick(HudManager hud) => _ability.Tick(hud);
        public static void Reset()
        {
            ReviveAbility.ClearHighlight();
            _ability.Reset();
        }
    }
}
