using System;
using System.Reflection;
using UnityEngine;

namespace LumaflyKnight {
        class Ui : MonoBehaviour {
        GameObject geoSprite = null;
        GameObject geoText = null;
        GameObject counter = null;
        GameObject sprite = null;

        public void Awake() {
            try {
                geoText = gameObject.transform.Find("Geo Text").gameObject;
                geoSprite = gameObject.transform.Find("Geo Sprite").gameObject;

                geoText.transform.localScale = geoText.transform.localScale * 0.6f;
                geoSprite.transform.localScale = geoSprite.transform.localScale * 0.6f;

                var bounds = geoSprite.GetComponent<Renderer>().bounds;
                TextMesh geoTextMesh = geoText.GetComponent<TextMesh>();

                geoTextMesh.alignment = TextAlignment.Left;
                geoTextMesh.anchor = TextAnchor.MiddleLeft;

                var pos = geoText.transform.position;
                pos.x = bounds.max.x + bounds.size.x * 0.2f;
                pos.y = bounds.center.y - bounds.size.y * 0.1f;
                geoText.transform.position = pos;

                counter = new GameObject("Lumafly counter", typeof(TextMesh), typeof(MeshRenderer));
                counter.layer = geoText.layer;

                TextMesh textMesh = counter.GetComponent<TextMesh>();
                textMesh.alignment = TextAlignment.Left;
                textMesh.anchor = TextAnchor.MiddleLeft;


                textMesh.font = geoTextMesh.font;
                textMesh.fontSize = geoTextMesh.fontSize;
                textMesh.text = "Error..?";

                MeshRenderer meshRenderer = counter.GetComponent<MeshRenderer>();
                meshRenderer.material = textMesh.font.material;

                counter.transform.parent = geoText.transform.parent.transform;
                counter.transform.localScale = geoText.transform.localScale;
                counter.transform.localPosition = geoText.transform.localPosition;

                sprite = new GameObject("Lumafly counter sprite", typeof(SpriteRenderer));
                sprite.layer = geoSprite.layer;
                var renderer = sprite.GetComponent<SpriteRenderer>();

                using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("lumafly.png")) {
                    byte[] arr = new byte[s.Length];
                    s.Read(arr, 0, arr.Length);

                    Texture2D texture = new Texture2D(1, 1);
                    texture.LoadImage(arr, true);
                    renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 0.5f), Math.Max(texture.width, texture.height));
                }
                renderer.sortingOrder = 30000;

                sprite.transform.parent = geoSprite.transform.parent.transform;
                sprite.transform.localScale = new Vector3(bounds.size.y, bounds.size.y, 1);
                sprite.transform.localPosition = geoSprite.transform.localPosition;

            } catch (Exception e) {
                LumaflyKnight.Instance.Log("Error in Ui.Start() " + e);
            }
        }

        public void Update() {
            try {
                var b = geoText.GetComponent<Renderer>().bounds;
                var pos = sprite.transform.position;
                pos.x = b.max.x + b.size.y * 0.5f;
                sprite.transform.position = pos;
                pos = sprite.transform.localPosition;
                pos.x = Mathf.Ceil(pos.x * 3f) / 3f;
                sprite.transform.localPosition = pos;

                b = sprite.GetComponent<Renderer>().bounds;
                pos = counter.transform.position;
                pos.x = b.max.x + b.size.x * 0.2f;
                counter.transform.position = pos;
            } catch (System.Exception e) {
                LumaflyKnight.Instance.Log("Error in Ui.Update()"  + e);
            }
        }

        public void UpdateStats(int hits, int count, int totalHits, int totalCount) {
            try {
                counter.GetComponent<TextMesh>().text = hits + "/" + count + " | " + totalHits + "/" + totalCount;
            }
            catch(Exception e) { LumaflyKnight.Instance.LogError(e); }
        }

        public static Ui getUi() {
            GameObject geoCounter = GameManager.instance?.hero_ctrl?.geoCounter?.gameObject;
             if (geoCounter != null) {
                var c = geoCounter.GetComponent<Ui>();
                if(c == null) c = geoCounter.AddComponent<Ui>();

                return c;
             }
             return null;
        }
    }

    class RegisterUi : MonoBehaviour {
        public void Update() {
            try {
                Ui.getUi();
            } catch(Exception e) {
                LumaflyKnight.Instance.LogError(e);
            }
        }

        public static void add() {
            GameManager.instance.gameObject.AddComponent<RegisterUi>();
        }
    }
}
