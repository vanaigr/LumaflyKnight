﻿// Update lumafly locations
// Do at your own risk! Grants some achievements (e.g. speedrun).
//#define EXTRACT

// for debugging
#define TEST

using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using static Modding.IMenuMod;

// Zombie miner - Husk Miner
// Zombie beam miner - Crystallised Husk
// Note: don't rely on objects becoming inactive. They do wnen exiting rooms

/*namespace LumaflyKnight {
    static class Constants {
        public static string extractPath = "";
    }
}*/

namespace LumaflyKnight {
    public class DoneSceneObjects {
        public HashSet<string> lamps = new HashSet<string>();
        public HashSet<string> enemies = new HashSet<string>();
    }

    public class DoneItems {
        public Dictionary<string, DoneSceneObjects> items;
        public Dictionary<string, HashSet<string>> items2;
    }

    public class GlobalSettings {
        public bool permanentLumaflyRelease = true;
        public bool countZombieBeamMiners = true;
        public bool countChandelier = true;
        public bool countSeerAssension = true;
        public bool spawnLumaflies = true;
        public bool lumaflyLantern = true;
	}

    public partial class LumaflyKnight : Mod, IMenuMod, ILocalSettings<DoneItems>, IGlobalSettings<GlobalSettings> {
        internal static LumaflyKnight Instance;

        public override List<(string, string)> GetPreloadNames() {
            return new List<(string, string)> {
                // to get its escape particle system
                ("Room_Town_Stag_Station", "station_pole/Stag_Pole_Tall_Break (2)/lamp_bug_escape (7)"),
            };
        }

        public MethodInfo partsActivation = typeof(Breakable).GetMethod("SetStaticPartsActivation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public FieldInfo isBroken = typeof(Breakable).GetField("isBroken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static GameObject findInHierarchy(GameObject o, string[] names, int beginI) {
            if(beginI >= names.Length) return o;
            string name = names[beginI];
            var ct = o.transform.Find(name);
            if(ct == null) return null;
            return findInHierarchy(ct.gameObject, names, beginI + 1);
        }

        // 1. Searches inactive as well. 2. Can specify scene
        public static GameObject find2(Scene s, string path) {
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
                // Can happen in a boss room actually. Not sure what should happen.
                Log("Should not happen 1: " + s0name + " " + sname);
                yield break;
            }

            roomUpdate();
        }

