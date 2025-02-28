using MelonLoader;
using System.IO;
using System.Collections;
using UnityEngine;

namespace MitaAI
{
    [RegisterTypeInIl2Cpp]
    public class Settings : MonoBehaviour
    {
        private string configPath;
        private Hashtable settings;

        void Start()
        {
            configPath = Path.Combine(Application.persistentDataPath, "settings.json");
            settings = new Hashtable();
            LoadSettings();
        }

        void LoadSettings()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    settings = JsonUtility.FromJson<Hashtable>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading settings: {e}");
                    settings = new Hashtable();
                }
            }
        }

        void SaveSettings()
        {
            try
            {
                //Il2CppSystem.Object ob  = settings;
                Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object> dict =
                new Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object>();

                foreach (DictionaryEntry entry in settings)
                {
                    dict.Add(entry.Key.ToString(), (Il2CppSystem.Object)entry.Value);
                }

                string json = JsonUtility.ToJson(dict);
                File.WriteAllText(configPath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving settings: {e}");
            }
        }

        public object Get(string key, object defaultValue = null)
        {
            return settings.ContainsKey(key) ? settings[key] : defaultValue;
        }

        public void Set(string key, object value)
        {
            settings[key] = value;
            SaveSettings();
        }
    }
}