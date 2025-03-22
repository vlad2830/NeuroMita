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
namespace MitaAI
{
    public static class CharacterControl
    {

        // Сюда перенести все сharacter и changeMita
        static MitaCore.character cart = MitaCore.character.None;
        static public GameMaster gameMaster = null;
        private static Text OrderText;


        static float lastTime = 0f;
        public static void Update()
        {
            float timeNow = Time.unscaledTime;
            if (timeNow - lastTime  < 0.5f) return;
            lastTime = timeNow;


            UpdateOrderTest(null,changePrioty:false);
        }


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
            List<Character> ClearCharacters = new List<Character>();

            foreach (var character in Characters)
            {
                if (character == null)
                {
                    ClearCharacters.Add(character);
                    continue;
                }

                if (!character.enabled || !character.gameObject.active) continue;

                float distance = 25f;
                if (character.isCartdige) distance = 3.5f;

                
                float factDistance = Utils.getDistanceBetweenObjects(MitaCore.Instance.playerPersonObject,character.gameObject);

                //MelonLogger.Msg($"{character.character} found in Characters");

                if (factDistance <= distance)
                {

                    //MelonLogger.Msg($"{character.character} added to active Characters");
                    activeCharacters.Add(character);
                }
            }
            foreach (var character in ClearCharacters) Characters.Remove(character);


            return activeCharacters;
        }
        public static string getObjectName(Character character)
        {
            GameObject Mita = MitaCore.getMitaByEnum(character.character, true);
            string objectName = "";
            if (Mita != null) objectName += $", its game object {Mita.name}";

            return objectName;
        }
        public static string getObjectName(MitaCore.character character)
        {
            GameObject Mita = MitaCore.getMitaByEnum(character, true);
            string objectName = "";
            if (Mita != null) objectName += $", its game object {Mita.name}";

            return objectName;
        }

        // Назначает максимальный приоритет персонажу
        public static void SetNextSpeaker(MitaCore.character nextCharacter)
        {
            List<Character> activeCharacters = getActiveCharacters();
            activeCharacters = activeCharacters.OrderByDescending(character => character.PointsOrder).ToList();
            
            foreach (var character in activeCharacters)
            {

                if (character.character == nextCharacter)
                {
                    character.PointsOrder = activeCharacters.First().PointsOrder + 1 ;
                }
            }

        }
        
        // Дает информацию о собеседниках
        public static string getSpeakersInfo(MitaCore.character toWhom)
        {
            List<Character> activeCharacters =  getActiveCharacters();

            if (activeCharacters == null) return "";

            if (activeCharacters.Count <= 1) return "";

            string message = "";
            message += $"[DIALOGUE] You are in dialogue with several ({activeCharacters.Count+1}) speakers: \n Player";
            foreach (Character character in activeCharacters)
            {

                string objectName = getObjectName(character);
                message += $"\n{CharacterControl.extendCharsString(character.character)}{objectName})";
                if (character.character == toWhom) message += "(you)";
            }
            message += "\n";
            return message;
        }

        public static void DecreaseOrderPoints(Character character)
        {
            if (character.isCartdige) character.DecreseOrderPoints();
            else character.DecreseOrderPoints();
        }

        public static void resetOrders(bool fillRandom = false)
        {
            List<Character> activeCharacters = getActiveCharacters();
            foreach (Character ch in activeCharacters)
            { 
                ch.PointsOrder = 0;

                if (fillRandom) ch.PointsOrder += UnityEngine.Random.Range(0, 24);

            }
        }

