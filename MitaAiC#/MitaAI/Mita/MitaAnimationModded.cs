using Il2Cpp;

using MelonLoader;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
namespace MitaAI.Mita
{
    public static class MitaAnimationModded
    {
        static private Queue<AnimationClip> animationQueue = new Queue<AnimationClip>();
        static private bool isPlaying = false;
        static private Il2CppAssetBundle bundle;
        static Animator_FunctionsOverride mitaAnimatorFunctions;
        static Location34_Communication location34_Communication;
        static AnimationClip idleAnimation;
        static AnimationClip idleWalkAnimation;
        // Основной метод для добавления анимации в очередь

        static public void init(Animator_FunctionsOverride _mitaAnimatorFunctions, Location34_Communication _location34_Communication)
        {
            // Получаем компонент Animator_FunctionsOverride из текущего объекта
            mitaAnimatorFunctions = _mitaAnimatorFunctions;
            location34_Communication = _location34_Communication;

            bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            if (mitaAnimatorFunctions == null)
            {
                Debug.LogError("Animator_FunctionsOverride component not found on this object!");
            }
            idleAnimation = location34_Communication.mitaAnimationIdle;
            idleWalkAnimation = location34_Communication.mitaAnimationWalk;
        }

        public static string setAnimation(string response)
        {
            // Регулярное выражение для извлечения эмоций
            string pattern = @"<a>(.*?)</a>";
            Match match = Regex.Match(response, pattern);

            string MovementStyle = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<a>.*?</a>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                MovementStyle = match.Groups[1].Value;
            }
            try
            {
                // Проверка на наличие объекта Mita перед применением эмоции
                if (MitaCore.Instance.Mita == null || MitaCore.Instance.Mita.gameObject == null)
                {
                    MelonLogger.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }
                // Устанавливаем лицо, если оно найдено
                switch (MovementStyle)
                {
                    case "Щелчек":
                        int randomIndex = UnityEngine.Random.Range(0, 4); // Генерация числа от 0 до 3
                        string animationName;

                        if (randomIndex == 0)
                            animationName = "Mita Click_0";
                        else if (randomIndex == 1)
                            animationName = "Mita Click_1";
                        else if (randomIndex == 2)
                            animationName = "Mita Click_2";
                        else
                            animationName = "Mita Click"; // Четвёртый кейс

                        EnqueueAnimation(animationName);
                        break;
                    case "Похлопать в ладоши":
                        EnqueueAnimation("Mita Cheerful");
                        break;
                    case "Указать направление":
                        EnqueueAnimation("Mita ShowTumb");
                        break;
                    case "Смотреть с презрением":
                        EnqueueAnimation("Mita IdleBat");
                        break;
                    case "Показать усталость":
                        EnqueueAnimation("Mita Start Tired");
                        EnqueueAnimation("MiMita Tired");
                        break;
                    case "Притвориться отключенной и упасть":
                        EnqueueAnimation("MitaBody Fall");
                        break;
                    case "Взять предмет":
                        EnqueueAnimation("Mita TakeBat");
                        break;
                    case "Кивнуть да":
                        MitaCore.Instance.MitaLook.Nod(true);
                        break;
                    case "Кивнуть нет":
                        MitaCore.Instance.MitaLook.Nod(false);
                        break;
                    case "Глянуть глазами в случайном направлении":
                        MitaCore.Instance.MitaLook.EyesLookOffsetRandom(90);
                        break;
                    case "Повернуться в случайном направлении":
                        MitaCore.Instance.MitaLook.LookRandom();
                        break;
                    case "Развести руки":
                        EnqueueAnimation("Mita StartShow Knifes");
                        EnqueueAnimation("Mita Throw Knifes");
                        
                        break;
                    case "Поднести палец к подбородку":
                        EnqueueAnimation("Mita TalkWithPlayer");
                        break;
                    case "Поднять игрока одной рукой":
                        EnqueueAnimation("Mita TakeMita");
                        EnqueueAnimation("Mita TakeMita Idle");
                        EnqueueAnimation("Mita ThrowPlayer");
                        break;
                    case "Сложить руки перед собой":
                        EnqueueAnimation("Mita Hands Down Idle");
                        break;
                    case "Показать предмет":
                        EnqueueAnimation("Mita Selfi");
                        break;
                    case "Прикрыть глаза":
                        EnqueueAnimation("Mita Close Eyes");
                        //int randomIndex = UnityEngine.Random.Range(0, 4); // Генерация числа от 0 до 3
                        //EnqueueAnimation("Mita Open Eyes");
                        //EnqueueAnimation("Mita Open Shar Eyes");
                        break;
                    case "Обнять":
                        EnqueueAnimation("Mita StartHug");
                        EnqueueAnimation("Mita HugIdle");
                        EnqueueAnimation("Mita StopHug");
                        break;
                    case "Удар":
                        EnqueueAnimation("Mita Kick");
                        break;
                    case "Похвастаться предметом":
                        EnqueueAnimation("Mita Take Recorder");
                        break;
                    case "Прикрыть рот и помахать рукой":
                        EnqueueAnimation("Mita Oi");
                        //EnqueueAnimation("Mita Idle");
                        EnqueueAnimation("Mita Heh");
                        break;
                       
                    case "Случайная анимация":
                        EnqueueAnimation("");
                        break;
                    default:
                        if (MovementStyle != "")
                        {
                            EnqueueAnimation(MovementStyle);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Problem with Animation: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }


        static public void EnqueueAnimation(string animName = "")
        {
            if (bundle == null)
            {
                bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            }

            AnimationClip anim = null;
            try
            {
                if (!string.IsNullOrEmpty(animName))
                {
                    anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
                }
                else
                {
                    anim = AssetBundleLoader.LoadRandomAnimationClip(bundle);
                }

                if (anim != null)
                {
                    anim.events = Array.Empty<AnimationEvent>();
                    animationQueue.Enqueue(anim);
                    MelonLogger.Msg($"Added to queue: {anim.name}");

                    if (!isPlaying)
                    {
                        MelonCoroutines.Start(ProcessQueue());
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("Animation error: " + e);
            }
        }

        // Корутина для последовательного проигрывания
        static private IEnumerator ProcessQueue()
        {
            isPlaying = true;
            location34_Communication.enabled = false;
            while (animationQueue.Count > 0)
            {
                AnimationClip currentAnim = animationQueue.Dequeue();

                if (currentAnim != null)
                {
                    MelonLogger.Msg($"Now playing: {currentAnim.name} ({currentAnim.length}s)");
                    mitaAnimatorFunctions.AnimationClipSimpleNext(currentAnim);

                    // Ждем завершения анимации
                    yield return new WaitForSeconds(currentAnim.length);
                }
            }
            mitaAnimatorFunctions.AnimationClipSimpleNext(idleAnimation);
            location34_Communication.enabled = true;
            isPlaying = false;
        }
        static public void setIdleAnimation(string animName)
        {
            if (bundle == null)
            {
                bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            }

            AnimationClip anim = null;
            if (!string.IsNullOrEmpty(animName))
            {
                anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
                location34_Communication.mitaAnimationIdle = anim;
            }

        }
        static public void setIdleWalk(string animName)
        {
            if (bundle == null)
            {
                bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
            }

            AnimationClip anim = null;
            if (!string.IsNullOrEmpty(animName))
            {
                anim = AssetBundleLoader.LoadAnimationClipByName(bundle, animName);
                location34_Communication.mitaAnimationWalk = anim;
                mitaAnimatorFunctions.AnimationClipWalk(anim);
            }


        }

        // Очистка очереди (опционально)
        static public void ClearQueue()
        {
            animationQueue.Clear();
            isPlaying = false;
        }
    }

}