        void roomUpdate() {
            if(!loadedItems) {
                Log("Should not happen: roomUpdate() #1: " + Environment.StackTrace);
                return;
            }
            if(!countedAll) {
                Log("Should not happen: roomUpdate() #2: " + Environment.StackTrace);
                return;
            }
            if(!countedDone) {
                Log("Should not happen: roomUpdate() #3: " + Environment.StackTrace);
                return;
            }

            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var sname = s.name;

            Log(sname);

            Dictionary<string, Type> objTypes;
            if(!data.itemType.TryGetValue(s.name, out objTypes)) {
                Log("  has no items");
                Ui.getUi()?.UpdateStats(0, 0, data.totalHit, data.totalCount);
                return;
            }
            Log("  has " + objTypes.Count + " items");

            var hitCount = 0;
            var possibleCount = 0;

            foreach(var pair in objTypes) {
                var path = pair.Key;
                var typ = pair.Value;
                var type = typ.type;
                var increase = countIncrease(typ);

                possibleCount += increase;

                try {
                    if(type == 7) {
                        var checkObj = new GameObject("LumaflyKnight Ending Checker", typeof(EndingChecker));
                        var checker = checkObj.AddComponent<EndingChecker>();

                        checker.path = path;
                        checker.scene = s;
                        checker.type = typ;
                        checker.onEnd += (_, args) => {
                            if(data.add(sname, path, typ)) {
                                hitCount += increase;
                                Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
                                if(globalSettings.spawnLumaflies) {
                                    showEffect(args.obj.transform.position);
                                }
                            }
                        };
                        continue;
                    }

                    var obj = find2(s, path);

                    if(data.has(sname, path)) {
                        hitCount += increase;

                        if(globalSettings.permanentLumaflyRelease) {
                            if(type == 0) {
                                obj.SetActive(false);

                                var d = (LampData)typ.data;
                                if(d.brk != "") {
                                    var pobj = find2(s, d.brk);
                                    var pb = pobj.GetComponent<Breakable>();
                                    isBroken.SetValue(pb, true);
                                    partsActivation.Invoke(pb, new object[]{ true });
                                }
                            }
                            else if(type == 1 || type == 2) {
                                obj.SetActive(false);
                            }
                        }
                    }
                    else {
                        if(type == 0 || type == 3 || type == 4) {
                            if(obj.activeInHierarchy) {
                                if(data.add(sname, path, typ)) {
                                    hitCount += increase;
                                }
                            }
                            else {
                                var u = obj.AddComponent<UpdateWhenActive>();
                                u.onEnable += (_, _) => {
                                    if(data.add(sname, path, typ)) {
                                        hitCount += increase;
                                        Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
                                        if(type == 4 && globalSettings.spawnLumaflies) {
                                            for(var i = 0; i < 5; i++) {
                                                showEffect(obj.transform.position + new Vector3((i - 2) * 0.7f, -7, 0));
                                            }
                                        }
                                    }
                                };
                            }
                        }
                        else if(type == 1 || type == 2) {
                            if(obj.name == "Zombie Myla") {
                                var obj2 = find2(s, "/Miner");
                                if(obj2 != null && obj2.activeInHierarchy) continue;
                            }

                            if(!obj.activeInHierarchy) {
                                if(data.add(sname, path, typ)) {
                                    hitCount += increase;
                                }
                            }
                            else {
                                if(type == 2 && globalSettings.spawnLumaflies) {
                                    // Thanks Peeka.
                                    // "I figured out why those don't, the Corpse component on those has the landEffects
                                    // field set to null so it doesn't do anything with the Land Effects child"
                                    var corpse = obj.GetComponentInChildren<Corpse>(true);
                                    if(corpse != null) {
                                        var target = corpse.gameObject.transform.Find("Land Effects");
                                        if(target != null) {
                                            var fi = corpse.GetType().GetField("landEffects", BindingFlags.Instance | BindingFlags.NonPublic);
                                            fi.SetValue(corpse, target.gameObject);
                                        }
                                    }
                                }

                                obj.GetComponent<HealthManager>().OnDeath += () => {
                                    if(data.add(sname, path, typ)) {
                                        hitCount += increase;
                                        Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
                                    }
                                };
                            }
                        }
                        else if(type == 5) {
                            var breakable = find2(s, ((LampData)typ.data).brk);
                            var fsm = breakable.GetComponent<PlayMakerFSM>();

                            if(fsm.FsmVariables.FindFsmBool("Activated").Value) {
                                if(data.add(sname, path, typ)) {
                                    hitCount += increase;
                                }
                            }
                            else {
                                if(globalSettings.spawnLumaflies) {
                                    obj.AddComponent<ReleaseLumaflyOnDestroy>();
                                }

                                var c = breakable.AddComponent<OnUpdate>();
                                c.onUpdate += (_, _) => {
                                    try {
                                        if(fsm.FsmVariables.FindFsmBool("Activated").Value) {
                                            // obj is deleted
                                            if(data.add(sname, path, typ)) {
                                                hitCount += increase;
                                                Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
                                            }
                                        }
                                    }
                                    catch(Exception e) {
                                        LumaflyKnight.Instance.LogError("Error: " + e);
                                    }
                                };
                            }
                        }
                        else if(type == 6) {
                            if(PlayerData.instance.mothDeparted) {
                                if(data.add(sname, path, typ)) {
                                    hitCount += increase;
                                }
                            }
                            else {
                                var c = obj.AddComponent<OnUpdate>();
                                c.onUpdate += (_, _) => {
                                    try {
                                        if(PlayerData.instance.mothDeparted) {
                                            if(data.add(sname, path, typ)) {
                                                hitCount += increase;
                                                Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
                                            }
                                        }
                                    }
                                    catch(Exception e) {
                                        LumaflyKnight.Instance.LogError("Error: " + e);
                                    }
                                };
                            }
                        }
                        else {
                            throw new Exception("Unreachable");
                        }
                    }
                }
                catch(Exception e) {
                    LogError("Died on object " + s.name + " " + path + ": " + e);
                    // This can happen if the object is permanently removed, but the game crashed
                    // and our data wasn't saved, but game data was.
                    if(data.add(sname, path, typ)) hitCount += increase;
                }
            }

            Ui.getUi()?.UpdateStats(hitCount, possibleCount, data.totalHit, data.totalCount);
        }

