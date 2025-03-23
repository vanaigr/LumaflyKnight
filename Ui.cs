using System;
using System.Reflection;
using UnityEngine;

namespace LumaflyKnight {
        class Ui : MonoBehaviour {
        GameObject geoSprite = null;
        GameObject geoText = null;
        GameObject counter = null;
        GameObject sprite = null;

        static Vector3 topLeft(Bounds bounds) {
            return new Vector3(bounds.min.x, bounds.max.y, 0);
        }

        public void Awake() {
            try {
                geoText = gameObject.transform.Find("Geo Text").gameObject;
                geoSprite = gameObject.transform.Find("Geo Sprite").gameObject;

                var beforePos = topLeft(geoSprite.GetComponent<Renderer>().bounds);
                gameObject.transform.localScale = Vector3.one * 0.6f;
                gameObject.transform.position = gameObject.transform.position 
                    - (topLeft(geoSprite.GetComponent<Renderer>().bounds) - beforePos);

                {
                    var bounds = geoSprite.GetComponent<Renderer>().bounds;
                    var pos = geoText.transform.position;
                    pos.x = bounds.max.x + bounds.size.x * 0.2f;
                    pos.y = bounds.center.y - bounds.size.y * 0.1f;
                    geoText.transform.position = pos;
                }

                TextMesh geoTextMesh = geoText.GetComponent<TextMesh>();

                geoTextMesh.alignment = TextAlignment.Left;
                geoTextMesh.anchor = TextAnchor.MiddleLeft;

                counter = new GameObject("Lumafly counter", typeof(TextMesh), typeof(MeshRenderer));
                counter.layer = geoText.layer;

                TextMesh textMesh = counter.GetComponent<TextMesh>();
                textMesh.alignment = TextAlignment.Left;
                textMesh.anchor = TextAnchor.MiddleLeft;


                textMesh.font = geoTextMesh.font;
                textMesh.fontSize = geoTextMesh.fontSize;
                textMesh.text = "Loading...";

                MeshRenderer meshRenderer = counter.GetComponent<MeshRenderer>();
                meshRenderer.material = textMesh.font.material;

                counter.transform.parent = geoText.transform.parent.transform;
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
                sprite.transform.localPosition = geoSprite.transform.localPosition;

                {
                    var bounds = geoSprite.GetComponent<Renderer>().bounds;
                    counter.transform.localScale = geoText.transform.localScale;
                    sprite.transform.localScale = Vector3.one * 0.8f;
                }
            }
            catch (Exception e) {
                LumaflyKnight.Instance.Log("Error in Ui.Start() " + e);
            }
        }

        public void Update() {
            try {
                var grassSprite = gameObject.transform.Find("Grass Sprite");
                if(grassSprite == null) {
                    var b = geoText.GetComponent<Renderer>().bounds;
                    var pos = sprite.transform.position;
                    pos.x = b.max.x + b.size.y * 0.5f;
                    sprite.transform.position = pos;

                    pos = sprite.transform.localPosition;
                    pos.x = Mathf.Ceil(pos.x * 3f) / 3f;
                    sprite.transform.localPosition = pos;
                }
                else {
                    var b = grassSprite.GetComponent<Renderer>().bounds;
                    var b2 = sprite.GetComponent<Renderer>().bounds;

                    var pos = sprite.transform.position;
                    pos.x = b.center.x - b2.size.x * 0.5f;
                    pos.y = b.min.y - b.size.y * 0.7f;
                    sprite.transform.position = pos;
                }

                {
                    var b = sprite.GetComponent<Renderer>().bounds;
                    var pos = counter.transform.position;
                    pos.x = b.max.x + b.size.x * 0.2f;
                    pos.y = b.min.y + b.size.y * 0.4f;
                    counter.transform.position = pos;
                }
            } 
            catch (System.Exception e) {
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
