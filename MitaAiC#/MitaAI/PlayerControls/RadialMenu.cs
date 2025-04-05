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
        public GameObject itemPrefab = RadialMenuItem.CreatePrefab();
        public float radius = 250f;
        public System.Collections.Generic.List<string> items = new System.Collections.Generic.List<string> { "Удар", "Обнять", "Что" };
        public System.Collections.Generic.List<string> descriptions = new System.Collections.Generic.List<string> { "Нанести удар", "Обнять персонажа", "Спросить что-то" };

        private Vector2 center;
        private bool isActive;
        private string selectedItem;
        private System.Collections.Generic.List<RectTransform> itemTransforms = new System.Collections.Generic.List<RectTransform>();

        void Start()
        {
            center = new Vector2(Screen.width / 2, Screen.height / 2);
            radialCanvas.enabled = false;
            GenerateItems();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                isActive = true;
                radialCanvas.enabled = true;
                Cursor.lockState = CursorLockMode.None;
                MitaCore.Instance.gameController.ShowCursor(true);
                PlayerAnimationModded.playerMove.stopMouseMove = true;
            }

            if (isActive)
            {
                // Простая проверка ближайшего элемента
                selectedItem = GetNearestItem(Input.mousePosition);

                // Подсветка выбранного элемента
                for (int i = 0; i < items.Count; i++)
                {
                    bool isSelected = (items[i] == selectedItem);
                    itemTransforms[i].GetComponent<Image>().color = isSelected ?
                        new Color(0.3f, 0.5f, 0.8f, 1f) :
                        new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                isActive = false;
                radialCanvas.enabled = false;
                Cursor.lockState = CursorLockMode.Locked;
                MitaCore.Instance.gameController.ShowCursor(false);
                PlayerAnimationModded.playerMove.stopMouseMove = false;

                if (!string.IsNullOrEmpty(selectedItem))
                {
                    Debug.Log($"Selected: {selectedItem}");
                    // Здесь вызывайте нужную функцию в зависимости от выбранного элемента
                }
            }
        }

        void GenerateItems()
        {
            itemTransforms.Clear();

            // Удаляем старые элементы если есть
            foreach (Transform child in radialCanvas.transform)
                Destroy(child.gameObject);

            float angleStep = 360f / items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                GameObject item = Instantiate(itemPrefab, radialCanvas.transform);
                itemTransforms.Add(item.GetComponent<RectTransform>());

                // Настройка позиции
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
                item.GetComponent<RectTransform>().anchoredPosition = pos;

                // Настройка текста
                var menuItem = item.GetComponent<RadialMenuItem>();
                if (menuItem != null)
                {
                    menuItem.Setup(items[i], descriptions.Count > i ? descriptions[i] : "");
                }
            }
        }

        string GetNearestItem(Vector2 mousePos)
        {
            string nearestItem = "";
            float minDistance = float.MaxValue;

            for (int i = 0; i < itemTransforms.Count; i++)
            {
                float dist = Vector2.Distance(mousePos, itemTransforms[i].position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestItem = items[i];
                }
            }

            return nearestItem;
        }
    

 
    }

    [RegisterTypeInIl2Cpp]


    public class RadialMenuItem : MonoBehaviour
    {

        public static GameObject CreatePrefab()
        {
            // Создаём основной объект
            GameObject item = new GameObject("RadialMenuItem");

            // Добавляем компонент Image (фон)
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Темно-серый с прозрачностью

            // Создаём дочерний объект для текста
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(item.transform);

            // Настраиваем Text
            Text text = textObj.AddComponent<Text>();
            text.text = "Item";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            // Центрируем текст
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return item;
          }
        

        // [Header("References")]
        public Image background;
        public Text label;
        public GameObject descriptionPanel;
        public Text descriptionText;

        //[Header("Colors")]
        public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        public Color hoverColor = new Color(0.3f, 0.5f, 0.8f, 1f);

        private bool isHovered;

        private void Update()
        {
            // Ручная проверка hover через Raycast (на случай проблем с EventSystem)
            if (IsMouseOver())
            {
                if (!isHovered) OnHoverEnter();
            }
            else
            {
                if (isHovered) OnHoverExit();
            }
        }

        //private bool IsMouseOver()
        //{
        //    if (EventSystem.current == null) return false;

        //    PointerEventData pointerData = new PointerEventData(EventSystem.current)
        //    {
        //        position = Input.mousePosition
        //    };

        //    // Используем Il2CppList вместо System.Collections.Generic.List
        //    var results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
        //    EventSystem.current.RaycastAll(pointerData, results);

        //    foreach (var result in results)
        //    {
        //        if (result.gameObject == gameObject) return true;
        //    }
        //    return false;
        //}

        private void OnHoverEnter()
        {
            isHovered = true;
            background.color = hoverColor;
            if (descriptionPanel != null)
                descriptionPanel.SetActive(true);
        }

        private void OnHoverExit()
        {
            isHovered = false;
            background.color = normalColor;
            if (descriptionPanel != null)
                descriptionPanel.SetActive(false);
        }

        //public void Setup(string itemName, string description)
        //{
        //    label.text = itemName;
        //    descriptionText.text = description;
        //}
    }
}
