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

        /// <summary>
        /// Возвращает true с вероятностью a/b
        /// </summary>
        /// <param name="a">Числитель (шанс успеха)</param>
        /// <param name="b">Знаменатель (общий диапазон)</param>
        /// <returns>True, если шанс произошел, иначе False</returns>
        public static bool Random(int a, int b)
        {
            // Проверка на корректность входных данных
            if (a < 0 || b <= 0 || a > b)
            {
                Debug.LogError("Некорректные значения a и b. Должно быть: 0 <= a <= b, b > 0");
                return false;
            }

            // Генерация случайного числа в диапазоне [0, b)
            int randomValue = UnityEngine.Random.Range(0, b);

            // Возвращаем true, если randomValue < a
            return randomValue < a;
        }

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


        public static GameObject TryfindChild(Transform parent, string path)
        {
            try
            {
                return parent.Find(path).gameObject;
            }
            catch (Exception e)
            {

                MelonLogger.Msg($"Tried found {path} but {e}");
                return null;
            }
        }
        public static void TryTurnChild(Transform parent, string path, bool on)
        {
            try
            {
                TryfindChild(parent, path).gameObject.SetActive(on);
            }
            catch (Exception e)
            {

                MelonLogger.Msg("Tried turn " + path + " " + e);
                return;
            }
        }
    }


}
