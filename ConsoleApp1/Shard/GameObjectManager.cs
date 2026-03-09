/*
*
*   This manager class makes sure update gets called when it should on all the game objects, 
*       and also handles the pre-physics and post-physics ticks.  It also deals with 
*       transient objects (like bullets) and removing destroyed game objects from the system.
*   @author Michael Heron
*   @version 1.0
*   
*/

using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Globalization;

namespace Shard
{
    class SceneObjectData
    {
        public string TypeName { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Rotz { get; set; }
        public float Scalex { get; set; }
        public float Scaley { get; set; }
        public string SpritePath { get; set; }
        public bool Visible { get; set; }
        public bool Transient { get; set; }
        public int ParentIndex { get; set; }
        public List<string> ComponentTypeNames { get; set; }
        public Dictionary<string, string> FieldValues { get; set; }
    }

    class SceneData
    {
        public List<SceneObjectData> Objects { get; set; } = new List<SceneObjectData>();
    }

    class GameObjectManager
    {
        class PlaySnapshotEntry
        {
            public GameObject Original { get; set; }
            public SceneObjectData Data { get; set; }
        }

        private static GameObjectManager me;
        List<GameObject> myObjects;
        private List<PlaySnapshotEntry> _playSnapshot;

        private GameObjectManager()
        {
            myObjects = new List<GameObject>();
        }

        public static GameObjectManager getInstance()
        {
            if (me == null)
            {
                me = new GameObjectManager();
            }

            return me;
        }

        public List<GameObject> GetGameObjects()
        {
            return myObjects;
        }

        public void addGameObject(GameObject gob)
        {
            myObjects.Add(gob);

        }

        public void removeGameObject(GameObject gob)
        {
            myObjects.Remove(gob);
        }


        public void physicsUpdate()
        {
            GameObject gob;
            for (int i = 0; i < myObjects.Count; i++)
            {
                gob = myObjects[i];
                gob.physicsUpdateComponents();
                gob.physicsUpdate();
            }
        }

        public void prePhysicsUpdate()
        {
            GameObject gob;
            for (int i = 0; i < myObjects.Count; i++)
            {
                gob = myObjects[i];

                gob.prePhysicsUpdate();
            }
        }

        public void update()
        {
            List<int> toDestroy = new List<int>();
            GameObject gob;
            for (int i = 0; i < myObjects.Count; i++)
            {
                gob = myObjects[i];

                gob.updateComponents();
                gob.update();

                gob.checkDestroyMe();

                if (gob.ToBeDestroyed == true)
                {
                    toDestroy.Add(i);
                }
            }

            if (toDestroy.Count > 0)
            {
                for (int i = toDestroy.Count - 1; i >= 0; i--)
                {
                    gob = myObjects[toDestroy[i]];
                    myObjects[toDestroy[i]].killMe();
                    myObjects.RemoveAt(toDestroy[i]);

                }
            }

            toDestroy.Clear();

            //            Debug.Log ("NUm Objects is " + myObjects.Count);
        }

        public void BeginPlaySnapshot()
        {
            _playSnapshot = BuildPlaySnapshot();
        }

        public void RestorePlaySnapshot()
        {
            if (_playSnapshot == null || _playSnapshot.Count == 0)
            {
                return;
            }

            HashSet<GameObject> originals = new HashSet<GameObject>();
            foreach (PlaySnapshotEntry entry in _playSnapshot)
            {
                if (entry.Original != null)
                {
                    originals.Add(entry.Original);
                }
            }

            List<GameObject> toRemove = new List<GameObject>();
            foreach (GameObject gob in myObjects)
            {
                if (!originals.Contains(gob))
                {
                    toRemove.Add(gob);
                }
            }

            foreach (GameObject gob in toRemove)
            {
                removeGameObjectImmediate(gob);
            }

            List<GameObject> restoredObjects = new List<GameObject>();

            foreach (PlaySnapshotEntry entry in _playSnapshot)
            {
                GameObject target = entry.Original;

                if (target == null || !myObjects.Contains(target) || target.Transform == null)
                {
                    target = instantiateFromTypeName(entry.Data.TypeName);
                }

                if (target == null)
                {
                    continue;
                }

                applyState(target, entry.Data);
                restoredObjects.Add(target);
            }

            for (int i = 0; i < _playSnapshot.Count; i++)
            {
                if (i >= restoredObjects.Count)
                {
                    break;
                }

                SceneObjectData state = _playSnapshot[i].Data;
                GameObject target = restoredObjects[i];

                if (state.ParentIndex >= 0 && state.ParentIndex < restoredObjects.Count)
                {
                    target.Transform.Parent = restoredObjects[state.ParentIndex].Transform;
                }
                else
                {
                    target.Transform.Parent = null;
                }
            }

            _playSnapshot = null;
        }

        public bool SaveSceneToFile(string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                SceneData scene = BuildSceneData();
                JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(scene, options);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.getInstance().log("SaveSceneToFile failed: " + ex.Message, Debug.DEBUG_LEVEL_ERROR);
                return false;
            }
        }

