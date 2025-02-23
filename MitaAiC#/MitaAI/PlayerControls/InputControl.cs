using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using System.ComponentModel.Design;

namespace MitaAI.PlayerControls
{
    public static class InputControl
    {
        public static bool isInputBlocked = false; // Флаг для блокировки
        static GameObject InputFieldComponent;
        public static void processInpute()
        {
            // Обрабатываем нажатие Tab для переключения InputField
            if (Input.GetKeyDown(KeyCode.Tab)) // Используем GetKeyDown для одноразового срабатывания
            {
                if (InputFieldComponent == null)
                {
                    try
                    {
                        CreateInputComponent();
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Msg("CreateInputComponent ex:" + ex);
                    }
                }
                else
                {

                    if (isInputBlocked) return;

                    // Переключаем видимость InputField
                    bool isActive = InputFieldComponent.activeSelf;
                    //PlayerAnimationModded.playerMove.speed  
                    InputFieldComponent.SetActive(!isActive);

                    // Если объект стал активным, активируем InputField
                    if (InputFieldComponent.activeSelf)
                    {
                        var ifc = InputFieldComponent.GetComponent<InputField>();
                        if (ifc != null)
                        {
                            ifc.Select();
                            ifc.ActivateInputField();
                        }
                    }
                }
            }

            // Обрабатываем нажатие Enter для передачи текста в функцию
            else if (Input.GetKeyDown(KeyCode.Return) && checkInput())
            {
                var ifc = InputFieldComponent.GetComponent<InputField>();
                if (ifc.text != "")
                {
                    ProcessInput(ifc.text); // Пустышка для обработки текста
                    ifc.text = "";
                }
            }


            // Обрабатываем нажатие Enter для передачи текста в функцию
            else if (Input.GetKeyDown(KeyCode.C) && !checkInput())
            {
                PlayerAnimationModded.playerMove.canSit = true;

            }
            else if (Input.GetKeyUp(KeyCode.C))
            {
                PlayerAnimationModded.playerMove.canSit = false;
            }
            else if (Input.GetKeyDown(KeyCode.Space) && !checkInput())
            {
                try
                {
                    MelonLogger.Msg("Space pressed");
                    //if (PlayerAnimationModded.currentPlayerMovement == PlayerAnimationModded.PlayerMovement.sit) PlayerAnimationModded.stopAnim();
                    PlayerAnimationModded.currentPlayerMovement = PlayerAnimationModded.PlayerMovement.normal;
                }
                catch (Exception e)
                {

                    MelonLogger.Msg(e);
                }

            }

            // unstack 
            else if (Input.GetKeyDown(KeyCode.O) && (Input.GetKeyDown(KeyCode.P) ) )
           
            {
                try
                {
                    MelonLogger.Msg("Teleport mita to player");
                    MitaCore.Instance.Mita.MitaTeleport( MitaCore.Instance.playerObject.transform );
                    MitaCore.Instance.Mita.AiShraplyStop();
                }
                catch (Exception e)
                {

                    MelonLogger.Msg(e);
                }

            }
            // unstack player anim
            else if (Input.GetKeyDown(KeyCode.O) && (Input.GetKeyDown(KeyCode.L)))

            {
                try
                {
                    MelonLogger.Msg("Teleport player to 0 0 0");
                    PlayerAnimationModded.stopAnim();
                    MitaCore.Instance.playerObject.transform.position = Vector3.zero;
                   
                }
                catch (Exception e)
                {

                    MelonLogger.Msg(e);
                }

            }
            else if (Input.GetKeyDown(KeyCode.J))
            {
                changeMitaButtons();

            }

        }
        private static DateTime _lastChangeTime = DateTime.MinValue; // Время последнего изменения
        private static readonly TimeSpan _cooldown = TimeSpan.FromSeconds(5); // Задержка в 5 секунд

