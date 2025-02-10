using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace MitaAI
{
    public static class Utils
    {
        public static void CopyComponentValues(ObjectInteractive source, ObjectInteractive destination)
        {
            if (source == null || destination == null)
            {
                MelonLogger.Msg("Source or destination is null!");
                return;
            }

            // Если нужно скопировать все поля автоматически, можно использовать рефлексию
            var fields = typeof(ObjectInteractive).GetFields();
            foreach (var field in fields)
            {
                field.SetValue(destination, field.GetValue(source));
            }

            MelonLogger.Msg("Component values copied!");
        }

        public static float getDistanceBetweenObjects(GameObject a, GameObject b)
        {
            return Vector3.Distance(a.transform.position, b.transform.position);
        }

        static public IEnumerator DestroyObjecteAfterTime(GameObject obj, float delay)
        {
            // Проверяем, не null ли объект
            if (obj == null)
            {
                MelonLogger.Msg("GameObject is null. Cannot toggle.");
                yield break;
            }

            // Ждём заданное время
            yield return new WaitForSeconds(delay);

            GameObject.Destroy(obj);
        }



        public static IEnumerator ToggleObjectActiveAfterTime(GameObject obj, float delay)
        {
            // Проверяем, не null ли объект
            if (obj == null)
            {
                MelonLogger.Msg("GameObject is null. Cannot toggle.");
                yield break;
            }

            // Ждём заданное время
            yield return new WaitForSeconds(delay);

            // Переключаем активность объекта
            obj.SetActive(!obj.activeSelf);
            MelonLogger.Msg($"GameObject {obj.name} is now {(obj.activeSelf ? "active" : "inactive")}");
        }



    }


}
