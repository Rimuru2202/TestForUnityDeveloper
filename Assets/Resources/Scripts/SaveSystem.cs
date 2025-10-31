using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts
{
    [DisallowMultipleComponent]
    public class SaveSystem : MonoBehaviour
    {
        private const string PlayerPrefsKey = "save_data_string";

        private ResourceManager _resourceManager;

        public static SaveSystem Instance { get; private set; }

        [Header("Auto-save")]
        [Tooltip("Интервал автосохранения в секундах.")]
        public float autoSaveInterval = 5f;

        [Tooltip("Если true — сохраняем каждые autoSaveInterval независимо от наличия изменений.")]
        public bool alwaysSaveIntervally;

        [Tooltip("Если true — объект не уничтожается при загрузке сцены.")]
        public bool dontDestroyOnLoad = true;

        private bool _isDirty;
        private Coroutine _autoSaveRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        [Obsolete("Obsolete")]
        private void Start()
        {
            _resourceManager = FindObjectOfType<ResourceManager>();
            Load();

            if (autoSaveInterval > 0f)
                _autoSaveRoutine = StartCoroutine(AutoSaveRoutine());
        }
        
        [Obsolete("Obsolete")]
        private IEnumerator AutoSaveRoutine()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.1f, autoSaveInterval));
            while (true)
            {
                yield return wait;
                if (alwaysSaveIntervally)
                {
                }
                else
                {
                    if (!_isDirty) continue;
                }

                Save();
                _isDirty = false;
            }
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        [Obsolete("Obsolete")]
        public void ForceSaveImmediate()
        {
            Save();
            _isDirty = false;
        }

        [Obsolete("Obsolete")]
        private void Save()
        {
            try
            {
                var collected = _resourceManager != null ? _resourceManager.GetAllCollected() : new Dictionary<string, int>();

                var buildings = new Dictionary<string, int>();
                var allBuildings = FindObjectsOfType<Building>();
                foreach (var b in allBuildings)
                {
                    string id;
                    try { id = b.BuildingIdForSave; }
                    catch { id = b.gameObject.name; }

                    if (string.IsNullOrEmpty(id))
                        id = b.gameObject.name;

                    int stored;
                    try { stored = b.GetStoredAmount(); }
                    catch { stored = 0; }

                    buildings[id] = stored;
                }

                var s = SaveData.FromDictionaries(collected, buildings);
                var json = JsonUtility.ToJson(s);

#if UNITY_WEBGL
                PlayerPrefs.SetString(PlayerPrefsKey, json);
                PlayerPrefs.Save();
#else
                try
                {
                    var path = Path.Combine(Application.persistentDataPath, SaveFileName);
                    File.WriteAllText(path, json);
#if UNITY_EDITOR
                    Debug.Log($"SaveSystem: saved to {path}");
#endif
                }
                catch (Exception fileEx)
                {
                    Debug.LogWarning($"SaveSystem: file write failed, falling back to PlayerPrefs. {fileEx.Message}");
                    PlayerPrefs.SetString(PlayerPrefsKey, json);
                    PlayerPrefs.Save();
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SaveSystem.Save failed: {ex.Message}");
            }
        }

        [Obsolete("Obsolete")]
        private void Load()
        {
            try
            {
                string json = null;

#if UNITY_WEBGL
                if (PlayerPrefs.HasKey(PlayerPrefsKey))
                    json = PlayerPrefs.GetString(PlayerPrefsKey);
#else
                var path = Path.Combine(Application.persistentDataPath, SaveFileName);
                if (File.Exists(path))
                {
                    json = File.ReadAllText(path);
                }
                else if (PlayerPrefs.HasKey(PlayerPrefsKey))
                {
                    json = PlayerPrefs.GetString(PlayerPrefsKey);
                }
#endif
                if (string.IsNullOrEmpty(json))
                {
#if UNITY_EDITOR
                    Debug.Log("SaveSystem.Load: no save data found.");
#endif
                    return;
                }

                var s = JsonUtility.FromJson<SaveData>(json);
                if (s == null)
                {
                    return;
                }

                var collectedDict = s.ToCollectedDictionary();
                if (_resourceManager != null)
                    _resourceManager.SetAllCollected(collectedDict);

                var buildingsDict = s.ToBuildingsDictionary();
                if (buildingsDict is { Count: > 0 })
                {
                    var allBuildings = FindObjectsOfType<Building>();
                    foreach (var b in allBuildings)
                    {
                        string id;
                        try { id = b.BuildingIdForSave; }
                        catch { id = b.gameObject.name; }

                        if (string.IsNullOrEmpty(id))
                            id = b.gameObject.name;

                        if (buildingsDict.TryGetValue(id, out int stored))
                        {
                            try { b.SetStoredAmount(stored); }
                            catch { /* ignore */ }
                        }
                    }
                }

#if UNITY_EDITOR
                Debug.Log("SaveSystem: loaded save data.");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SaveSystem.Load failed: {ex.Message}");
            }
        }

        [Obsolete("Obsolete")]
        private void OnApplicationQuit()
        {
            Save();
        }

        [Obsolete("Obsolete")]
        private void OnApplicationPause(bool pause)
        {
            if (pause)
                Save();
        }

        private void OnDestroy()
        {
            if (_autoSaveRoutine != null)
                StopCoroutine(_autoSaveRoutine);
        }
    }

    [Serializable]
    public class SaveData
    {
        public List<string> collectedKeys = new List<string>();
        public List<int> collectedValues = new List<int>();

        public List<string> buildingIds = new List<string>();
        public List<int> buildingStored = new List<int>();

        public static SaveData FromDictionaries(Dictionary<string, int> collected, Dictionary<string, int> buildings)
        {
            var s = new SaveData();

            if (collected != null)
            {
                foreach (var kv in collected)
                {
                    s.collectedKeys.Add(kv.Key);
                    s.collectedValues.Add(kv.Value);
                }
            }

            if (buildings != null)
            {
                foreach (var kv in buildings)
                {
                    s.buildingIds.Add(kv.Key);
                    s.buildingStored.Add(kv.Value);
                }
            }

            return s;
        }

        public Dictionary<string, int> ToCollectedDictionary()
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i < Math.Min(collectedKeys.Count, collectedValues.Count); i++)
                d[collectedKeys[i]] = collectedValues[i];
            return d;
        }

        public Dictionary<string, int> ToBuildingsDictionary()
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i < Math.Min(buildingIds.Count, buildingStored.Count); i++)
                d[buildingIds[i]] = buildingStored[i];
            return d;
        }
    }
}
