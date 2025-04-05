using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class RadialMenu : MonoBehaviour
    {
        public Canvas radialCanvas;
        public GameObject itemPrefab = RadialMenuItem.CreatePrefab();
        public float radius = 250f;
        public List<string> items = new List<string> { "Удар", "Обнять", "Что" };

        private Vector2 center;
        private bool isActive;
        private string selectedItem;

        void Start()
        {
            center = new Vector2(Screen.width / 2, Screen.height / 2);
            radialCanvas.enabled = false;
            GenerateItems();
        }

        bool block = false;
        void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                isActive = true;
                radialCanvas.enabled = true;
                Cursor.lockState = CursorLockMode.None;


                MitaCore.Instance.gameController.ShowCursor(true);
                PlayerAnimationModded.playerMove.stopMouseMove = true;
                block = true;
            }
            else if (block)
            {
                block = false;
                MitaCore.Instance.gameController.ShowCursor(false);
                PlayerAnimationModded.playerMove.stopMouseMove = false;
            }

            if (isActive)
            {
                Vector2 mousePos = Input.mousePosition;
                Vector2 dir = mousePos - center;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360;

                int selectedIndex = Mathf.FloorToInt(angle / (360f / items.Count));
                selectedItem = items[selectedIndex];
            }

            if (Input.GetMouseButtonUp(1))
            {
                isActive = false;
                radialCanvas.enabled = false;
                Cursor.lockState = CursorLockMode.Locked;

                if (!string.IsNullOrEmpty(selectedItem))
                {
                    // Вызов вашей функции с выбранным элементом
                    Debug.Log($"Selected: {selectedItem}");
                }
            }
        }

        void GenerateItems()
        {
            float angleStep = 360f / items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                GameObject item = Instantiate(itemPrefab, radialCanvas.transform);
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
                item.GetComponent<RectTransform>().anchoredPosition = pos;
                item.GetComponentInChildren<Text>().text = items[i];
            }
        }

        // Для динамического обновления списка
        public void UpdateItems(List<string> newItems)
        {
            items = newItems;
            foreach (Transform child in radialCanvas.transform)
                Destroy(child.gameObject);
            GenerateItems();
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
    }
}
