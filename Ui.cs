using System;
using System.Collections.Generic;
using UnityEngine;

namespace LumaflyKnight
{
    class Ui : MonoBehaviour {
        private class RowLayoutObject {
            public float MinWidth = 0;
            public float WidthStepSize;
            public float PaddingRight = 0;

            public GameObject GameObject_ = null;

            public float GetRealWidth() {
                Renderer renderer = GameObject_?.GetComponent<Renderer>();
                if (renderer == null) {
                    throw new InvalidOperationException(
                        "GameObject_ must be non-null and have a renderer.");
                }

                Transform parentTransform = GameObject_.transform.parent;
                if (parentTransform == null) {
                    return renderer.bounds.size.x;
                } else {
                    Vector3 localSize =
                        parentTransform.InverseTransformVector(
                            renderer.bounds.size);
                    return localSize.x;
                }
            }

            public float GetComputedWidth() {
                float realWidth = GetRealWidth();
                float paddedRealWidth = realWidth + PaddingRight;
                float unroundedComputedWidth = Mathf.Max(
                    paddedRealWidth, MinWidth);
                if (unroundedComputedWidth <= MinWidth) {
                    return MinWidth;
                } else if (WidthStepSize <= 0) {
                    return unroundedComputedWidth;
                } else {
                    return MinWidth + WidthStepSize * Mathf.Ceil(
                        (unroundedComputedWidth - MinWidth) / WidthStepSize);
                }
            }
        }

        // The first object is the "anchor", it will not be moved but its
        // computed width will be used.
        private List<RowLayoutObject> _layout = new List<RowLayoutObject>();

        // The normal size of the geo count is rather large, such that adding
        // a bunch of grass stats next to it is overwhelmingly large. So we
        // scale it down by this factor.
        public float Scale = 0.6f;

        private GameObject _roomCount = null;
        private GameObject _globalCount = null;

        public void Start() {
            try {
                _Start();
            } catch (System.Exception e) {
                LumaflyKnight.Instance.Log("Error in Ui.Start() " + e);
            }
        }

        private void _Start() {
            _layout.Add(new RowLayoutObject {
                MinWidth = 1.4f, // A bit wider than 3 digits
                WidthStepSize = 0.5f, // Roughly 1 digit
                PaddingRight = 0.7f,
                GameObject_ = GetGeoTextObject(),
            });
            _roomCount = CreateTextObject("Room Grass Count");
            _layout.Add(new RowLayoutObject {
                MinWidth = 1.4f,
                WidthStepSize = 0.5f,
                PaddingRight = 0.7f,
                GameObject_ = _roomCount,
            });
            _globalCount = CreateTextObject("Global Grass Count");
            _layout.Add(new RowLayoutObject {
                MinWidth = 0,
                WidthStepSize = 0,
                PaddingRight = 0,
                GameObject_ = _globalCount,
            });
        }

        private GameObject CreateTextObject(string name) {
            GameObject result = new GameObject(
                name, typeof(TextMesh), typeof(MeshRenderer));
            result.layer = gameObject.layer;
            UnityEngine.Object.DontDestroyOnLoad(result);

            GameObject geoTextObject = GetGeoTextObject();

            TextMesh geoTextMesh = geoTextObject.GetComponent<TextMesh>();
            TextMesh textMesh = result.GetComponent<TextMesh>();
            textMesh.alignment = TextAlignment.Left;
            textMesh.anchor = TextAnchor.MiddleLeft;
            textMesh.font = geoTextMesh.font;
            textMesh.fontSize = geoTextMesh.fontSize;
            textMesh.text = geoTextMesh.text;

            MeshRenderer meshRenderer = result.GetComponent<MeshRenderer>();
            meshRenderer.material = textMesh.font.material;
            meshRenderer.enabled = false;

            result.transform.parent = gameObject.transform;
            result.transform.localScale = geoTextObject.transform.localScale;
            result.transform.localPosition = geoTextObject.transform.localPosition;

            return result;
        }

        public void Destroy() {
            try {
                UnityEngine.Object.Destroy(_roomCount);
            } catch (System.Exception e) {
                LumaflyKnight.Instance.Log("Error in Ui.Destroy() " + e);
            }
        }

        void Update() {
            try {
                _Update();
            } catch (System.Exception e) {
                LumaflyKnight.Instance.Log("Error in Ui.Update()"  + e);
            }
        }

        void _Update() {
            MaybeResize();
            ReflowLayout();
        }

        static Vector3 WithX(Vector3 v, float newX) {
            return new Vector3(newX, v.y, v.z);
        }

        void ReflowLayout() {
            if (_layout.Count <= 0) {
                return;
            }

            // The first component (the anchor) isn't created by us, and its
            // position is the center of itself (I think), so we use the
            // Renderer's bounds to get its leftmost edge.
            Transform anchorParentTransform =
                _layout[0].GameObject_.transform.parent;
            float anchorLeft = anchorParentTransform.InverseTransformPoint(
                _layout[0].GameObject_.GetComponent<Renderer>().bounds.min).x;

            float currentX = anchorLeft + _layout[0].GetComputedWidth();
            for (int i = 1; i < _layout.Count; ++i) {
                Transform transform = _layout[i].GameObject_.transform;
                transform.localPosition = WithX(transform.localPosition,
                                                currentX);
                currentX += _layout[i].GetComputedWidth();
                _layout[i].GameObject_.GetComponent<Renderer>().enabled = true;
            }
        }

        // Makes the counter smaller
        void MaybeResize() {
            if (gameObject.transform.localScale.x == Scale && 
                    gameObject.transform.localScale.y == Scale &&
                    gameObject.transform.localScale.z == Scale) {
                // Nothing to do!
                return;
            }

            // Fix the top left of the Geo Sprite in place as we shrink it
            GameObject child = GetSpriteObject();
            Vector2 startPosition = TopLeftOfBounds(
                child.GetComponent<Renderer>().bounds);
            gameObject.transform.localScale = Vector3.one * Scale;
            gameObject.transform.position -= (Vector3)(
                TopLeftOfBounds(child.GetComponent<Renderer>().bounds) -
                startPosition);
        }

        Vector2 TopLeftOfBounds(Bounds bounds) {
            return new Vector2(bounds.min.x, bounds.max.y);
        }

        GameObject GetGeoTextObject() {
            GameObject result = gameObject.transform.Find("Geo Text").gameObject;
            if (result == null) {
                throw new InvalidOperationException("Cannot find Geo Text.");
            }

            return result;
        }

        GameObject GetSpriteObject() {
            GameObject result = gameObject.transform.Find("Geo Sprite").gameObject;
            if (result == null) {
                throw new InvalidOperationException("Cannot find Geo Sprite.");
            }

            return result;
        }

        public void UpdateStats(int hits, int count, int totalHits, int totalCount) {
            _roomCount.GetComponent<TextMesh>().text = hits + "/" + count + " of " + totalHits + "/" + totalCount;
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
            Ui.getUi();
        }

        public static void add() { 
            GameManager.instance.gameObject.AddComponent<RegisterUi>();
        }
    }
}