using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

using UnityEngine;
using UnityEngine.SceneManagement;
using System.ComponentModel.Design.Serialization;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using System.Reflection;
using Modding.Converters;
using System.IO;
using Newtonsoft.Json;
using IL.InControl;
using UnityEngine.Assertions.Must;
using GlobalEnums;
using static GameManager;

namespace LumaflyKnight
{
    public class DoneSceneObjects {
        public HashSet<string> lamps = new HashSet<string>();
        public HashSet<string> enemies = new HashSet<string>();
    }

    public class DoneItems {
        public Dictionary<string, DoneSceneObjects> items = new Dictionary<string, DoneSceneObjects>();
    }

    public class GlobalSettings {
        public bool permanentLumaflyRelease = true;
    }

    public class LumaflyKnight : Mod, ILocalSettings<DoneItems>, IGlobalSettings<GlobalSettings>
    {
        internal static LumaflyKnight Instance;

        //public override List<ValueTuple<string, string>> GetPreloadNames()
        //{
        //    return new List<ValueTuple<string, string>>
        //    {
        //        new ValueTuple<string, string>("White_Palace_18", "White Palace Fly")
        //    };
        //}

        //public LumaflyKnight() : base("LumaflyKnight")
        //{
        //    Instance = this;
        //}

        /*public void reportAll(GameObject it, string indent)
        {
            Log(indent + "name: " + it.name + ", active=" + it.activeSelf + ", " + it.activeInHierarchy);
            Log(indent + " components:");
            var cs = it.GetComponents<Component>();
            for(int i = 0; i < cs.Length; i++) {
                Log(indent + "  " + cs[i].GetType().Name);
            }
            Log(indent + " children:");
            for(int i = 0; i < it.transform.childCount; i++) {
                reportAll(it.transform.GetChild(i).gameObject, indent + "  ");
            }
        }

        public void reportAllCurrentScene() {
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    var rs = s.GetRootGameObjects();
                    for(var i = 0; i < rs.Length; i++) {
                        reportAll(rs[i], "");
                    }
        }*/

        public class ContainLumafly {
            public List<GameObject> lamps;
            public List<GameObject> enemies;
            //public List<GameObject> unbreakableLamps;
        }

        public void addAll(GameObject it, ContainLumafly cl) {
            var s = (string name) => it.name.StartsWith(name);

            if(s("lamp_bug_escape")) {
                // why remove the object when you can delete the particle system, right?
                if(it.GetComponent<ParticleSystem>() != null) {
                    cl.lamps.Add(it);
                }
            }
            else if(s("Zombie Miner")) cl.enemies.Add(it);
            else if(s("Zombie Myla")) cl.enemies.Add(it);
            //else if(s("lamp_01")) cl.unbreakableLamps.Add(it);
            //else if(s("lamp_02")) cl.unbreakableLamps.Add(it);
            //else if(s("lamp_01_glows")) cl.unbreakableLamps.Add(it);
            //else if(s("lamp_02_glows")) cl.unbreakableLamps.Add(it);

            for(var i = 0; i < it.transform.childCount; i++) {
                addAll(it.transform.GetChild(i).gameObject, cl);
            }
        }

        public static FieldInfo breakableRemnantParts;