        public bool LoadSceneFromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                string json = File.ReadAllText(path);
                SceneData scene = JsonSerializer.Deserialize<SceneData>(json);

                if (scene == null || scene.Objects == null)
                {
                    return false;
                }

                ClearScene();

                List<GameObject> loaded = new List<GameObject>();
                foreach (SceneObjectData state in scene.Objects)
                {
                    GameObject gob = CreateObjectFromTypeName(state.TypeName);
                    if (gob == null)
                    {
                        gob = new GameObject();
                    }

                    applyState(gob, state);
                    restoreComponents(gob, state);
                    loaded.Add(gob);
                }

                for (int i = 0; i < scene.Objects.Count; i++)
                {
                    int parentIndex = scene.Objects[i].ParentIndex;
                    if (parentIndex >= 0 && parentIndex < loaded.Count)
                    {
                        loaded[i].Transform.Parent = loaded[parentIndex].Transform;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.getInstance().log("LoadSceneFromFile failed: " + ex.Message, Debug.DEBUG_LEVEL_ERROR);
                return false;
            }
        }

        public void ClearScene()
        {
            for (int i = myObjects.Count - 1; i >= 0; i--)
            {
                removeGameObjectImmediate(myObjects[i]);
            }
            myObjects.Clear();
        }

        public GameObject CreateObjectFromTypeName(string typeName)
        {
            return instantiateFromTypeName(typeName);
        }

        private SceneData BuildSceneData()
        {
            SceneData scene = new SceneData();
            Dictionary<GameObject, int> indexMap = new Dictionary<GameObject, int>();

            int idx = 0;
            foreach (GameObject gob in myObjects)
            {
                if (gob == null || gob.Transform == null)
                {
                    continue;
                }

                indexMap[gob] = idx;
                idx += 1;
            }

            foreach (GameObject gob in myObjects)
            {
                if (gob == null || gob.Transform == null)
                {
                    continue;
                }

                SceneObjectData state = createState(gob);
                if (gob.Transform.Parent != null && gob.Transform.Parent.Owner != null && indexMap.ContainsKey(gob.Transform.Parent.Owner))
                {
                    state.ParentIndex = indexMap[gob.Transform.Parent.Owner];
                }
                else
                {
                    state.ParentIndex = -1;
                }

                scene.Objects.Add(state);
            }

            return scene;
        }

        private List<PlaySnapshotEntry> BuildPlaySnapshot()
        {
            List<PlaySnapshotEntry> snapshot = new List<PlaySnapshotEntry>();
            SceneData scene = BuildSceneData();
            int index = 0;

            foreach (GameObject gob in myObjects)
            {
                if (gob == null || gob.Transform == null)
                {
                    continue;
                }

                snapshot.Add(new PlaySnapshotEntry
                {
                    Original = gob,
                    Data = scene.Objects[index]
                });

                index += 1;
            }

            return snapshot;
        }

        private SceneObjectData createState(GameObject gob)
        {
            List<string> compNames = new List<string>();
            foreach (Component c in gob.GetComponents())
            {
                if (c == null)
                {
                    continue;
                }

                compNames.Add(c.GetType().FullName);
            }

            return new SceneObjectData
            {
                TypeName = gob.GetType().FullName,
                X = gob.Transform.X,
                Y = gob.Transform.Y,
                Rotz = gob.Transform.Rotz,
                Scalex = gob.Transform.Scalex,
                Scaley = gob.Transform.Scaley,
                SpritePath = toScenePath(gob.Transform.SpritePath),
                Visible = gob.Visible,
                Transient = gob.Transient,
                ParentIndex = -1,
                ComponentTypeNames = compNames,
                FieldValues = captureSerializableFields(gob)
            };
        }

        private void applyState(GameObject gob, SceneObjectData state)
        {
            gob.Transform.X = state.X;
            gob.Transform.Y = state.Y;
            gob.Transform.Rotz = state.Rotz;
            gob.Transform.Scalex = state.Scalex;
            gob.Transform.Scaley = state.Scaley;
            gob.Transform.SpritePath = fromScenePath(state.SpritePath);
            gob.Visible = state.Visible;
            gob.Transient = state.Transient;
            gob.ToBeDestroyed = false;
            gob.Transform.Parent = null;
            applySerializableFields(gob, state.FieldValues);
        }