        static void changeMitaButtons()
        {
            try
            {
                MelonLogger.Msg("Try change Mita");
                // Проверяем, прошло ли 5 секунд с последнего изменения
                if (DateTime.Now - _lastChangeTime < _cooldown)
                {
                    return; // Если не прошло, выходим из метода
                }

                // Проверяем нажатие клавиш
                if (Input.GetKeyDown(KeyCode.I))
                {
                    MelonLogger.Msg("Try change to Kind");
                    MitaCore.Instance.changeMita(MitaCore.KindObject, MitaCore.character.Kind);
                    MitaCore.Instance.sendSystemMessage("Тебя только что заменили");
                    _lastChangeTime = DateTime.Now; // Обновляем время последнего изменения
                }
                else if (Input.GetKeyDown(KeyCode.K))
                {
                    MelonLogger.Msg("Try change to Cappy");
                    MitaCore.Instance.changeMita(MitaCore.CappyObject, MitaCore.character.Cappy);
                    MitaCore.Instance.sendSystemMessage("Тебя только что заменили");
                    _lastChangeTime = DateTime.Now;
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    MelonLogger.Msg("Try change to Crazy");
                    MitaCore.Instance.changeMita(MitaCore.Instance.MitaObject, MitaCore.character.Mita);
                    MitaCore.Instance.sendSystemMessage("Тебя только что заменили");
                    _lastChangeTime = DateTime.Now;
                }
                else if (Input.GetKeyDown(KeyCode.U))
                {
                    MelonLogger.Msg("Try change to ShortHair");
                    MitaCore.Instance.changeMita(MitaCore.ShortHairObject, MitaCore.character.ShortHair);
                    MitaCore.Instance.sendSystemMessage("Тебя только что заменили");
                    _lastChangeTime = DateTime.Now;
                }


            }
            catch (Exception e)
            {
                MelonLogger.Msg(e);
            }
        }


        static bool checkInput()
        {
            if (InputFieldComponent != null)
            {
                return InputFieldComponent.active;
            }
            return false;
        }

        public static void TurnBlockInputField(bool blocked)
        {
            isInputBlocked = blocked; // Устанавливаем блокировку
            if (InputFieldComponent != null)
            {
                InputFieldComponent.SetActive(!blocked); // Отключаем поле ввода, если оно активно
            }

        }

        private static void CreateInputComponent()
        {


            // Создаем объект InputField
            InputFieldComponent = new GameObject("InputFieldComponent");

            var ifc = InputFieldComponent.AddComponent<InputField>();
            var _interface = GameObject.Find("Interface");
            if (_interface == null) return;

            InputFieldComponent.transform.parent = _interface.transform;


            var rect = InputFieldComponent.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);



            var image = InputFieldComponent.AddComponent<UnityEngine.UI.Image>();
            /*            try
                        {
                            var KeyRun = _interface.transform.Find("GameController/Interface/SubtitlesFrame/Text 2").GetComponent<UnityEngine.UI.Image>();
                            MelonLogger.Msg("KeyRun");
                            image.sprite = KeyRun.sprite;
                        }
                        catch (Exception ex)
                        {*/
            Sprite blackSprite = CreateBlackSprite(100, 100);
            image.sprite = blackSprite;
            //}

            image.color = new Color(0f, 0f, 0f, 0.7f);
            ifc.image = image;


            var TextLegacy = new GameObject("TextLegacy");
            var textComponent = TextLegacy.AddComponent<Text>();
            TextLegacy.transform.parent = InputFieldComponent.transform;
            var rectText = TextLegacy.GetComponent<RectTransform>();
            rectText.sizeDelta = new Vector2(500, 100);
            rectText.anchoredPosition = Vector2.zero;
            var texts = GameObject.FindObjectsOfType<Text>();



            foreach (var text in texts)
            {
                textComponent.font = text.font;
                textComponent.fontStyle = text.fontStyle;
                textComponent.fontSize = 35;
                if (textComponent.font != null) break;
            }


            var textInputField = InputFieldComponent.GetComponent<InputField>();
            textInputField.textComponent = TextLegacy.GetComponent<Text>();
            textInputField.text = "Введи текст";
            textInputField.textComponent.color = Color.yellow;
            textInputField.textComponent.alignment = TextAnchor.MiddleCenter;



            // Устанавливаем 70% ширины от родителя
            RectTransform parentRect = _interface.GetComponent<RectTransform>();
            float parentWidth = parentRect.rect.width;
            rect.sizeDelta = new Vector2(parentWidth * 0.7f, rect.sizeDelta.y);
            rectText.sizeDelta = rect.sizeDelta;
            textInputField.Select();
            textInputField.ActivateInputField();

        }

        // Пустышка для обработки ввода
        private static void ProcessInput(string inputText)
        {
            MelonLogger.Msg("Input received: " + inputText);
            MelonCoroutines.Start(MitaCore.Instance.PlayerTalk(inputText));
            MitaCore.Instance.playerMessage += $"{inputText}\n";

        }

        public static Sprite CreateBlackSprite(int width, int height)
        {
            // Создаем текстуру с заданными размерами
            Texture2D texture = new Texture2D(width, height);

            // Задаем все пиксели как черные
            Color darkColor = new Color(0f, 0f, 0f, 0f);  // Черный цвет
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, darkColor);
                }
            }

            // Применяем изменения
            texture.Apply();

            // Создаем и возвращаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }


    }
}
