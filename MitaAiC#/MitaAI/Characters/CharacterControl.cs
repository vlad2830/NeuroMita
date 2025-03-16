using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MitaAI
{
    public static class CharacterControl
    {

        // Сюда перенести все сharacter и changeMita
        static MitaCore.character cart = MitaCore.character.None;

        public static MitaCore.character get_cart()
        {
            if (cart == MitaCore.character.None) init_cart();
            return cart;
        }

        public static void init_cart()
        {
            if (Utils.Random(1, 2)) cart = MitaCore.character.Cart_portal;
            else cart = MitaCore.character.Cart_divan;
        }


        public static HashSet<Character> Characters = new HashSet<Character>();
        
        private static List<Character> getActiveCharacters()
        {
            List<Character> activeCharacters = new List<Character>();
            foreach (var character in Characters)
            {
                
                if (!character.enabled) continue;

                float distance = 120f;
                if (character.isCartdige) distance = 1.5f;

                
                float factDistance = Utils.getDistanceBetweenObjects(MitaCore.Instance.playerPersonObject,character.gameObject);

                MelonLogger.Msg($"{character.character} found in Characters");

                if (factDistance <= distance)
                {

                    MelonLogger.Msg($"{character.character} added to actibe Characters");
                    activeCharacters.Add(character);
                }
            }

            return activeCharacters;
        }

        public static string getSpeakersInfo(MitaCore.character toWhom)
        {
            if (Characters == null) return "";

            if (Characters.Count <= 2) return "";

            string message = "";
            message += $"[DIALOGUE] You are in dialogue with several ({Characters.Count+1}) speakers: \n player";
            foreach (Character character in Characters)
            {
                message += $"\n{CharacterControl.extendCharsString(character.character)}";
                if (character.character == toWhom) message += "(you)";
            }
            message += "\n";
            return message;
        }

        public static void increaseOrderPoints(Character character)
        {
            if (character.isCartdige) character.increaseOrderPoints(2);
            else character.increaseOrderPoints();
        }

        public static void resetOrders(bool fillRandom = false)
        {
            foreach (Character ch in Characters)
            { 
                ch.PointsOrder = 0;

                if (fillRandom) ch.PointsOrder += UnityEngine.Random.Range(0, 25);

            }
        }

        public static List<MitaCore.character> GetCharactersToAnswer()
        {
            List<Character> activeCharacters = getActiveCharacters();

            activeCharacters = activeCharacters.OrderByDescending(character => character.PointsOrder).ToList();


            if (activeCharacters.Count > 0)
            {
                increaseOrderPoints(activeCharacters.First());
            }
            
            List<MitaCore.character> characters = new List<MitaCore.character>();
            foreach (var character in activeCharacters)
            {
                MelonLogger.Msg($"{character.character} found in activeCharacters");
                characters.Add(character.character);
            }

            

            
            return characters;
        }



        static HashSet<MitaCore.character> speakersWere = new HashSet<MitaCore.character>();
        static int limit = 0;

        public static void nextAnswer(string response, MitaCore.character from, bool lastMessageWasFromAi)
        {

            // Получаем список персонажей
            List<MitaCore.character> characters = GetCharactersToAnswer();
            if (characters == null) return;

            // Добавляем отправителя в список говорящих
            speakersWere.Add(from);
            characters.Remove(from);
            // Удаляем из characters всех, кто уже говорил

            // Если больше некому отвечать
            if (characters.Count <= 0)
            {
                MelonLogger.Msg("No characters left to answer.");
                return;
            }

            // Логика для сообщений от ИИ
            if (lastMessageWasFromAi && limit < Characters.Count)
            {
                MitaCore.character character = characters.First();

                // Если отправитель и получатель совпадают, выходим
                if (from == character)
                {
                    MelonLogger.Msg("Sender and receiver are the same. Skipping.");
                    return;
                }
                limit += 100;
                // Отправляем сообщение и добавляем получателя в список говорящих

                string message = getSpeakersInfo(character);

                string nextSpeaker = "";
                if (limit + 1 == Characters.Count)
                {
                    nextSpeaker = "Player";
                }
                else
                {
                    if (characters.Count >= 2) nextSpeaker = CharacterControl.extendCharsString(characters[1]);
                    else nextSpeaker = "Player";
                }
                

                message += $"[SPEAKER] {CharacterControl.extendCharsString(from)} said: <{response}>. Next speaker is {nextSpeaker} Respond to him or name somebody you want to speak with.";

                MitaCore.Instance.sendSystemMessage(message, character);
                speakersWere.Add(character);
            }
            else
            {
                // Сбрасываем список говорящих
                limit = 1;
                speakersWere.Clear();
                resetOrders(true);
                MelonLogger.Msg("Speakers list cleared.");
            }
        }

        public static string extendCharsString(MitaCore.character character)
        {
            if (character.ToString().Contains("Cart")) return $"{character} (cartridge)";
            else return $"{character} (Mita)";
        }
    }
}