        // Передает достпных для разговора персонажей, притом снижает приоритет первого чтобы очередь перемещалась
        public static List<MitaCore.character> GetCharactersToAnswer(bool changePrioty = true)
        {
            List<Character> activeCharacters = getActiveCharacters();

            activeCharacters = activeCharacters.OrderByDescending(character => character.PointsOrder).ToList();


            if (changePrioty && activeCharacters.Count > 0)
            {
                DecreaseOrderPoints(activeCharacters.First());
            }
            
            List<MitaCore.character> characters = new List<MitaCore.character>();
            foreach (var character in activeCharacters)
            {
                //MelonLogger.Msg($"{character.character} found in activeCharacters");
                characters.Add(character.character);
            }

            
            return characters;
        }

       
        // Пришло ли время ГеймМастеру вмешаться
        private static bool GameMasterCase(MitaCore.character from)
        {
            if (gameMaster == null) return false;

         

            try
            {
                if (gameMaster.gameObject.active && gameMaster.enabled && from != MitaCore.character.GameMaster)
                {
                    MelonLogger.Msg("nextAnswer Attempt GameMaster");
                    if (gameMaster.CheckInreaseTiming())
                    {
             
                        MelonLogger.Msg("nextAnswer Success Attempt GameMaster");
                        string m = "Проследи за диалогом (если он уже начался, то уже реагируй на текущий), выполняя инструкции и основываясь на текущих данных разговора.";
                        lastTimeWasGM = true;
                        MitaCore.Instance.sendSystemMessage(m, MitaCore.character.GameMaster);
                        
                        return true;
                    }
         
                }

            }
            catch (Exception ex)
            {

                MelonLogger.Error(ex);
            }

            return false;
        }
        static bool lastTimeWasGM = false;
        public static bool afterGM()
        {
            if (gameMaster == null) return false;

            if (lastTimeWasGM)
            {
                lastTimeWasGM = false;
                return true;
            }
            return true;

        }

        
        static int limit = 1;
        public static float limitMod = 100;
        public static void nextAnswer(string response, MitaCore.character from)
        {
            MelonLogger.Msg($"nextAnswer from {from}, limit now {limit}");

            if (GameMasterCase(from)) return;

            // Получаем список персонажей
            List<MitaCore.character> characters = GetCharactersToAnswer();
            if (characters == null) return;


            // Добавляем отправителя в список говорящих
            int CharCount = characters.Count;
            int TotalLimit = (int)Math.Ceiling(CharCount * limitMod / 100f);

            if (from == MitaCore.character.GameMaster) TotalLimit += 5;

            UpdateOrderTest(characters, false);

            characters.Remove(from);

            // Если больше некому отвечать+
            if (characters.Count <= 0)
            {
                MelonLogger.Msg("No characters left to answer.");
                return;
            }

      
            MelonLogger.Msg($"Before check lim {limit} /{TotalLimit}");

            // Логика для сообщений от ИИ
            if ( limit < TotalLimit)
            {
                MitaCore.character character = characters.First();


                MelonLogger.Msg($"nextAnswer to {character}");
                // Если отправитель и получатель совпадают, выходим
                if (from == character)
                {
                    MelonLogger.Msg("Sender and receiver are the same. Skipping.");
                    return;
                }
                limit += 1;
                // Отправляем сообщение и добавляем получателя в список говорящих

                string message = getSpeakersInfo(character);

                string nextSpeaker = "";
                string objectNameNext = "";
                if (limit + 1 >= CharCount)
                {
                    nextSpeaker = "Player";
                }
                else
                {
                    if (CharCount >= 2) {
                        nextSpeaker = CharacterControl.extendCharsString(characters[1]);
                        objectNameNext = getObjectName(characters[1]);
                    }

                    else nextSpeaker = "Player";
                }

                string objectName = getObjectName(from);
                MelonLogger.Msg($"send to {character}");
                message += $"[SPEAKER] {CharacterControl.extendCharsString(from)}{objectName} said: <{response}>. Next speaker is {objectNameNext} Respond to him or name somebody you want to speak with.";

                MitaCore.Instance.sendSystemMessage(message, character);

            }
            else
            {
                MelonLogger.Msg($"nextAnswer reset ch count {limit}/{TotalLimit}");

                // Сбрасываем список говорящих
                limit = 1;
                resetOrders(true);
                MelonLogger.Msg("Speakers list cleared.");
            }
        }