        public GameObject[] getRemnantParts(Breakable it) {
            if(breakableRemnantParts == null) {
                breakableRemnantParts = typeof(Breakable).GetField("debrisParts", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return ((List<GameObject>)breakableRemnantParts.GetValue(it)).ToArray();
        }

        public bool canReleaseLumafly(GameObject it, HashSet<GameObject> possibleRemnants, out GameObject root) {
            var p = it.transform.parent;
            if(p == null) {
                root = null;
                return false;
            }

            possibleRemnants.Add(it);

            var b = p.GetComponent<Breakable>();
            if(b) {
                var list = getRemnantParts(b);
                var i = 0;
                for(; i < list.Length; i++) {
                    if (possibleRemnants.Contains(list[i])) break;
                }
                
                if(i != list.Length && p.gameObject.activeInHierarchy) {
                    root = p.gameObject;
                    return true;
                }

                root = null;
                return false;
            }
            else if(p.gameObject.name.StartsWith("Chest")) {
                root = null;
                return true;
            }
            else {
                return canReleaseLumafly(p.gameObject, possibleRemnants, out root);
            }
        }

         public MethodInfo partsActivation = typeof(Breakable).GetMethod("SetStaticPartsActivation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
         public FieldInfo isBroken = typeof(Breakable).GetField("isBroken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public GameObject findInHierarchy(GameObject o, string[] names, int beginI) {
            if(beginI >= names.Length) return o;
            string name = names[beginI];
            var ct = o.transform.Find(name);
            if(ct == null) return null;
            return findInHierarchy(ct.gameObject, names, beginI + 1);
        }

        // 1. Searches inactive as well. 2. Can specify scene
        public GameObject find2(Scene s, string path) {
            if(path[0] != '/') throw new Exception("Not absolute path");
            var names = path.Split('/');
            if(names.Length <= 1) return null;
            var rs = s.GetRootGameObjects();
            foreach(var r in rs) {
                if(r.name == names[1]) return findInHierarchy(r, names, 2);
            }
            return null;
        }

        public IEnumerator prepareScene() { 
            var s0 = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var s0name = s0.name;
            yield return null;
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var sname = s.name;
            if(s != s0) {
                Log("Should not happen 1: " + s0name + " " + sname);
                yield break;
            }

            SceneObjects objs;
            if(!this.items.TryGetValue(s.name, out objs)) {
                Ui.getUi()?.UpdateStats(0, 0, data.totalHit, data.totalCount);
                yield break;
            }

            var hitCount = 0;
            var possibleCount = 0;

            foreach(var pair in objs.lamps) {
                possibleCount++;
                var i = pair.Key;
                var d = pair.Value;
                try {
                    if(data.hasLamp(sname, i)) {
                        hitCount++;
                        if(globalSettings.permanentLumaflyRelease) {
                            var obj = find2(s, i);
                            obj.SetActive(false);
                            if(d.brk != "") {
                                var pobj = find2(s, d.brk);
                                var pb = pobj.GetComponent<Breakable>();
                                isBroken.SetValue(pb, true);
                                partsActivation.Invoke(pb, new object[]{ true });
                            }
                        }
                    }
                    else { 
                        var obj = find2(s, i);
                        var u = obj.AddComponent<UpdateWhenActive>();
                        u.onEnable += (_, _) => {
                            if(data.addLamp(sname, i)) {
                                hitCount++;
                                Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
                            }
                        };
                    }
                }
                catch(Exception e) {
                    LogError("Died on lamp " + s.name + " " + i + ": " + e);
                }
            }

            foreach(var pair in objs.enemies) {
                possibleCount++;
                var i = pair.Key;
                try {
                    if(data.hasEnemy(sname, i)) {
                        hitCount++;
                        if(globalSettings.permanentLumaflyRelease) {
                            var obj = find2(s, i);
                            obj.SetActive(false);
                        }
                    }
                    else { 
                        var obj = find2(s, i);
                        obj.GetComponent<HealthManager>().OnDeath += () => {
                            if(data.addEnemy(sname, i)) {
                                hitCount++;
                                Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
                            }
                        };
                    }
                }
                catch(Exception e) {
                    LogError("Died on enemy " + s.name + " " + i + ": " + e);
                }
            }

            Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
        }

        public static string path(GameObject obj) {
            string path = "/" + obj.name;
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        /*public SceneObjects saveSceneObjects(Scene s) {
            var res = new SceneObjects();

            var lumas = new ContainLumafly { 
                enemies = new List<GameObject>(), 
                lamps = new List<GameObject>(),
            };
            
            var rs = s.GetRootGameObjects();
            for(var i = 0; i < rs.Length; i++) {
                addAll(rs[i], lumas);
            }
            Log("  Count is " + lumas.lamps.Count + " " + lumas.enemies.Count);

            for(var i = 0; i < lumas.lamps.Count; i++) {
                var l = lumas.lamps[i];
                GameObject root;
                if(canReleaseLumafly(l, new HashSet<GameObject>(), out root)) { 
                    // chests remember their state, so we will do nothing for them.
                    res.lamps.Add(path(l), new LampData { brk = root == null ? "" : path(root) });
                }
            }

            for(var i = 0; i < lumas.enemies.Count; i++) {
                var l = lumas.enemies[i];
                res.enemies.Add(path(l), new EnemyData());
            }

            return res;
        }*/

        public  struct LampData {
            public string brk; // path to GameObject with Breakable. Empty string if none
        }

        public  struct EnemyData {
        }

        public  class SceneObjects {
            public Dictionary<string, LampData> lamps = new Dictionary<string, LampData>();
            public Dictionary<string, EnemyData> enemies = new Dictionary<string, EnemyData>();
        }

        public Dictionary<string, SceneObjects> items;

        public struct Data {
            public int totalHit;
            public int totalCount;

            public DoneItems items;

            public bool hasLamp(string scene, string path) {
                if(items == null) {
                    LumaflyKnight.Instance.LogError("Should not happen 4");
                    return false;
                }

                DoneSceneObjects dso;
                if(!items.items.TryGetValue(scene, out dso)) return false;
                return dso.lamps.Contains(path);
            }
            public bool hasEnemy(string scene, string path) {
                if(items == null) {
                    LumaflyKnight.Instance.LogError("Should not happen 5");
                    return false;
                }

                DoneSceneObjects dso;
                if(!items.items.TryGetValue(scene, out dso)) return false;
                return dso.enemies.Contains(path);
            }

            public bool addLamp(string scene, string path) {
                if(items == null) {
                    LumaflyKnight.Instance.LogError("Should not happen 2");
                    return false;
                }

                DoneSceneObjects dso;
                if(!items.items.TryGetValue(scene, out dso)) {
                    dso = new DoneSceneObjects();
                    items.items.Add(scene, dso);
                }

                if(dso.lamps.Contains(path)) {
                    return false;
                }

                totalHit++;
                dso.lamps.Add(path);
                return true;
            }

            public bool addEnemy(string scene, string path) {
                if(items == null) {
                    LumaflyKnight.Instance.LogError("Should not happen 3");
                    return false;
                }

                DoneSceneObjects dso;
                if(!items.items.TryGetValue(scene, out dso)) {
                    dso = new DoneSceneObjects();
                    items.items.Add(scene, dso);
                }

                if(dso.enemies.Contains(path)) {
                    return false;
                }

                totalHit++;
                dso.enemies.Add(path);
                return true;
            }
        }

        public Data data;

        public void OnLoadLocal(DoneItems s) {
            data.items = s;
            int toalHit = 0;
            foreach (var it in s.items) {
                toalHit += it.Value.lamps.Count;
                toalHit += it.Value.enemies.Count;
            }
            data.totalHit = toalHit;
        }
        public DoneItems OnSaveLocal() => data.items;

        public GlobalSettings globalSettings = new GlobalSettings();

       public void OnLoadGlobal(GlobalSettings s) {
            globalSettings = s;
        }

        public GlobalSettings OnSaveGlobal() {
            return globalSettings;
        }

        public struct Entry {
            public float SqDist;
            public GameObject Obj;

            public Entry(float sqDist, GameObject obj)
            {
                SqDist = sqDist;
                Obj = obj;
            }
        }

        public void processAll(GameObject it, Action<GameObject> action) {
            action(it);
            for(int i = 0; i < it.transform.childCount; i++) {
                processAll(it.transform.GetChild(i).gameObject, action);
            }
        }

        public override string GetVersion() => "2";

        public class Anyception : Exception {
            public dynamic payload;
            public Anyception(dynamic payload) : base() { 
                this.payload = payload; 
            }
        }

            static Texture2D duplicateTexture(Texture2D source) {
                RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(source, temporary);
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = temporary;
                Texture2D texture2D = new Texture2D(source.width, source.height);
                texture2D.ReadPixels(new Rect(0f, 0f, (float)temporary.width, (float)temporary.height), 0, 0);
                texture2D.Apply();
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(temporary);
                return texture2D;
            }

        public IEnumerator doStuff() {
            /*
            // Update lumafly locations
            // Do at your own risk! Grants some achievements (e.g. speedrun).
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            var scenes = new Scene[sceneCount];
            Log("there's " + sceneCount + " scenes.");
            var result = new Dictionary<string, SceneObjects>();
            for(int i = 0; i < sceneCount; i++) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(i);
                yield return null; // we DO need to await for individual scenes and not load all + wait + process all
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(i);
                Log("  scene " + i + " is named " + s.name);
                var sr = saveSceneObjects(s);
                if(sr.lamps.Count > 0 || sr.enemies.Count > 0) {
                    result.Add(s.name, sr);
                }
                UnityEngine.SceneManagement.SceneManager.UnloadScene(i);
                //var roots = s.GetRootGameObjects();
                //for (int j = 0; j < roots.Length; j++) {
                //    reportAll(roots[j], "");
                //}
            }

            var resS = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            File.WriteAllText(path, resS);

            UnityEngine.Application.Quit(0);
            */

            /*
            // Extract lumafly icon
            // Do at your own risk! Grants some achievements (e.g. speedrun).
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            var scenes = new Scene[sceneCount];
            Log("there's " + sceneCount + " scenes.");
            var result = new Dictionary<string, SceneObjects>();
            for(int i = 0; i < sceneCount; i++) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(i);
                yield return null; // we DO need to await for individual scenes and not load all + wait + process all
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(i);
                Log("  scene " + i + " is named " + s.name);
                var roots = s.GetRootGameObjects();
                try {
                    for(var j = 0; j < roots.Length; j++) {
                        processAll(roots[j], it => {
                            if(it.name.StartsWith("shop_lamp_bug")) throw new Anyception(it);
                        });
                    }
                }
                catch(Anyception a) {
                    GameObject bug = a.payload as GameObject;
                    var sp = bug.GetComponent<SpriteRenderer>().sprite;
                    // texture atlas. Cannot be cropped here because none of the sprite's rect's have the right coords...
                    var array = duplicateTexture(sp.texture).EncodeToPNG();
                    using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write)) {
				        fileStream.Write(array, 0, array.Length);
                    }
                    Log("Extracted texture successfully");
                    break;
                }
                UnityEngine.SceneManagement.SceneManager.UnloadScene(i);
            }

            UnityEngine.Application.Quit(0);
            */

            string listStr = null;
            using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("list")) {
                var arr = new byte[s.Length];
                s.Read(arr, 0, arr.Length);
                listStr = System.Text.Encoding.UTF8.GetString(arr);
            }

            this.items = JsonConvert.DeserializeObject<Dictionary<string, SceneObjects>>(listStr);
            var totalCount = 0;
            foreach (var it in items) {
                totalCount += it.Value.lamps.Count;
                totalCount += it.Value.enemies.Count;
            }
            this.data.totalCount = totalCount;
            RegisterUi.add();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, _) => GameManager.instance.StartCoroutine(prepareScene());

            //GameManager.instance.gameObject.AddComponent<ModUpdate>();

            yield break;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            LumaflyKnight.Instance = this;
            GameManager.instance.StartCoroutine(doStuff());

            Log("Initialized");
        }
    }

    class UpdateWhenActive : MonoBehaviour {
        public event EventHandler onEnable;
        public void OnEnable() {
            onEnable?.Invoke(this, EventArgs.Empty);
        }
    }

    /*class ModUpdate : MonoBehaviour {
        int curRoomI = 0;
        string[] scenes = LumaflyKnight.Instance.items.Keys.ToArray();
        
        public void Update() {
            GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
            if (Input.GetKeyDown(KeyCode.Q)) {
               var list = new List<LumaflyKnight.Entry>();

               Action<GameObject> insert = (newObj) => {
                    var diff = (newObj.gameObject.transform.position - hero.transform.position);
                    var sqDist = diff.sqrMagnitude;

                    var newEntry = new LumaflyKnight.Entry(sqDist, newObj);
                    int index = list.BinarySearch(newEntry, Comparer<LumaflyKnight.Entry>.Create((a, b) => a.SqDist.CompareTo(b.SqDist)));

                    // If not found, BinarySearch returns a negative index that is the bitwise complement of the next larger element's index
                    if (index < 0) list.Add(newEntry);
                    else list.Insert(index, newEntry);

                    if(list.Count > 100) list.RemoveAt(list.Count - 1);
               };

               var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
               var rs = s.GetRootGameObjects();
               for(var i = 0; i < rs.Length; i++) {
                    LumaflyKnight.Instance.processAll(rs[i], insert);
               }

               LumaflyKnight.Instance.Log("Objects near the player:");
               for(var i = 0; i < list.Count; i++) {
                    var it = list[i].Obj;
                    LumaflyKnight.Instance.Log(i + ". name: " + LumaflyKnight.path(it));
                    LumaflyKnight.Instance.Log(" components:");
                    var cs = it.GetComponents<Component>();
                    for(int j = 0; j < cs.Length; j++) {
                        LumaflyKnight.Instance.Log("  " + cs[j].GetType().Name);
                    }
                    LumaflyKnight.Instance.Log("");
               }
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                LumaflyKnight.Instance.reportAllCurrentScene();
            }
            if (Input.GetKeyDown(KeyCode.K)) {
                try {
                for(; curRoomI > 0; curRoomI--) {
                    var k = scenes[curRoomI];
                    DoneSceneObjects it; 
                    if(!LumaflyKnight.Instance.data.items.items.TryGetValue(k, out it)) break;
                    var or = LumaflyKnight.Instance.items[k];
                    if(or.lamps.Count - it.lamps.Count != 0 || or.enemies.Count - it.enemies.Count != 0) {
                        break;
                    }
                }
                //curRoomI = Math.Max(curRoomI - 1, 0);
                GameManager.instance.StartCoroutine(LumaflyKnight.Instance.go(scenes[curRoomI]));
                }
                catch(Exception e) { LumaflyKnight.Instance.LogError(e); }

            }
            if(Input.GetKeyDown(KeyCode.L)) {
                try { 
                for(; curRoomI < scenes.Length - 1; curRoomI++) {
                    var k = scenes[curRoomI];
                    DoneSceneObjects it; 
                    if(!LumaflyKnight.Instance.data.items.items.TryGetValue(k, out it)) break;
                    var or = LumaflyKnight.Instance.items[k];
                    if(or.lamps.Count - it.lamps.Count != 0 || or.enemies.Count - it.enemies.Count != 0) {
                        break;
                    }
                }
                //curRoomI = Math.Min(curRoomI + 1, scenes.Length);
                GameManager.instance.StartCoroutine(LumaflyKnight.Instance.go(scenes[curRoomI]));
                    }
                catch(Exception e) { LumaflyKnight.Instance.LogError(e); }
            }
            if(Input.GetKeyDown(KeyCode.J)) {
                try { 
                    var list = new List<LumaflyKnight.Entry>();

                   Action<GameObject> insert = (newObj) => {
                        var diff = (newObj.gameObject.transform.position - hero.transform.position);
                        var sqDist = diff.sqrMagnitude;

                        var newEntry = new LumaflyKnight.Entry(sqDist, newObj);
                        int index = list.BinarySearch(newEntry, Comparer<LumaflyKnight.Entry>.Create((a, b) => a.SqDist.CompareTo(b.SqDist)));

                        // If not found, BinarySearch returns a negative index that is the bitwise complement of the next larger element's index
                        if (index < 0) list.Add(newEntry);
                        else list.Insert(index, newEntry);

                        if(list.Count > 100) list.RemoveAt(list.Count - 1);
                   };

                   var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                   var rs = s.GetRootGameObjects();
                   for(var i = 0; i < rs.Length; i++) {
                        LumaflyKnight.Instance.processAll(rs[i], insert);
                   }

                   hero.transform.position = list[Math.Min(list.Count - 1, 10)].Obj.transform.position;
                }
                catch(Exception e) { LumaflyKnight.Instance.LogError(e); }
            }
        }
    }*/

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
                textMesh.text = "Loading...";

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
                    LumaflyKnight.Instance.Log("aboba" + texture.LoadImage(arr, true));
                    LumaflyKnight.Instance.Log(texture.width + " " + texture.height);
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
                pos.x = b.max.x;
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