        static string seerScene = "RestingGrounds_07";
        // should be changed to just "/Dream Moth" but it's now used in several places and in save files
        static string seer = "/Dream Moth/Dream Dialogue";

        public static string lanternReleaseScene = "Dream_Final_Boss";
        public static string lanternRelease = "/hollow_knight_skull_left";


		public struct LampData {
            public string brk; // path to GameObject with Breakable. Empty string if none
            public int count; // number of lumaflies in a lamp
        }
        public struct ChestData {
            public int count; // turns out this is useless. It says 99, but 9 actually release
        }
        public struct SpecialData {}
        public struct EnemyData {}

        public class SceneObjects {
            public Dictionary<string, LampData> lamps;
            public Dictionary<string, EnemyData> enemies;
            public Dictionary<string, EnemyData> beamMiners;
            public Dictionary<string, ChestData> chests;
            public Dictionary<string, SpecialData> chandeliers;
            // BREAKABLE walls
            public Dictionary<string, LampData> lampsOnWalls;
            public SpecialData seer;
            public SpecialData lantern;
		}

        public struct Type {
            public int type; // 0 - lamps, 1 - enemies, etc.
            public object data;
        }

        public static int countIncrease(Type it) {
            if(it.type == 0) return ((LampData)it.data).count;
             // chest has 9 lumaflies. For some reason the data reports 99...
            if(it.type == 3) return 9;
            // chandelier has 5
            if(it.type == 4) return 5;
            return 1;
        }

        public class Data {
            public int totalHit;
            public int totalCount;

            public Dictionary<string, SceneObjects> allItems;
            // Doesn't contain an item if it is disabled by settings.
            public Dictionary<string, Dictionary<string, Type>> itemType;

            public Dictionary<string, HashSet<string>> done = new Dictionary<string, HashSet<string>>();

            public Type? getType(string scene, string path) {
                Dictionary<string, Type> a;
                if(!itemType.TryGetValue(scene, out a)) return null;
                Type type;
                if(!a.TryGetValue(path, out type)) return null;
                return type;
            }

            public bool has(string scene, string path) {
                if(done == null) {
                    LumaflyKnight.Instance.LogError("Should not happen 4: " + Environment.StackTrace);
                    return false;
                }

                HashSet<string> its;
                if(!done.TryGetValue(scene, out its)) return false;
                return its.Contains(path);
            }

            public bool add(string scene, string path, Type type) {
                if(done == null) {
                    LumaflyKnight.Instance.LogError("Should not happen 2: " + Environment.StackTrace);
                    return false;
                }

                HashSet<string> its;
                if(!done.TryGetValue(scene, out its)) {
                    its = new HashSet<string>();
                    done.Add(scene, its);
                }

                if(its.Contains(path)) {
                    return false;
                }

                totalHit += countIncrease(type);
                its.Add(path);

                return true;
            }
        }

        public Data data = new Data();

        public void OnLoadLocal(DoneItems s) {
            var ni = new Dictionary<string, HashSet<string>>();
            if(s.items2 != null) ni = s.items2;
            else if(s.items != null) {
                foreach(var p in s.items) {
                    var result = new HashSet<string>();
                    foreach(var item in p.Value.lamps) {
                        result.Add(item);
                    }
                    foreach(var item in p.Value.enemies) {
                        result.Add(item);
                    }
                    ni.Add(p.Key, result);
                }
            }

            data.done = ni;

            loadedLocalSettings = true;
            countedDone = false;
            refresh();
        }

        public DoneItems OnSaveLocal() => new DoneItems{ items2 = data.done };

