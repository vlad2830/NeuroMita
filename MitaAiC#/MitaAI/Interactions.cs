using Il2Cpp;
using Il2CppSystem;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text; // Для StringBuilder

namespace MitaAI
{
    public static class Interactions
    {
        public static void CreateObjectInteractable(GameObject gameObject)
        {
            if (gameObject == null)
            {
                MelonLogger.Msg("ERROR FIND: GameObject is null");
                return;
            }

            if (!gameObject.GetComponent<Collider>())
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                MelonLogger.Msg($"Collider added to {gameObject.name}");
            }
            if (gameObject.GetComponent<ObjectInteractive>())
            {
                var objectInteractive = gameObject.AddComponent<ObjectInteractive>();
            }
        }
        private static Dictionary<string, float> objectViewTime = new Dictionary<string, float>();

        public static void Update()
        {
            try
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    GameObject hitObject = hit.collider.gameObject;
                    string objectName = hitObject.name; // Используем имя как ключ

                
                        if (!objectViewTime.ContainsKey(objectName))
                        {
                            objectViewTime[objectName] = 0.0f;
                            //MelonLogger.Msg($"Adding new object: {objectName}");
                        }
                        else
                        {
                           // MelonLogger.Msg($"Object already tracked: {objectName}");
                        }
                        objectViewTime[objectName] += Time.deltaTime;
            


                    if (Input.GetMouseButtonDown(0))
                    {
                        MelonLogger.Msg("Mouse clicked.");
                        OnGameObjectClicked(hitObject);
                    }

                   //MelonLogger.Msg($"{objectName}:{objectViewTime[objectName]}s.");
                   //MelonLogger.Msg($"objectViewTime count {objectViewTime.Count}.");
                }
            }
            catch (System.Exception ex)
            {

                MelonLogger.Error($"Interactions Update error: {ex}");
            }
        }

        public static string getObservedObjects()
        {
            StringBuilder answer = new StringBuilder("\nPlayer has observed since last answer (object:view time seconds):");
            //List<string> toRemove = new List<string>();
            try
            {
                foreach (var item in objectViewTime)
                {
                    if (item.Value >= 0.5f)
                    {
                        answer.Append($"{item.Key}:{item.Value.ToString("F2")}s\n");
                        //toRemove.Add(item.Key);
                    }
                }
                objectViewTime.Clear();
            }
            catch (System.Exception ex)
            {

                MelonLogger.Error($"getObservedObjects error: {ex}");
            }


            // Удаляем только те объекты, которые уже обработаны
            // foreach (var obj in toRemove)
            //{
                //   objectViewTime.Remove(obj);
                // }

                return answer.ToString();
            
        }
        public static void OnGameObjectClicked(GameObject gameObject)
        {

            //MitaCore.Instance.sendSystemInfo($"Игрок стукнул по {gameObject.name}.");
            MelonLogger.Msg($"The GameObject {gameObject.name} was clicked!");

            if (gameObject.GetComponent<ObjectInteractive>())
            {
                gameObject.GetComponent<ObjectInteractive>().active = false;
                ToggleBoolAfterTime(gameObject, 3, true);
            }
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


