using MelonLoader;
using System.IO;
using System.Collections;
using UnityEngine;
using MelonLoader.Utils;

namespace MitaAI
{
    [RegisterTypeInIl2Cpp]
    public class Settings : MonoBehaviour
    {
        private string configPath;
        private Hashtable settings;

        //Settings(string configPath, Hashtable settings)
        //{
          //  Start();
        //}

        void Start()
        {
            string configPath = Path.Combine(MelonEnvironment.ModsDirectory, "NeuroMita", "settings.json");

            // Проверяем, существует ли директория
            string directoryPath = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(directoryPath))
            {
                // Создаем директорию, если она не существует
                Directory.CreateDirectory(directoryPath);
                MelonLogger.Msg($"directory created: {directoryPath}");
            }

            // Проверяем, существует ли файл
            if (!File.Exists(configPath))
            {
                // Создаем файл, если он не существует
                File.WriteAllText(configPath, "{}"); // Создаем пустой JSON-файл
                MelonLogger.Msg($"File created: {configPath}");
            }
            else
            {
                MelonLogger.Msg($"File already created: {configPath}");
            }

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