using MelonLoader;
using MitaAI.Mita;
using MitaAI.PlayerControls;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI
{
    public static class DataChange
    {
        private const float MitaBoringInterval = 120f;
        public static float MitaBoringtimer = 0f;
        
        public static Dictionary<int, string> sound_files = new Dictionary<int, string>();

        static private float lastActionTime = -Mathf.Infinity;  // Для отслеживания времени последнего действия
        private const float actionCooldown = 6f;  // Интервал в секундах, надо сделать умнее для нормальных диалогов

        public static IEnumerator HandleDialogue()
        {
            //MelonLogger.Msg("HandleDialogue");

            string playerText = MitaCore.Instance.playerMessage;
            MitaCore.Instance.playerMessage = "";
            bool senfPlayerMessage = false;

            string dataToSent = "waiting";
            string dataToSentSystem = "-";
            string info = "-";
            MitaCore.character characterToWas = MitaCore.character.None;
            MitaCore.character characterToSend = MitaCore.Instance.currentCharacter;
            List<MitaCore.character> Characters = MitaCore.Instance.playerMessageCharacters;


            float currentTime = Time.unscaledTime;
            if (currentTime - lastActionTime > actionCooldown - ( CharacterControl.needToIgnoreTimeout() ? 4 : 0 ) )
            {
                //MelonLogger.Msg("Ready to send");
                if (playerText != "")
                {
                    MelonLogger.Msg("HAS playerMessage");
                    senfPlayerMessage = true;

                    if (Characters.Count > 0)
                    {
                        if (Characters.First().ToString().Contains("Cart"))
                        {
                            characterToSend = Characters.First();
                        }
                        else
                        {
                            MitaBoringtimer = 0f;
                        }


                        MitaCore.Instance.sendInfoListeners(playerText, Characters, characterToSend, null);

                    }


                    dataToSent = playerText;

                    lastActionTime = Time.unscaledTime;
                }
                else if (MitaCore.Instance.systemMessages.Count > 0)
                {
                    MelonLogger.Msg("HAS SYSTEM MESSAGES");
                    MitaBoringtimer = 0f;


                    //Отправляю залпом.
                    while (MitaCore.Instance.systemMessages.Count() > 0)
                    {
                        var message = MitaCore.Instance.systemMessages.Dequeue();
                        dataToSentSystem += message.Item1 + "\n";
                        characterToSend = message.Item2;
                        if (characterToWas == MitaCore.character.None || characterToWas == characterToSend)
                        {
                            characterToWas = message.Item2;
                        }
                        else
                        {
                            MitaCore.Instance.sendSystemMessage(message.Item1, characterToSend);
                            break;
                        }

                    }


                    lastActionTime = Time.unscaledTime;

                }
                else if (MitaBoringtimer >= MitaBoringInterval && MitaCore.Instance.mitaState == MitaCore.MitaState.normal)
                {
                    MitaBoringtimer = 0f;
                    dataToSentSystem = "Player did nothing for 90 seconds";
                    lastActionTime = Time.unscaledTime;
                }
            }



            string response = "";

            if (MitaCore.Instance.systemInfos.Count > 0)
            {
                //MelonLogger.Msg("HAS SYSTEM INFOS");
                //Отправляю залпом.
                while (MitaCore.Instance.systemInfos.Count() > 0)
                {
                    var message = MitaCore.Instance.systemInfos.Dequeue();
                    MitaCore.character ch = message.Item2;

                    if (ch == characterToSend)
                    {
                        info += message.Item1 + "\n";
                    }
                    else
                    {
                        MitaCore.Instance.sendSystemInfo(message.Item1, ch);
                        break;
                    }
                }

            }
            if (characterToSend != MitaCore.Instance.currentCharacter)
            {
                if (characterToSend != MitaCore.character.GameMaster)
                {
                    MitaCore.Instance.addChangeMita(MitaCore.getMitaByEnum(characterToSend), characterToSend, false, false, false, false);
                }
                else
                {
                    MitaCore.Instance.currentCharacter = characterToSend;
                }
            }

            if (dataToSent != "waiting" || dataToSentSystem != "-") MitaCore.Instance.prepareForSend();


            Task<Dictionary<string, JsonElement>> responseTask = NetworkController.GetResponseFromPythonSocketAsync(dataToSent, dataToSentSystem, info, characterToSend);



            float timeout = 40f;     // Лимит времени ожидания
            float waitMessageTimer = 0.5f;
            float elapsedTime = 0f; // Счетчик времени
            float lastCallTime = 0f; // Время последнего вызова


            while (!responseTask.IsCompleted)
            {
                elapsedTime += 0.1f;
                if (elapsedTime >= timeout)
                {
                    MelonLogger.Msg("Too long waiting for text");
                    break;
                }

                //MelonLogger.Msg($"!responseTask.IsCompleted{elapsedTime}/{timeout}");
                if (elapsedTime - lastCallTime >= waitMessageTimer && !DialogueControl.dialogActive)
                {
                    try
                    {
                        List<String> parts = new List<String> { "..." };
                        MelonCoroutines.Start(DialogueControl.ShowDialoguesSequentially(characterToSend,parts, true));
                        lastCallTime = elapsedTime; // Обновляем время последнего вызова
                    }
                    catch (Exception ex)
                    {

                        MelonLogger.Msg(ex);
                    }

                }
                yield return new WaitForSecondsRealtime(0.1f);
            }

            string patch = null;
            bool GM_ON = false;
            bool GM_READ = false;
            bool GM_VOICE = false;
            int id = 0;
            if (responseTask.IsCompleted)
            {
                Dictionary<string, JsonElement> messageData2 = responseTask.Result;
                try
                {

                    id = messageData2["id"].GetInt32();
                    string type = messageData2["type"].GetString();

                    string new_character = messageData2["character"].GetString();
                    response = messageData2["response"].GetString();
                    bool connectedToSilero = messageData2["silero"].GetBoolean();

                    int idSound = messageData2["id_sound"].GetInt32();
                    //int idSound_cp = messageData2.ContainsKey("id_sound_cp") ? messageData2["id_sound_cp"].GetInt32() : 0;

                    patch = messageData2.ContainsKey("patch_to_sound_file") ? messageData2["patch_to_sound_file"].GetString() : "";
                    string user_input = messageData2.ContainsKey("user_input") ? messageData2["user_input"].GetString() : "";

                    GM_ON = messageData2.ContainsKey("GM_ON") ? messageData2["GM_ON"].GetBoolean() : false;
                    GM_READ = messageData2.ContainsKey("GM_READ") ? messageData2["GM_READ"].GetBoolean() : false;
                    GM_VOICE = messageData2.ContainsKey("GM_VOICE") ? messageData2["GM_VOICE"].GetBoolean() : false;
                    int GM_REPEAT = messageData2.ContainsKey("GM_REPEAT") ? messageData2["GM_REPEAT"].GetInt32() : 2;

                    int limitmod = messageData2.ContainsKey("CC_Limit_mod") ? messageData2["CC_Limit_mod"].GetInt32() : 100;

                    InputControl.instantSend = messageData2.ContainsKey("instant_send") ? messageData2["instant_send"].GetBoolean() : false;


                    if (!string.IsNullOrEmpty(patch))
                    {

                        sound_files[idSound] = patch;

                            
                    }

                    if (CharacterControl.gameMaster != null)
                    {
                        CharacterControl.gameMaster.timingEach = GM_REPEAT;
                        CharacterControl.gameMaster.enabled = GM_ON;


                    }
                    NetworkController.connectedToSilero = connectedToSilero;
                    CharacterControl.limitMod = limitmod;


                    InputControl.UpdateInput(user_input);



                }
                catch (Exception ex)
                {

                    MelonLogger.Error(ex);
                }

            }
            else
            {
                response = "Too long waited for text from python";
                NetworkController.connectedToSilero = false;
            }




            if (response != "")
            {
                MelonLogger.Msg($"after GetResponseFromPythonSocketAsync char {characterToSend} {GM_READ} {GM_VOICE}");

                if (characterToSend.ToString().Contains("Cart")) MelonCoroutines.Start(DialogueControl.DisplayResponseAndEmotionCoroutine(id, characterToSend, response, AudioControl.cartAudioSource));
                else if (characterToSend == MitaCore.character.GameMaster)
                {



                    if (GM_READ) MelonCoroutines.Start(DialogueControl.DisplayResponseAndEmotionCoroutine(id, characterToSend, response, AudioControl.playerAudioSource, GM_VOICE));
                    else CommandProcessor.ProcessCommands(CommandProcessor.ExtractCommands(response).Item1);
                }
                else MelonCoroutines.Start(DialogueControl.DisplayResponseAndEmotionCoroutine(id, characterToSend,response));

                if (characterToSend != MitaCore.character.GameMaster) MitaCore.Instance.sendInfoListeners(Utils.CleanFromTags(response), Characters, characterToSend, CharacterControl.extendCharsString(characterToSend));
                else MitaCore.Instance.sendInfoListenersFromGm(Utils.CleanFromTags(response), Characters, characterToSend);


                //Тестово - хочешь чтобы было без лишнего отрубай это

                MelonCoroutines.Start(testNextAswer(response, characterToSend, senfPlayerMessage));



            }

            static IEnumerator testNextAswer(string response, MitaCore.character currentCharacter, bool fromPlayer)
            {
                yield return new WaitForSeconds(0.25f);
                while (DialogueControl.dialogActive)
                {
                    yield return null;
                }

                CharacterControl.nextAnswer(Utils.CleanFromTags(response), currentCharacter, fromPlayer);
            }

        }

    }
}
