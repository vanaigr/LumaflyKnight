﻿using Modding;
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

namespace LumaflyKnight
{
    public class DoneSceneObjects {
        public HashSet<string> lamps = new HashSet<string>();
        public HashSet<string> enemies = new HashSet<string>();
    }

    public class DoneItems {
        public Dictionary<string, DoneSceneObjects> items = new Dictionary<string, DoneSceneObjects>();
    }

    public class LumaflyKnight : Mod, ILocalSettings<DoneItems>
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

        void reportAll(GameObject it, string indent)
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

        void reportAllCurrentScene() {
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    var rs = s.GetRootGameObjects();
                    for(var i = 0; i < rs.Length; i++) {
                        reportAll(rs[i], "");
                    }
        }

        class ContainLumafly {
            public List<GameObject> lamps;
            public List<GameObject> enemies;
            //public List<GameObject> unbreakableLamps;
        }

        void addAll(GameObject it, ContainLumafly cl) {
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

        static FieldInfo breakableRemnantParts;

        GameObject[] getRemnantParts(Breakable it) {
            if(breakableRemnantParts == null) {
                breakableRemnantParts = typeof(Breakable).GetField("debrisParts", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
            return ((List<GameObject>)breakableRemnantParts.GetValue(it)).ToArray();
        }

        GameObject isBreakable(GameObject it, HashSet<GameObject> possibleRemnants) {
            var p = it.transform.parent;
            if(p == null) { 
                return null;
            }

            possibleRemnants.Add(it);

            var b = p.GetComponent<Breakable>();
            if(b) {
                var list = getRemnantParts(b);
                var i = 0;
                for(; i < list.Length; i++) {
                    if (possibleRemnants.Contains(list[i])) break;
                }
                
                if(i != list.Length) {
                    return p.gameObject.activeInHierarchy ? p.gameObject : null;
                }

                return null;
            }
            else {
                return isBreakable(p.gameObject, possibleRemnants);
            }
        }

        /*IEnumerator updateCount() {                
            var s0 = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var s0name = s0.name;
            yield return null;
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(s != s0) {
                Log("Should not happen 1: " + s0name + " " + s.name);
                yield break;
            }
            Log("Room name: " + s.name + " " + s.buildIndex);

            try { 
                var rs = s.GetRootGameObjects();
                var lumas = new ContainLumafly { 
                    enemies = new List<GameObject>(), 
                    lamps = new List<GameObject>(),
                    unbreakableLamps = new List<GameObject>(),
                };
                for(var i = 0; i < rs.Length; i++) {
                    addAll(rs[i], lumas);
                }
                Log("Count is " + lumas.lamps.Count + " " + lumas.enemies.Count);

                var hitCount = 0;
                var possibleCount = 0;

                for(var i = 0; i < lumas.lamps.Count; i++) {
                    var l = lumas.lamps[i];
                    try {
                        Log("checking " + i);
                        if(isBreakable(l, new HashSet<GameObject>())) { 
                            var u = l.AddComponent<UpdateWhenActive>();
                            u.onEnable += (_, _) => {
                                hitCount++;
                                Ui.getUi()?.UpdateStats(hitCount, possibleCount);
                            };
                            possibleCount++;
                        }
                        else {
                            lumas.unbreakableLamps.Add(l);
                        }
                    }
                    catch(Exception e) {
                        LogError("Died on lamp " + s.name + " " + l.name + ": " + e);
                    }
                }

                for(var i = 0; i < lumas.enemies.Count; i++) {
                    var l = lumas.enemies[i];
                    try {
                        l.GetComponent<HealthManager>().OnDeath += () => {
                            hitCount++;
                                Ui.getUi()?.UpdateStats(hitCount, possibleCount);
                        };
                            possibleCount++;
                    }
                    catch(Exception e) {
                        LogError("Died on enemy " + s.name + " " + l.name + ": " + e);
                    }
                }

                for(var i = 0; i < lumas.unbreakableLamps.Count; i++) {
                    var l = lumas.unbreakableLamps[i];
                    try {
                        l.transform.localScale = l.transform.localScale * 3;
                    }
                    catch(Exception e) {
                        LogError("Died on unbreakable " + s.name + " " + l.name + ": " + e);
                    }
                }

                Ui.getUi()?.UpdateStats(hitCount, possibleCount);
            }
            catch(Exception e) {
                LogError("Counting died: " + e);
            }

            yield break;
        }*/

         MethodInfo partsActivation = typeof(Breakable).GetMethod("SetStaticPartsActivation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
         FieldInfo isBroken = typeof(Breakable).GetField("isBroken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        GameObject findInHierarchy(GameObject o, string[] names, int beginI) {
            if(beginI >= names.Length) return o;
            string name = names[beginI];
            var ct = o.transform.Find(name);
            if(ct == null) return null;
            return findInHierarchy(ct.gameObject, names, beginI + 1);
        }

        // 1. Searches inactive as well. 2. Can specify scene
        GameObject find2(Scene s, string path) {
            if(path[0] != '/') throw new Exception("Not absolute path");
            var names = path.Split('/');
            if(names.Length <= 1) return null;
            var rs = s.GetRootGameObjects();
            foreach(var r in rs) {
                if(r.name == names[1]) return findInHierarchy(r, names, 2);
            }
            return null;
        }

        IEnumerator prepareScene() { 
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
                        var obj = find2(s, i);
                        obj.SetActive(false);
                        var pobj = find2(s, d.deletePath);
                        var pb = pobj.GetComponent<Breakable>();
                        isBroken.SetValue(pb, true);
                        partsActivation.Invoke(pb, new object[]{ true });
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
                    reportAllCurrentScene();
                }
            }

            foreach(var pair in objs.enemies) {
                possibleCount++;
                var i = pair.Key;
                try {
                    if(data.hasEnemy(sname, i)) {
                        hitCount++;
                        var obj = find2(s, i);
                        obj.SetActive(false);
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

        SceneObjects saveSceneObjects(Scene s) {
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
                var root = isBreakable(l, new HashSet<GameObject>());
                if(root != null) { 
                    res.lamps.Add(path(l), new LampData { deletePath = path(root) });
                }
            }

            for(var i = 0; i < lumas.enemies.Count; i++) {
                var l = lumas.enemies[i];
                res.enemies.Add(path(l), new EnemyData());
            }

            return res;
        }

        private struct LampData {
            public string deletePath;
        }

        private struct EnemyData {
        }

        private class SceneObjects {
            public Dictionary<string, LampData> lamps = new Dictionary<string, LampData>();
            public Dictionary<string, EnemyData> enemies = new Dictionary<string, EnemyData>();
        }

        Dictionary<string, SceneObjects> items;

        private struct Data {
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

        private Data data;

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

        public struct Entry {
            public float SqDist;
            public GameObject Obj;

            public Entry(float sqDist, GameObject obj)
            {
                SqDist = sqDist;
                Obj = obj;
            }
        }

        void processAll(GameObject it, Action<GameObject> action) {
            action(it);
            for(int i = 0; i < it.transform.childCount; i++) {
                processAll(it.transform.GetChild(i).gameObject, action);
            }
        }

        IEnumerator doStuff() {
            /*
            // Do at your own risk! Grans some achievements (e.g. speedrun).
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

            UnityEngine.Application.Quit(0);*/

            /*var l = new List<string>();
            l.Add("Crossroads_27");
            for(int i = 0; i < l.Count; i++) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(l[i]);
                yield return null;
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneByName(l[i]);
                Log("  scene " + i + " is named " + s.name);
                var roots = s.GetRootGameObjects();
                for (int j = 0; j < roots.Length; j++) {
                    reportAll(roots[j], "");
                }
            }*/
            
            //UnityEngine.Application.Quit(0);

            this.items = JsonConvert.DeserializeObject<Dictionary<string, SceneObjects>>(Properties.Resources.items);
            var totalCount = 0;
            foreach (var it in items) {
                totalCount += it.Value.lamps.Count;
                totalCount += it.Value.enemies.Count;
            }
            this.data = new Data{ totalHit = 0, totalCount = totalCount };
            RegisterUi.add();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, _) => GameManager.instance.StartCoroutine(prepareScene());

            ModHooks.HeroUpdateHook += () => {
                GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
                if (hero != null && Input.GetKeyDown(KeyCode.Q)) {
                    var list = new List<Entry>();

                    Action<GameObject> insert = (newObj) => {
                        var diff = (newObj.gameObject.transform.position - hero.transform.position);
                        var sqDist = diff.sqrMagnitude;

                        var newEntry = new Entry(sqDist, newObj);
                        int index = list.BinarySearch(newEntry, Comparer<Entry>.Create((a, b) => a.SqDist.CompareTo(b.SqDist)));

                        // If not found, BinarySearch returns a negative index that is the bitwise complement of the next larger element's index
                        if (index < 0) list.Add(newEntry);
                        else list.Insert(index, newEntry);

                        if(list.Count > 100) list.RemoveAt(list.Count - 1);
                    };

                    var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    var rs = s.GetRootGameObjects();
                    for(var i = 0; i < rs.Length; i++) {
                        processAll(rs[i], insert);
                    }

                    Log("Objects near the player:");
                    for(var i = 0; i < list.Count; i++) {
                        var it = list[i].Obj;
                        Log(i + ". name: " + path(it));
                        Log(" components:");
                        var cs = it.GetComponents<Component>();
                        for(int j = 0; j < cs.Length; j++) {
                            Log("  " + cs[j].GetType().Name);
                        }
                        Log("");
                    }
                }
                if (hero != null && Input.GetKeyDown(KeyCode.P)) {
                    reportAllCurrentScene();
                }
            };


            //UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, _) => GameManager.instance.StartCoroutine(updateCount());

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
}
