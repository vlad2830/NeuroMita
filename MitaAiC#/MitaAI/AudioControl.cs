using MelonLoader;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


namespace MitaAI
{
    static class AudioControl
    {
        // Словарь для хранения музыкальных объектов
        private static Dictionary<string, GameObject> _audioObjects = new Dictionary<string, GameObject>();

        public static GameObject currentAudioObject;
        public static string getCurrrentMusic()
        {
            if (currentAudioObject == null) return "None";
            else return currentAudioObject.name;
        }
        // Метод для инициализации словаря
        public static void Init(Transform worldHouse)
        {
            // Добавляем объекты в словарь с обработкой исключений
            try
            {
                _audioObjects["Music 1"] = worldHouse.Find("Audio/Music 1").gameObject;
                currentAudioObject = _audioObjects["Music 1"];
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music 1': {e.Message}");
            }

            try
            {
                _audioObjects["Music BedClick"] = worldHouse.Find("Audio/MusicBedClick").gameObject;
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music BedClick': {e.Message}");
            }

            try
            {
                _audioObjects["Music 2"] = worldHouse.Find("Audio/Music 2").gameObject;
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music 2': {e.Message}");
            }

            try
            {
                _audioObjects["Music 3 Tamagochi"] = worldHouse.Find("Audio/Music 3 Tamagochi").gameObject;
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music 3 Tamagochi': {e.Message}");
            }

            MelonLogger.Msg($"_audioObjects count {_audioObjects.Count()}");
            // По умолчанию все объекты выключены
            //   foreach (var audioObject in _audioObjects.Values)
            //{
            //  audioObject.SetActive(false);
            //}
        }
        public static string ProcessMusic(string response)
        {
            MelonLogger.Msg($"Inside ProcessMusic");
            string pattern = @"<mu>(.*?)</mu>";
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    PlayAudio(match.Groups[1].Value);
                    break;
                }
            }

            string result = Regex.Replace(response, @"<mu>.*?</mu>", "");


            MelonLogger.Msg($"Inside ProcessMusic End");
            return result;

        }
        // Функция для включения объекта по имени и выключения остальных
        public static void PlayAudio(string audioName)
        {
            // Проверяем, существует ли объект с таким именем в словаре
            if (_audioObjects.ContainsKey(audioName))
            {

                currentAudioObject?.SetActive(false);
                currentAudioObject = null;
                // Включаем нужный объект
                if (audioName != "None")
                {
                    currentAudioObject = _audioObjects[audioName];
                    currentAudioObject.SetActive(true);
                }

            }
            else
            {
                MelonLogger.Msg($"Audio object with name '{audioName}' not found!");
            }
        }
    }
}