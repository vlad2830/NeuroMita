using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using static Il2CppRootMotion.FinalIK.InteractionObject;

using UnityEngine.UI;
using UnityEngine.Device;
namespace MitaAI
{
    
    public static class CharacterMessages
    {
        public static Queue<(string, character)> systemMessages = new Queue<(string, character)>();
        public static Queue<(string, character)> systemInfos = new Queue<(string, character)>();

        public static void sendSystem(string m, bool info, character character = character.None)
        {
            if (info) sendSystemInfo(m, character);
            else sendSystemMessage(m, character);

        }
        public static void sendSystemMessage(string m, character character = character.None)
        {
            if (character == character.None) character = MitaCore.Instance.currentCharacter;
            systemMessages.Enqueue((m, character));

            EventsModded.registerLastEvent();
        }
        public static void sendSystemInfo(string m, character character = character.None)
        {

            if (character == character.None) character = MitaCore.Instance.currentCharacter;
            systemInfos.Enqueue((m, character));
        }

        public static void sendInfoListeners(string message, List<character> characters = null, character exluding = character.None, string from = "Игрок")
        {
            MelonLogger.Msg($"sendInfoListeners char {characters} exl {exluding} from {from}");

            if (characters == null) characters = CharacterControl.GetCharactersToAnswer();

            if (exluding == character.None) exluding = MitaCore.Instance.currentCharacter;


            string charName = CharacterControl.extendCharsString(exluding);

            if (CharacterControl.gameMaster != null) characters.Add(character.GameMaster);
            //characters.Remove(exluding);

            foreach (character character in characters)
            {
                string speakersText = CharacterControl.getSpeakersInfo(character);

                if (character != exluding)
                {
                    string messageToListener = "";
                    messageToListener += speakersText;

                    messageToListener += $"[SPEAKER] : {from} said: {message} and was answered by {charName}";
                    sendSystemInfo(messageToListener, character);
                }
            }


        }
        public static void sendInfoListenersFromGm(string message, List<character> characters = null, character exluding = character.None)
        {
            if (characters == null) characters = CharacterControl.GetCharactersToAnswer();

            if (exluding == character.None) exluding = MitaCore.Instance.currentCharacter;
            character from = character.GameMaster;

            string charName = CharacterControl.extendCharsString(exluding);




            foreach (character character in characters)
            {
                string speakersText = CharacterControl.getSpeakersInfo(character);

                if (character != exluding)
                {
                    string messageToListener = "";
                    messageToListener += speakersText;

                    messageToListener += $"[GAME_MASTER] : {from} said: {message}";


                    CharacterMessages.sendSystemInfo(messageToListener, character);
                }
            }
        }
    }
}
