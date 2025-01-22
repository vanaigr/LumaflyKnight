using System;
using System.Collections.Generic;
using UnityEngine;

namespace LumaflyKnight
{
    class Ui : MonoBehaviour {
        GameObject counter = null;
        GameObject geoText = null;

        public void Awake() {
            try {
                geoText = gameObject.transform.Find("Geo Text").gameObject;

                var result = new GameObject("Lumafly counter", typeof(TextMesh), typeof(MeshRenderer));
                result.layer = geoText.layer;

                TextMesh textMesh = result.GetComponent<TextMesh>();
                textMesh.alignment = TextAlignment.Left;
                textMesh.anchor = TextAnchor.UpperLeft;
                
                TextMesh geoTextMesh = geoText.GetComponent<TextMesh>();
                textMesh.font = geoTextMesh.font;
                textMesh.fontSize = (int)(geoTextMesh.fontSize * 0.6f);
                textMesh.text = "Loading...";

                MeshRenderer meshRenderer = result.GetComponent<MeshRenderer>();
                meshRenderer.material = textMesh.font.material;

                result.transform.parent = geoText.transform.parent.transform;
                result.transform.localScale = geoText.transform.localScale;
                result.transform.localPosition = geoText.transform.localPosition;

                counter = result;
            } catch (Exception e) {
                LumaflyKnight.Instance.Log("Error in Ui.Start() " + e);
            }
        }

        public void Update() {
            try {     
                var b = geoText.GetComponent<Renderer>().bounds;
                var pos = counter.transform.position;
                pos.x = b.min.x;
                pos.y = b.min.y - (b.max.y - b.min.y) * 0.2f;
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