        public GlobalSettings globalSettings = new GlobalSettings();

       public void OnLoadGlobal(GlobalSettings s) {
            globalSettings = s;
            loadedGlobalSettings = true;
            countedAll = false;
            refresh();
        }

        public GlobalSettings OnSaveGlobal() {
            return globalSettings;
        }

        public bool ToggleButtonInsideMenu { get { return true; } }

        static string on = "ON";
        static string off = "OFF";
        static string[] bools = { off, on };

        static int ind(bool v) => v ? 1 : 0;

        void menu() {
            countedAll = false;
            countedDone = false;
            refresh();
        }

        public List<MenuEntry> GetMenuData(MenuEntry? toggleButtonEntry) {
            var res  = new List<MenuEntry>();

            res.Add(new MenuEntry(
                "Permanent release", bools, "Keep poles broken and enemies dead if they have lumaflies.",
                (i) => {
                    globalSettings.permanentLumaflyRelease = i != 0;
                    menu();
                },
                () => ind(globalSettings.permanentLumaflyRelease)
            ));

            res.Add(new MenuEntry(
                "Count Crystallized Husks", bools, "With beams. Their lumafly just disappears on death.",
                (i) => {
                    globalSettings.countZombieBeamMiners = i != 0;
                    menu();
                },
                () => ind(globalSettings.countZombieBeamMiners)
            ));

            res.Add(new MenuEntry(
                "Count chandelier", bools, "Watcher Knights chandelier and the wall before it. Lumaflies just disappear.",
                (i) => {
                    globalSettings.countChandelier = i != 0;
                    menu();
                },
                () => ind(globalSettings.countChandelier)
            ));

            res.Add(new MenuEntry(
                "Count Seer assension", bools, "The 2400 essence check.",
                (i) => {
                    globalSettings.countSeerAssension = i != 0;
                    menu();
                },
                () => ind(globalSettings.countSeerAssension)
            ));

            res.Add(new MenuEntry(
                "Add extra lumafly effects", bools, "Adds freed effect to Crystallized Hustks, chandelier, and the wall.",
                (i) => {
                    globalSettings.spawnLumaflies = i != 0;
                    menu();
                },
                () => ind(globalSettings.spawnLumaflies)
            ));

            return res;
        }

        public void processAll(GameObject it, Action<GameObject> action) {
            action(it);
            for(int i = 0; i < it.transform.childCount; i++) {
                processAll(it.transform.GetChild(i).gameObject, action);
            }
        }

        public override string GetVersion() => "11";

        static bool loadedItems;
        // they are (and must be) set to default values if not loaded normally
        static bool loadedGlobalSettings = true;
        static bool loadedLocalSettings = true;
        static bool countedDone;
        static bool countedAll;

