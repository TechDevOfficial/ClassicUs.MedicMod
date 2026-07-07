using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using TMPro;
using UnityEngine;

namespace ClassicUs.MedicMod
{
    public class MedicIntroOverlay : MonoBehaviour
    {
        public MedicIntroOverlay(IntPtr ptr) : base(ptr) { }

        private const float FadeInSeconds = 0.6f;
        private const float HoldSeconds = 1.4f;
        private const float FadeOutSeconds = 0.6f;

        public Action OnFinished;

        public void Begin()
        {
            var sprite = MedicAssets.LoadIntroSprite();
            if (sprite == null)
            {
                OnFinished?.Invoke();
                Destroy(gameObject);
                return;
            }

            var camera = Camera.main;

            var imageGo = new GameObject("MedicIntroImage");
            imageGo.transform.SetParent(transform, false);
            var renderer = imageGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = short.MaxValue;
            renderer.color = new Color(1f, 1f, 1f, 0f);

            if (camera != null)
            {
                float targetHeight = camera.orthographicSize * 2f * 0.6f;
                float spriteHeight = sprite.bounds.size.y;
                if (spriteHeight > 0f)
                {
                    float scale = targetHeight / spriteHeight;
                    imageGo.transform.localScale = new Vector3(scale, scale, 1f);
                }
                imageGo.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y + 0.3f, 0f);
            }

            var textGo = new GameObject("MedicIntroText");
            textGo.transform.SetParent(transform, false);
            var text = textGo.AddComponent<TextMeshPro>();
            text.text = "loading mod...";
            text.fontSize = 3f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 1f, 1f, 0f);
            text.sortingOrder = short.MaxValue;

            if (camera != null)
            {
                float spriteHeight = imageGo.transform.localScale.y * sprite.bounds.size.y;
                textGo.transform.position = new Vector3(
                    camera.transform.position.x,
                    imageGo.transform.position.y - spriteHeight / 2f - 0.6f,
                    0f);
            }

            this.StartCoroutine(Run(renderer, text));
        }

        private IEnumerator Run(SpriteRenderer renderer, TextMeshPro text)
        {
            for (float t = 0f; t < FadeInSeconds; t += Time.deltaTime)
            {
                SetAlpha(renderer, text, t / FadeInSeconds);
                yield return null;
            }
            SetAlpha(renderer, text, 1f);

            yield return new WaitForSeconds(HoldSeconds);

            for (float t = 0f; t < FadeOutSeconds; t += Time.deltaTime)
            {
                SetAlpha(renderer, text, 1f - t / FadeOutSeconds);
                yield return null;
            }
            SetAlpha(renderer, text, 0f);

            OnFinished?.Invoke();
            Destroy(gameObject);
        }

        private static void SetAlpha(SpriteRenderer renderer, TextMeshPro text, float alpha)
        {
            if (renderer != null)
            {
                var c = renderer.color;
                renderer.color = new Color(c.r, c.g, c.b, alpha);
            }
            if (text != null)
            {
                var c = text.color;
                text.color = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }
}