        private string toScenePath(string spritePath)
        {
            if (string.IsNullOrWhiteSpace(spritePath))
            {
                return spritePath;
            }

            try
            {
                if (!Path.IsPathRooted(spritePath))
                {
                    return spritePath.Replace('/', Path.DirectorySeparatorChar);
                }

                string baseDir = Bootstrap.getBaseDir();
                if (string.IsNullOrWhiteSpace(baseDir))
                {
                    return spritePath;
                }

                string fullBase = Path.GetFullPath(baseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string fullSprite = Path.GetFullPath(spritePath);

                StringComparison cmp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (fullSprite.StartsWith(fullBase + Path.DirectorySeparatorChar, cmp) || string.Equals(fullSprite, fullBase, cmp))
                {
                    return Path.GetRelativePath(fullBase, fullSprite);
                }

                return spritePath;
            }
            catch
            {
                return spritePath;
            }
        }

        private string fromScenePath(string spritePath)
        {
            if (string.IsNullOrWhiteSpace(spritePath))
            {
                return spritePath;
            }

            try
            {
                if (Path.IsPathRooted(spritePath))
                {
                    return spritePath;
                }

                string baseDir = Bootstrap.getBaseDir();
                if (string.IsNullOrWhiteSpace(baseDir))
                {
                    return spritePath;
                }

                return Path.GetFullPath(Path.Combine(baseDir, spritePath));
            }
            catch
            {
                return spritePath;
            }
        }

        private Dictionary<string, string> captureSerializableFields(GameObject gob)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (gob == null)
            {
                return data;
            }

            Type t = gob.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            while (t != null && t != typeof(GameObject))
            {
                foreach (FieldInfo field in t.GetFields(flags))
                {
                    if (field.IsStatic || field.IsInitOnly || !field.IsPublic)
                    {
                        continue;
                    }

                    if (!isSupportedSceneFieldType(field.FieldType))
                    {
                        continue;
                    }

                    object value = field.GetValue(gob);
                    if (value == null)
                    {
                        continue;
                    }

                    data[field.Name] = serializeFieldValue(value, field.FieldType);
                }

                t = t.BaseType;
            }

            return data;
        }

        private void applySerializableFields(GameObject gob, Dictionary<string, string> fieldValues)
        {
            if (gob == null || fieldValues == null || fieldValues.Count == 0)
            {
                return;
            }

            Type t = gob.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (KeyValuePair<string, string> kv in fieldValues)
            {
                FieldInfo field = t.GetField(kv.Key, flags);
                if (field == null || field.IsStatic || field.IsInitOnly || !field.IsPublic)
                {
                    continue;
                }

                if (!isSupportedSceneFieldType(field.FieldType))
                {
                    continue;
                }

                object parsed;
                if (!tryParseFieldValue(kv.Value, field.FieldType, out parsed))
                {
                    continue;
                }

                field.SetValue(gob, parsed);
            }
        }

        private bool isSupportedSceneFieldType(Type type)
        {
            return type == typeof(int)
                || type == typeof(float)
                || type == typeof(bool)
                || type == typeof(string)
                || type == typeof(double);
        }

        private string serializeFieldValue(object value, Type type)
        {
            if (type == typeof(float))
            {
                return ((float)value).ToString(CultureInfo.InvariantCulture);
            }

            if (type == typeof(double))
            {
                return ((double)value).ToString(CultureInfo.InvariantCulture);
            }

            if (type == typeof(bool))
            {
                return ((bool)value) ? "true" : "false";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private bool tryParseFieldValue(string raw, Type type, out object parsed)
        {
            parsed = null;

            if (type == typeof(string))
            {
                parsed = raw;
                return true;
            }

            if (type == typeof(int))
            {
                int v;
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                {
                    parsed = v;
                    return true;
                }
                return false;
            }

            if (type == typeof(float))
            {
                float v;
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                {
                    parsed = v;
                    return true;
                }
                return false;
            }

            if (type == typeof(double))
            {
                double v;
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                {
                    parsed = v;
                    return true;
                }
                return false;
            }

            if (type == typeof(bool))
            {
                bool v;
                if (bool.TryParse(raw, out v))
                {
                    parsed = v;
                    return true;
                }
                return false;
            }

            return false;
        }

        private GameObject instantiateFromTypeName(string typeName)
        {
            try
            {
                Type t = resolveType(typeName);
                if (t == null)
                {
                    t = typeof(GameObject);
                }

                object obj = Activator.CreateInstance(t);
                return obj as GameObject;
            }
            catch (Exception ex)
            {
                Debug.getInstance().log("instantiateFromTypeName failed: " + ex.Message, Debug.DEBUG_LEVEL_WARNING);
                return null;
            }
        }

        private void removeGameObjectImmediate(GameObject gob)
        {
            if (gob == null)
            {
                return;
            }

            gob.killMe();
            myObjects.Remove(gob);
        }

        private void restoreComponents(GameObject gob, SceneObjectData state)
        {
            if (state.ComponentTypeNames == null)
            {
                return;
            }

            foreach (string typeName in state.ComponentTypeNames)
            {
                Type t = resolveType(typeName);
                if (t == null || !typeof(Component).IsAssignableFrom(t))
                {
                    continue;
                }

                bool exists = false;
                foreach (Component c in gob.GetComponents())
                {
                    if (c.GetType() == t)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                {
                    continue;
                }

                Component created = Activator.CreateInstance(t) as Component;
                if (created != null)
                {
                    gob.addComponent(created);
                }
            }
        }

        private Type resolveType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            Type t = Type.GetType(typeName);
            if (t != null)
            {
                return t;
            }

            Assembly asm = Assembly.GetExecutingAssembly();
            t = asm.GetType(typeName);
            if (t != null)
            {
                return t;
            }

            foreach (Type at in asm.GetTypes())
            {
                if (at.Name == typeName)
                {
                    return at;
                }
            }

            return null;
        }

    }
}