        void refresh() {
            if(!loadedItems) {
                return;
            }

            if(loadedGlobalSettings && !countedAll) {
                var itemType = new Dictionary<string, Dictionary<string, Type>>();
                var totalCount = 0;

                try {
                    foreach (var it in data.allItems) {
                        var v = it.Value;
                        var res = new Dictionary<string, Type>();

                        var add = (int type, string k, object v) => {
                            var data = new Type { type = type, data = v };
                            totalCount += countIncrease(data);
                            res.Add(k, data);
                        };

                        foreach(var p in it.Value.lamps) add(0, p.Key, p.Value);
                        foreach(var p in it.Value.enemies) add(1, p.Key, p.Value);
                        if(globalSettings.countZombieBeamMiners) {
                            foreach (var p in it.Value.beamMiners) add(2, p.Key, p.Value);
                        }
                        foreach (var p in it.Value.chests) add(3, p.Key, p.Value);
                        if(globalSettings.countChandelier) {
                            foreach (var p in it.Value.chandeliers) add(4, p.Key, p.Value);
                        }
                        if(globalSettings.countChandelier) {
                            foreach (var p in it.Value.lampsOnWalls) add(5, p.Key, p.Value);
                        }

                        itemType.Add(it.Key, res);
                    }

                    if(globalSettings.countSeerAssension) {
                        Dictionary<string, Type> res;
                        if(!itemType.TryGetValue(seerScene, out res)) {
                            res = new Dictionary<string, Type>();
                            itemType.Add(seerScene, res);
                        }

                        var data = new Type { type = 6, data = new SpecialData{} };
                        totalCount += countIncrease(data);
                        res.Add(seer, data);
                    }

					if (globalSettings.lumaflyLantern)
					{
						Dictionary<string, Type> res;
						if (!itemType.TryGetValue(lanternReleaseScene, out res))
						{
							res = new Dictionary<string, Type>();
							itemType.Add(lanternReleaseScene, res);
						}

						var data = new Type { type = 7, data = new SpecialData { } };
						totalCount += countIncrease(data);
						res.Add(LumaflyKnight.lanternRelease, data);
					}
				} catch(Exception e) {
                    LogError("Died: " + e);
                }

                data.itemType = itemType;
                data.totalCount = totalCount;

                countedAll = true;
                countedDone = false;
            }

            if(loadedLocalSettings && countedAll && !countedDone) {
                int totalHit = 0;

                foreach(var scene in data.done) {
                    Dictionary<string, Type> vs;
                    if(!data.itemType.TryGetValue(scene.Key, out vs)) continue;

                    foreach(var path in scene.Value) {
                        Type type;
                        if(!vs.TryGetValue(path, out type)) continue;
                        totalHit += countIncrease(type);
                    }
                }

                data.totalHit = totalHit;
                countedDone = true;
            }

            if(countedAll && countedDone && GameManager.instance != null) {
                // If outside the game, nothing will happen anyway
                roomUpdate();
            }
        }

        public class Obj {
            public FieldInfo fi;
            public PropertyInfo pi;
            public object value;
        }

        // https://discussions.unity.com/t/how-to-add-a-copy-of-particle-system-of-another-object-in-scene-using-c-script/610663
        // https://discussions.unity.com/t/copy-a-component-at-runtime/71172
        public static List<Obj> toProps(object src) {
            var res = new List<Obj>();

            System.Type type = src.GetType();
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach(var field in fields) {
                res.Add(new Obj { fi = field, value = field.GetValue(src) });
            }

            foreach(var prop in type.GetProperties()) {
                if(!prop.CanWrite || !prop.CanRead) continue;
                res.Add(new Obj { pi = prop, value = prop.GetValue(src) });
            }

            return res;
        }

        public static void setProps(object dst, List<Obj> values) {
            foreach(var value in values) {
                if(value.fi != null) value.fi.SetValue(dst, value.value);
                if(value.pi != null) value.pi.SetValue(dst, value.value);
            }
        }

        public class EffectInfo {
            public List<Obj> particleSystemInfo;
            public List<Obj> particleSystemMainInfo;
            public List<Obj> particleSystemTextureSheetInfo;
            public List<Obj> particleSystemSizeOverLifetime;
            public List<Obj> rendererInfo;
        }
        public static EffectInfo effectInfo;

        public static void showEffect(Vector3 position) {
            try {
                var target = new GameObject("", typeof(ParticleSystem), typeof(ParticleSystemRenderer));

                var newPS = target.GetComponent<ParticleSystem>();
                var targetRenderer = target.GetComponent<ParticleSystemRenderer>();

                LumaflyKnight.setProps(newPS, LumaflyKnight.effectInfo.particleSystemInfo);
                LumaflyKnight.setProps(newPS.main, LumaflyKnight.effectInfo.particleSystemMainInfo);
                LumaflyKnight.setProps(newPS.textureSheetAnimation, LumaflyKnight.effectInfo.particleSystemTextureSheetInfo);
                LumaflyKnight.setProps(newPS.sizeOverLifetime, LumaflyKnight.effectInfo.particleSystemSizeOverLifetime);
                LumaflyKnight.setProps(targetRenderer, LumaflyKnight.effectInfo.rendererInfo);

                target.transform.position = position;
                newPS.Play();
            }
            catch(Exception e) {
                if(LumaflyKnight.Instance != null) LumaflyKnight.Instance.LogError("Effect spawn failed");
            }
        }

