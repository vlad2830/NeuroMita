using Il2Cpp;
using MelonLoader;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;


namespace MitaAI.Mita
{
    static class AudioControl
    {
        // Словарь для хранения музыкальных объектов
        private static Dictionary<string, GameObject> _audioObjects = new Dictionary<string, GameObject>();

        public static GameObject currentAudioObject;

        static DataValues_Sounds dataValues_Sounds;
        public static AudioClip chibiMitaAudio;
        static AudioSource mitaAudioSourse;
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
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music 1': {e.Message}");
            }

            try
            {
                _audioObjects["Music BedClick"] = worldHouse.Find("Audio/MusicBedClick").gameObject;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music BedClick': {e.Message}");
            }

            try
            {
                _audioObjects["Music 2"] = worldHouse.Find("Audio/Music 2").gameObject;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music 2': {e.Message}");
            }

            try
            {
                _audioObjects["Music 3 Tamagochi"] = worldHouse.Find("Audio/Music 3 Tamagochi").gameObject;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to find or add 'Music 3 Tamagochi': {e.Message}");
            }

            MelonLogger.Msg($"_audioObjects count {_audioObjects.Count()}");


            try
            {
                dataValues_Sounds =  worldHouse.Find("Dialogues/DialogueMita Speak").GetComponent<DataValues_Sounds>();
                chibiMitaAudio = dataValues_Sounds.sounds[0];
                mitaAudioSourse = MitaCore.Instance.MitaObject.transform.Find("MitaPerson Mita/Armature/Hips/Spine").GetComponent<AudioSource>();
            }
            catch (Exception e)
            {

                MelonLogger.Error($"Failed to find or add 'Dialogues/DialogueMita Speak': {e.Message}");
            }

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

        public static IEnumerator PlayTextAudio(string text)
        {
            MelonLogger.Msg("Chibi Sound Play");
            for (int i = 0; i < text.Length; i = i + 3)
            {
                GameObject currentDialog = MitaCore.Instance.InstantiateDialog();
                if (text[i] != ' ')
                {
                    
                    currentDialog.SetActive(true);
                    Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                    
                    answer.timeSound = AudioControl.chibiMitaAudio.length * UnityEngine.Random.Range(60, 141) * 0.01f;

                    answer.speaker = MitaCore.Instance.Mita?.gameObject;
                    //AudioControl.chibiMitaAudio.SetSpeed(1 * (UnityEngine.Random.Range(1, 40)*0.01) );
                    answer.LoadVoice(AudioControl.chibiMitaAudio);

                    yield return new WaitForSecondsRealtime(0.07f);
                }
                else
                {
                    yield return new WaitForSecondsRealtime(0.14f);
                }
                
                GameObject.Destroy(currentDialog);
            }
        }
    }
}