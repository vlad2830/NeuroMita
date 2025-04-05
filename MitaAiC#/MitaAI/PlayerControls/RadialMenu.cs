using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Il2CppSystem.Collections.Generic; // Добавляем Il2Cpp-коллекции

namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class RadialMenu : MonoBehaviour
    {
        public Canvas radialCanvas;
        public GameObject itemPrefab;
        public float radius = 250f;
        public float selectionThreshold = 200f; // Макс. расстояние для выбора
        public System.Collections.Generic.List<string> items = new System.Collections.Generic.List<string> { "Удар", "Обнять", "Что" };
        public System.Collections.Generic.List<string> descriptions = new System.Collections.Generic.List<string> { "Нанести удар", "Обнять персонажа", "Спросить что-то" };

        private Vector2 center;
        private bool Showed = false;
        private string selectedItem;
        private System.Collections.Generic.List<GameObject> menuItems = new System.Collections.Generic.List<GameObject>();
        private int lastSelectedIndex = -1;

        private void Start()
        {
            // Создаем префаб если он не задан
            if (itemPrefab == null)
            {
                itemPrefab = RadialMenuItem.CreatePrefab();
                DontDestroyOnLoad(itemPrefab); // Сохраняем префаб между сценами
            }

            center = new Vector2(Screen.width / 2, Screen.height / 2);
            GenerateItems();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (!Showed) ShowMenu();
            }
            else if (Showed)
            {
                HideMenu();
            }

            if (Showed)
            {
                UpdateSelection();

                if (Input.GetMouseButtonUp(1))
                {

                    
                    ExecuteSelectedAction();
                }
            }
        }

        void ShowMenu()
        {
            Showed = true;
            gameObject.active = true;
            //radialCanvas.enabled = true;
            //Cursor.lockState = CursorLockMode.None;
            //MitaCore.Instance.gameController.ShowCursor(true);
            //PlayerAnimationModded.playerMove.stopMouseMove = true;
            center = new Vector2(Screen.width / 2, Screen.height / 2);
        }

        void HideMenu()
        {
            Showed = false;
            gameObject.active = false;

            //radialCanvas.enabled = false;
            //Cursor.lockState = CursorLockMode.Locked;
            //MitaCore.Instance.gameController.ShowCursor(false);
            //PlayerAnimationModded.playerMove.stopMouseMove = false;

            // Сброс выделения
            //if (lastSelectedIndex != -1)
            //{
            //    menuItems[lastSelectedIndex].GetComponent<Image>().color = normalColor;
              //  lastSelectedIndex = -1;
            //}
        }

        void UpdateSelection()
        {
            Vector2 mousePos = Input.mousePosition;
            int closestIndex = -1;
            float minDistance = float.MaxValue;

            // Находим ближайший элемент
            for (int i = 0; i < menuItems.Count; i++)
            {
                float dist = Vector2.Distance(mousePos, menuItems[i].transform.position);
                if (dist < minDistance && dist < selectionThreshold)
                {
                    minDistance = dist;
                    closestIndex = i;
                }
            }

            // Обновляем выделение
            if (closestIndex != lastSelectedIndex)
            {
                // Снимаем выделение с предыдущего
                if (lastSelectedIndex != -1)
                {
                    menuItems[lastSelectedIndex].GetComponent<Image>().color = normalColor;
                }

                // Выделяем новый
                if (closestIndex != -1)
                {
                    menuItems[closestIndex].GetComponent<Image>().color = hoverColor;
                    selectedItem = items[closestIndex];
                }

                lastSelectedIndex = closestIndex;
            }
        }

        void ExecuteSelectedAction()
        {
            if (!string.IsNullOrEmpty(selectedItem))
            {
                MelonLogger.Msg($"!!! CHOSE: {selectedItem}");

                // Здесь добавьте обработку выбранного действия
                switch (selectedItem)
                {
                    case "Удар":
                        // Код для удара
                        break;
                    case "Обнять":
                        // Код для объятия
                        break;
                    case "Что":
                        // Код для вопроса
                        break;
                }
            }
        }

        void GenerateItems()
        {
            // Очистка старых элементов
            foreach (var item in menuItems)
            {
                Destroy(item);
            }
            menuItems.Clear();

            float angleStep = 360f / items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                // Создаем элемент меню
                GameObject item = Instantiate(itemPrefab, radialCanvas.transform);
                menuItems.Add(item);

                // Позиционирование по кругу
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
                item.GetComponent<RectTransform>().anchoredPosition = pos;

                // Настройка внешнего вида
                var menuItem = item.GetComponent<RadialMenuItem>();
                if (menuItem != null)
                {
                    menuItem.Setup(items[i], i < descriptions.Count ? descriptions[i] : "");
                }
            }
        }

        // Цвета для удобства
        private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        private Color hoverColor = new Color(0.3f, 0.5f, 0.8f, 1f);
    }

    [RegisterTypeInIl2Cpp]
    public class RadialMenuItem : MonoBehaviour
    {
        public static GameObject CreatePrefab()
        {
            // Создаем корневой объект
            var item = new GameObject("RadialMenuItem");
            var rectTransform = item.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100);

            // Добавляем компоненты
            var image = item.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Создаем дочерний объект для текста
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(item.transform);

            var text = textObj.AddComponent<Text>();
            text.text = "Item";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            // Настраиваем RectTransform для текста
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Добавляем компонент RadialMenuItem
            var menuItem = item.AddComponent<RadialMenuItem>();
            menuItem.label = text;
            menuItem.background = image;

            return item;
        }

        public Image background;
        public Text label;
        public GameObject descriptionPanel;
        public Text descriptionText;

        public void Setup(string itemName, string description)
        {
            if (label != null) label.text = itemName;
            if (descriptionText != null) descriptionText.text = description;
            if (descriptionPanel != null) descriptionPanel.SetActive(false);
        }
    }
}
