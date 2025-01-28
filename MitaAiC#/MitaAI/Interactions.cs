using Il2Cpp;
using Il2CppSystem;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace MitaAI
{
    public static class Interactions
    {
        public static void CreateObjectInteractable(UnityEngine.GameObject gameObject, System.Action unityAction)
        {
            if (gameObject == null)
            {
                MelonLogger.Msg($"ERROR FIND: GameObject is null");
                return;
            }

            // Проверяем, есть ли коллайдер у объекта, если нет — добавляем
            if (!gameObject.GetComponent<UnityEngine.Collider>())
            {
                UnityEngine.Bounds bounds = new UnityEngine.Bounds(gameObject.transform.position, UnityEngine.Vector3.zero);
                UnityEngine.MeshFilter meshFilter = gameObject.GetComponent<UnityEngine.MeshFilter>();

                if (meshFilter != null && meshFilter.mesh != null)
                {
                    bounds.Encapsulate(meshFilter.mesh.bounds);
                }

                UnityEngine.BoxCollider boxCollider = gameObject.AddComponent<UnityEngine.BoxCollider>();
                boxCollider.center = bounds.center;
                boxCollider.size = bounds.size;

                MelonLogger.Msg($"SIZE: {boxCollider.size} and center: {boxCollider.center}");
            }
            

            // Делаем объект неактивным перед добавлением компонентов
            gameObject.SetActive(false);

            // Добавляем компонент взаимодействия
            var objectInteractive = gameObject.AddComponent<ObjectInteractive>();

            // Создаём событие и добавляем обработчик через Il2CppSystem.Action
            objectInteractive.eventClick = new UnityEngine.Events.UnityEvent();
            objectInteractive.eventClick.AddListener(unityAction);

            // Делаем объект активным
            gameObject.SetActive(true);
        }


        public static void Test(UnityEngine.GameObject gameObject2)
        {
            MelonLogger.Msg($"Attempting interaction setup...");
            try
            {
                // Создаем обработчик и преобразуем в Il2CppSystem.Action
                System.Action action = () => OnGameObjectClicked(gameObject2);


                // Передаём в метод CreateObjectInteractable
                CreateObjectInteractable(gameObject2, action);
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"Error during interaction setup: {e}");
            }
            MelonLogger.Msg($"Interaction setup completed.");
        }


        private static Il2CppSystem.Action CreateIl2CppAction(System.Action action)
        {
            return (Il2CppSystem.Action)action;
        }

        // Метод для обработки события
        private static void OnGameObjectClicked(UnityEngine.GameObject gameObject)
        {
            switch (gameObject.name)
            {
                case "SofaChair":
                    MelonLogger.Msg($"{gameObject.name}");
                    MitaCore.Instance.sendSystemMessage("Игрок засмотрелся на кресло");
                    break;
                case "CornerSofa":
                    MelonLogger.Msg($"{gameObject.name}");
                    MitaCore.Instance.sendSystemMessage("Игрок засмотрелся на кресло в ");
                    break;
                case "Kitchen Table":
                    MelonLogger.Msg($"{gameObject.name}");
                    MitaCore.Instance.sendSystemMessage("Игрок засмотрелся на стол на кухне");
                    break;
                default:
                    break;
            }
            MelonLogger.Msg($"The GameObject {gameObject.name} did something!");
            ToggleBoolAfterTime(gameObject, 1, true);
        }

        public static IEnumerator ToggleBoolAfterTime(GameObject gameObject, float delay, bool value)
        {
            // Ждём заданное время
            yield return new WaitForSeconds(delay);

            // Проверяем, есть ли компонент ObjectInteractive
            var objectInteractive = gameObject.GetComponent<ObjectInteractive>();
            if (objectInteractive != null)
            {
                objectInteractive.active = value;
                Debug.Log($"Object {gameObject.name} active set to {value}");
            }
            else
            {
                Debug.LogError($"ObjectInteractive component not found on {gameObject.name}");
            }
        }
    }

}