        public IEnumerator doStuff(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
#if EXTRACT
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            var scenes = new Scene[sceneCount];
            Log("there's " + sceneCount + " scenes.");
            var result = new Dictionary<string, SceneObjects>();
            for(int i = 0; i < sceneCount; i++) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(i);
                yield return null; // we DO need to await for individual scenes and not load all + wait + process all
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(i);
                Log("  scene " + i + " is named " + s.name);
                SceneObjects sr;
                try {
                    sr = saveSceneObjects(s);
                }
                catch(Exception e) {
                    LogError("Error: " + e);
                    yield break;
                }
                if(sr.lamps.Count > 0 || sr.enemies.Count > 0 || sr.beamMiners.Count > 0 || sr.chests.Count > 0 || sr.chandeliers.Count > 0 || sr.lampsOnWalls.Count > 0) {
                    result.Add(s.name, sr);
                }
                UnityEngine.SceneManagement.SceneManager.UnloadScene(i);
                //var roots = s.GetRootGameObjects();
                //for (int j = 0; j < roots.Length; j++) {
                //    reportAll(roots[j], "");
                //}
            }

            var resS = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            File.WriteAllText(Constants.extractPath, resS);

            UnityEngine.Application.Quit(0);
#endif

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

            try {
                var e = effectInfo = new EffectInfo{};

                var source = preloadedObjects.First().Value.First().Value;
                var sourcePS = source.GetComponent<ParticleSystem>();
                e.particleSystemInfo = toProps(sourcePS);
                e.particleSystemMainInfo = toProps(sourcePS.main);
                e.particleSystemTextureSheetInfo = toProps(sourcePS.textureSheetAnimation);
                e.particleSystemSizeOverLifetime = toProps(sourcePS.sizeOverLifetime);
                e.rendererInfo = toProps(source.GetComponent<ParticleSystemRenderer>());
            }
            catch(Exception err) {
                Log(err);
            }

            string listStr = null;
            using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("list")) {
                var arr = new byte[s.Length];
                s.Read(arr, 0, arr.Length);
                listStr = System.Text.Encoding.UTF8.GetString(arr);
            }
            data.allItems = JsonConvert.DeserializeObject<Dictionary<string, SceneObjects>>(listStr);
            loadedItems = true;
            countedAll = false;
            countedDone = false;