        public static string extendCharsString(MitaCore.character character)
        {
            if (character.ToString().Contains("Cart")) return $"{character} (cartridge)";
            else return $"{character} (Mita)";
        }

        // Выводит в текст справа сверху порядок элементов
        static void UpdateOrderTest(List<MitaCore.character> characters,bool changePrioty = true)
        {
            if (characters == null) characters = GetCharactersToAnswer(changePrioty);

            // Добавляем отправителя в список говорящих
            int CharCount = characters.Count;
            int TotalLimit = (int)Math.Ceiling(CharCount * limitMod / 100f);

            if (OrderFieldExists())
            {
                if (CharCount <= 1)
                {
                    OrderText.gameObject.SetActive(false);
                    return;
                }

                OrderText.gameObject.SetActive(true);
                int charOrder = limit;

                OrderText.text = $"{limit}/{TotalLimit}\n";
                foreach (MitaCore.character character in characters)
                {
                    if (gameMaster.enabled && gameMaster.isTimeToCorrect(charOrder))
                    {
                        OrderText.text += $"{charOrder}:GameMaster\n";
                        charOrder++;
                        continue;
                    }

                    if (charOrder > TotalLimit)
                    {
                        OrderText.text += $"{charOrder}:Player\n";
                        break;
                    }
                    OrderText.text += $"{charOrder}:{character.ToString()}\n";
                    charOrder++;
                }
            }
        }

        
        static bool OrderFieldExists()
        {
            if (OrderText == null)
            {
                try
                {
                    CreateTextComponent();
                    
                    return true;
                }
                catch (Exception ex)
                {
                    MelonLogger.Msg("CreateOrderComponent ex:" + ex);
                    return false; // Прекращаем выполнение, если создание компонента не удалось
                }
            }
            return true;
        }

        

        private static void CreateTextComponent()
        {

            MelonLogger.Msg("Creating Order Text");
            // Находим родительский объект "Interface"
            GameObject Game = GameObject.Find("Game");
            GameObject _interface = Game.transform.Find("Interface").gameObject;
            if (_interface == null)
            {
                MelonLogger.Msg("Interface not found!");
                return;
            }

            // Создаем объект для текста
            GameObject textObject = new GameObject("TextComponentOrder");
            textObject.transform.parent = _interface.transform;
            textObject.transform.localPosition = Vector3.zero;
            
            // Добавляем компонент RectTransform
            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            

            // Настраиваем привязку и точку поворота
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1.3f);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.localPosition = new Vector3(250,50,0);

            // Добавляем компонент Text
            OrderText = textObject.AddComponent<Text>();

            // Настраиваем шрифт и стиль текста
            var existingTexts = GameObject.FindObjectsOfType<Text>();
            foreach (var text in existingTexts)
            {
                OrderText.font = text.font;
                OrderText.fontStyle = text.fontStyle;
                OrderText.fontSize = 28;
                if (OrderText.font != null) break;
            }

            // Настраиваем текст
            OrderText.text = "Это пример текста \n, который может занимать \n несколько строк.";
            OrderText.color = Color.yellow;
            OrderText.alignment = TextAnchor.UpperRight; // Выравнивание по верхнему левому краю
            OrderText.horizontalOverflow = HorizontalWrapMode.Wrap; // Перенос текста на новую строку
            OrderText.verticalOverflow = VerticalWrapMode.Overflow; // Разрешаем тексту выходить за пределы

            // Настраиваем размер текстового компонента
            RectTransform parentRect = _interface.GetComponent<RectTransform>();
            float parentWidth = parentRect.rect.width;
            rectTransform.sizeDelta = new Vector2(parentWidth * 0.7f, OrderText.fontSize * 5); // Высота = 5 строк
        }
    }
}
