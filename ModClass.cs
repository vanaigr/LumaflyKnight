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

namespace LumaflyKnight
{
    public class LumaflyKnight : Mod
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
            Log(indent + "name: " + it.name);
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

        class ContainLumafly {
            public List<GameObject> lamps;
            public List<GameObject> enemies;
            //public List<GameObject> unbreakableLamps;
        }

        void addAll(GameObject it, ContainLumafly cl) {
            var s = (string name) => it.name.StartsWith(name);

            if(s("lamp_bug_escape")) cl.lamps.Add(it);
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
        private object content;

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

        IEnumerator doStuff() {
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
            File.WriteAllText("C:\\Users\\Artem\\Downloads\\HKMODS\\LumaflyKnight\\all.json", resS);

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
            
            UnityEngine.Application.Quit(0);

            RegisterUi.add();

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