            RegisterUi.add();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, _) => GameManager.instance.StartCoroutine(prepareScene());

            #if TEST
            GameManager.instance.gameObject.AddComponent<ModUpdate>();
            #endif

            refresh();

            yield break;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initialize() {");

            LumaflyKnight.Instance = this;
            GameManager.instance.StartCoroutine(doStuff(preloadedObjects));

            Log("Initialize() }");
        }
    }

    class UpdateWhenActive : MonoBehaviour {
        public event EventHandler onEnable;
        public void OnEnable() {
            onEnable?.Invoke(this, EventArgs.Empty);
        }
    }

    class OnUpdate : MonoBehaviour {
        public event EventHandler onUpdate;
        public void Update() {
            onUpdate?.Invoke(this, EventArgs.Empty);
        }
    }

    class ReleaseLumaflyOnDestroy : MonoBehaviour { 
        public void OnDestroy() {
            LumaflyKnight.showEffect(gameObject.transform.position);
        }
    }

    class EndingChecker : MonoBehaviour {
        public string path;
        public Scene scene;
        public LumaflyKnight.Type type;
        public event EventHandler<EndArgs> onEnd;

        public class EndArgs : EventArgs {
            public GameObject obj;
        }

        public void Update() {
            var obj = LumaflyKnight.find2(scene, path);
            if(!obj) return;
            if(!obj.activeInHierarchy) return;
            if(!PlayerData.instance.hasLantern) return;
            onEnd?.Invoke(this, new EndArgs { obj = obj });
        }
    }

    public partial class LumaflyKnight {
        #if EXTRACT || TEST
        public void reportAll(GameObject it, string indent)
        {
            Log(indent + "name: '" + it.name + "', active=" + it.activeSelf + ", " + it.activeInHierarchy);
            Log(indent + " components:");
            var cs = it.GetComponents<Component>();
            for(int i = 0; i < cs.Length; i++) {
                Log(indent + "  " + cs[i].GetType().Name);
                /*if(cs[i].GetType() == typeof(ParticleSystem)) {
                    var p = (cs[i] as ParticleSystem);
                    Log(indent + "    rate: " + p.emission.rateOverTime.constantMin + ", " + p.emission.rateOverTime.constantMax);
                    Log(indent + "    main: " + p.main.maxParticles);
                    var bs = new ParticleSystem.Burst[p.emission.burstCount];
                    p.emission.GetBursts(bs);
                    for(var j = 0; j < bs.Length; j++) {
                        var b = bs[j];
                        Log(indent + "    burst " + j + ": " + b.minCount + ", " + b.maxCount + ", " + b.cycleCount);
                    }
                }*/
            }
            Log(indent + " children:");
            for(int i = 0; i < it.transform.childCount; i++) {
                reportAll(it.transform.GetChild(i).gameObject, indent + "  ");
            }
        }

        public void reportAllCurrentScene() {
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rs = s.GetRootGameObjects();
            Log("Scene name is: '" + s.name + "'");
            for(var i = 0; i < rs.Length; i++) {
                reportAll(rs[i], "");
            }
        }

        public class ContainLumafly {
            public List<GameObject> bugEscape;
            public List<GameObject> zombieMiners;
            public List<GameObject> zombieBeamMiners;
            public GameObject chandelier;
             // some breakable walls have lumaflies. record all and filter later
            public List<GameObject> lamps;

            //public List<GameObject> unbreakableLamps;
        }

        public void addAll(GameObject it, ContainLumafly cl) {
            var s = (string name) => it.name.StartsWith(name);

            if(s("lamp_bug_escape")) {
                // why remove the object when you can delete the particle system, right?
                if(it.GetComponent<ParticleSystem>() != null) {
                    cl.bugEscape.Add(it);
                }
            }
            else if(it.name == "chandelier_broken") {
                cl.chandelier = it;
            }
            else if(s("Zombie Miner")) cl.zombieMiners.Add(it);
            else if(s("Zombie Myla")) cl.zombieMiners.Add(it);
            else if(s("Zombie Beam Miner")) {
                // Mines_32 /Battle Scene/Zombie Beam Miner Rematch
                // GG_Crystal_Guardian_2 /Battle Scene/Zombie Beam Miner Rematch
                // But crystal guardian 2 doesn't have lumaflies
                if(!it.name.Contains("Rematch")) {
                    cl.zombieBeamMiners.Add(it);
                }
            }
            else if(it.name.Contains("lamp_bug")) cl.lamps.Add(it);
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

        class LumaflyReleaseInfo {
            public GameObject root;
            public bool chest;
        }

        LumaflyReleaseInfo canReleaseLumafly(GameObject it, HashSet<GameObject> possibleRemnants) {
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

                if(i != list.Length && p.gameObject.activeInHierarchy) {
                    return new LumaflyReleaseInfo{ root = p.gameObject };
                }

                return null;
            }
            else if(p.gameObject.name.StartsWith("Chest")) {
                return new LumaflyReleaseInfo { chest = true };
            }
            else {
                return canReleaseLumafly(p.gameObject, possibleRemnants);
            }
        }

        class BreakableWallInfo {
            public GameObject wall;
        }

        BreakableWallInfo isOnBreakableWall(GameObject it) {
            var cur = it.transform.parent;
            while(cur != null) {
                if(cur.name.StartsWith("Breakable Wall")) {
                    return new BreakableWallInfo { wall = cur.gameObject };
                }
                cur = cur.transform.parent;
            }

            return null;
        }

                public static string path(GameObject obj) {
            string path = "/" + obj.name;
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public SceneObjects saveSceneObjects(Scene s) {
            var lamps = new Dictionary<string, LampData>();
            var enemies = new Dictionary<string, EnemyData>();
            var beamMiners = new Dictionary<string, EnemyData>();
            var chests = new Dictionary<string, ChestData>();
            var chandeliers = new Dictionary<string, SpecialData>();
            var lampsOnWalls = new Dictionary<string, LampData>();

            var lumas = new ContainLumafly {
                bugEscape = new List<GameObject>(),
                zombieMiners = new List<GameObject>(),
                zombieBeamMiners = new List<GameObject>(),
                lamps = new List<GameObject>(),
            };

            var rs = s.GetRootGameObjects();
            for(var i = 0; i < rs.Length; i++) {
                addAll(rs[i], lumas);
            }
            Log("  Count is " + lumas.bugEscape.Count + " " + lumas.zombieMiners.Count + " " + lumas.zombieBeamMiners.Count + " " + (lumas.chandelier != null));

            for(var i = 0; i < lumas.bugEscape.Count; i++) {
                var l = lumas.bugEscape[i];
                var info = canReleaseLumafly(l, new HashSet<GameObject>());
                if(info != null) {
                    var ps = l.GetComponent<ParticleSystem>();
                    var particlesC = ps.main.maxParticles;
                    if(info.chest) {
                        chests.Add(path(l), new ChestData { count = particlesC });
                    }
                    else {
                        lamps.Add(path(l), new LampData{ brk = path(info.root), count = particlesC });
                    }
                }
            }
            if(lumas.chandelier != null) {
                chandeliers.Add(path(lumas.chandelier), new SpecialData());
            }

            for(var i = 0; i < lumas.zombieMiners.Count; i++) {
                var l = lumas.zombieMiners[i];
                enemies.Add(path(l), new EnemyData());
            }

            for(var i = 0; i < lumas.zombieBeamMiners.Count; i++) {
                var l = lumas.zombieBeamMiners[i];
                beamMiners.Add(path(l), new EnemyData());
            }

            for(var i = 0; i < lumas.lamps.Count; i++) {
                var l = lumas.lamps[i];
                var info = isOnBreakableWall(l);
                if(info == null) continue;
                lampsOnWalls.Add(path(l), new LampData { brk = path(info.wall) });
            }

            return new SceneObjects{
                lamps = lamps,
                enemies = enemies,
                beamMiners = beamMiners,
                chests = chests,
                chandeliers = chandeliers,
                lampsOnWalls = lampsOnWalls,
            };
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
        #endif
    }

    #if TEST
    public struct Entry {
        public float SqDist;
        public GameObject Obj;

        public Entry(float sqDist, GameObject obj)
        {
            SqDist = sqDist;
            Obj = obj;
        }
    }

    public class Anyception : Exception {
        public dynamic payload;
        public Anyception(dynamic payload) : base() {
            this.payload = payload;
        }
    }

    class ModUpdate : MonoBehaviour {
        int curRoomI = 0;
        string[] scenes = LumaflyKnight.Instance.data.allItems.Keys.ToArray();

        public void Update() {
            GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
            if (Input.GetKeyDown(KeyCode.Q)) {
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
               for(var i = 0; i < list.Count; i++) {
                    GameObject.Destroy(list[i].Obj);
                }
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                LumaflyKnight.Instance.reportAllCurrentScene();
            }

            if (Input.GetKeyDown(KeyCode.O)) {
                try {
                    var target = new GameObject("", typeof(ParticleSystem), typeof(ParticleSystemRenderer));

                    var newPS = target.GetComponent<ParticleSystem>();
                    var targetRenderer = target.GetComponent<ParticleSystemRenderer>();

                    LumaflyKnight.setProps(newPS, LumaflyKnight.effectInfo.particleSystemInfo);
                    LumaflyKnight.setProps(newPS.main, LumaflyKnight.effectInfo.particleSystemMainInfo);
                    LumaflyKnight.setProps(newPS.textureSheetAnimation, LumaflyKnight.effectInfo.particleSystemTextureSheetInfo);
                    LumaflyKnight.setProps(targetRenderer, LumaflyKnight.effectInfo.rendererInfo);

                    target.transform.position = hero.transform.position;
                    newPS.Play();
                }
                catch(Exception e) {
                    LumaflyKnight.Instance.LogError("" + e);
                }
            }
        }
    }
    #endif
}
