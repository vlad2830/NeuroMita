/*using Il2Cpp;
using MelonLoader;
using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic; // Добавлено для использования HashSet
using UnityEngine.Animations;

namespace MitaAI.Mita
{
    public static class MitaAnimationModded
    {// Получаем Animator Controller
        
        
        static private Queue<AnimationClip> animationQueue = new Queue<AnimationClip>();
        static private bool isPlaying = false;
        static private Il2CppAssetBundle bundle;
        static Animator_FunctionsOverride mitaAnimatorFunctions;
        static Location34_Communication location34_Communication;
        static AnimationClip idleAnimation;
        static Animator mitaAnimator; // Добавлен доступ к Animator
        static AnimatorOverrideController controller;
       // static AnimatorOverrideController controllerOverride;
        // Для отслеживания завершения анимации
        static private HashSet<string> persistentAnimations = new HashSet<string>()
        {
            "Mita Tired", "Mita HugIdle" // Анимации, которые не должны автоматически прерываться
        };

        static public void init(Animator _mitaAnimator, Animator_FunctionsOverride _mitaAnimatorFunctions, Location34_Communication _location34_Communication)
        {
            mitaAnimatorFunctions = _mitaAnimatorFunctions;
            location34_Communication = _location34_Communication;
            mitaAnimator = _mitaAnimator;
            // Получаем Animator из объекта
            try
            {
                controller = mitaAnimator.runtimeAnimatorController as AnimatorOverrideController;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("controller casting error!"+ex);
            }
            if (controller == null)
            {
                try
                {
                    controller = mitaAnimator.runtimeAnimatorController.TryCast<AnimatorOverrideController>();
                }
                catch (Exception ex )
                {

                    MelonLogger.Error("controller casting 2 error!" + ex);
                }
                

            }

            bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");
        }

        public static string setAnimation(string response)
        {
            MelonLogger.Msg("setAnimation start");

            // Регулярное выражение для извлечения эмоций
            string pattern = @"<a>(.*?)</a>";
            Match match = Regex.Match(response, pattern);

            string animationName = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<a>.*?</a>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                animationName = match.Groups[1].Value;
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
                switch (animationName)
                {
                    case "Щелчек":
                        int randomIndex = UnityEngine.Random.Range(0, 4); // Генерация числа от 0 до 3
                        string animationNameForRandom;

                        if (randomIndex == 0)
                            animationNameForRandom = "Mita Click_0";
                        else if (randomIndex == 1)
                            animationNameForRandom = "Mita Click_1";
                        else if (randomIndex == 2)
                            animationNameForRandom = "Mita Click_2";
                        else
                            animationNameForRandom = "Mita Click"; // Четвёртый кейс

                        EnqueueAnimation(animationNameForRandom);
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
                    case "Глянуть глазами в случаном направлении":
                        MitaCore.Instance.MitaLook.EyesLookOffsetRandom(90);
                        break;
                    case "Повернуться в случаном направлении":
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
                        // EnqueueAnimation("Mita Idle");
                        EnqueueAnimation("Mita Heh");
                        break;
                       
                    case "Случайная анимация":
                        EnqueueAnimation("");
                        break;
                    default:
                        MelonLogger.Error($"Unknown animation: {animationName}");
                        if (animationName != "")
                        {
                            MelonLogger.Error($"use random animation:");
                            EnqueueAnimation(animationName);
                        }
                        
                        break;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Problem with Animation: {ex.Message}");
            }

            MelonLogger.Msg("setAnimation End");
            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }

        static public void EnqueueAnimation(string animName = "")
        {
            try
            {

                if (bundle == null) bundle = AssetBundleLoader.LoadAssetBundle("assetbundle");

                AnimationClip anim = !string.IsNullOrEmpty(animName) ?
                    AssetBundleLoader.LoadAnimationClipByName(bundle, animName) :
                    AssetBundleLoader.LoadRandomAnimationClip(bundle);

                if (anim == null) return;

                // 9. Очистка событий анимации для безопасности
                anim.events = Array.Empty<AnimationEvent>();

                // 10. Приоритетное добавление важных анимаций
                if (anim.name.Contains("Emergency"))
                    animationQueue = new Queue<AnimationClip>(new[] { anim });
                else
                    animationQueue.Enqueue(anim);

                if (!isPlaying)
                    MelonCoroutines.Start(ProcessQueue());
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Animation error: {e}");
                ClearQueue(); // 11. Очистка очереди при ошибках
            }
        }

        static private IEnumerator ProcessQueue()
        {
            try
            {
                isPlaying = true;
                location34_Communication.enabled = false;

                while (animationQueue.Count > 0)
                {
                    AnimationClip currentAnim = animationQueue.Peek(); // Без Dequeue пока не убедимся

                    if (currentAnim == null)
                    {
                        animationQueue.Dequeue();
                        continue;
                    }

                    // 4. Проверка на персистентные анимации
                    bool isPersistent = persistentAnimations.Contains(currentAnim.name);
                    
                    yield return MelonCoroutines.Start(PlayAnimationSmoothly(currentAnim));

                    // 5. Удаляем из очереди только после успешного воспроизведения
                    animationQueue.Dequeue();

                    // 6. Специальная обработка для переходных анимаций
                    if (currentAnim.name == "Mita Start Tired")
                    {
                        yield return new WaitForSeconds(0.5f); // Пауза перед следующей анимацией
                    }
                }
            }
            finally // 7. Гарантированное восстановление состояния
            {
                // 8. Возврат к idle-анимации с плавным переходом
                if (idleAnimation != null)
                    mitaAnimator.CrossFade(idleAnimation.name, 0.2f);

                location34_Communication.enabled = true;
                isPlaying = false;
            }
        }
        // Добавлен метод для плавного перехода
        static private IEnumerator PlayAnimationSmoothly(AnimationClip clip)
        {
            // Проверка на null
            if (clip == null)
            {
                MelonLogger.Error("AnimationClip is null!");
                yield break;
            }

            MelonLogger.Msg("111");

           
            // Проверка и инициализация mitaAnimator
            if (mitaAnimator == null)
            {
                MelonLogger.Error("Animator is not initialized!");
                mitaAnimator = MitaCore.Instance.MitaPersonObject.GetComponent<Animator>();
            }
            MelonLogger.Msg("222");
            // Проверка и инициализация controller
            if (controller == null)
            {
                MelonLogger.Error("controller is not initialized!");
                try
                {
                    if (mitaAnimator.runtimeAnimatorController is AnimatorOverrideController)
                    {
                        MelonLogger.Msg("aaa");
                        controller = mitaAnimator.runtimeAnimatorController as AnimatorOverrideController;
                    }
                    else
                    {
                        MelonLogger.Msg("bbb");
                        // Создаем новый AnimatorOverrideController на основе текущего контроллера
                        controller = new AnimatorOverrideController(mitaAnimator.runtimeAnimatorController);
                        mitaAnimator.runtimeAnimatorController = controller;
                    }
                }
                catch (Exception e) { MelonLogger.Error("Tried to set controlles!"+e);  }
            }
            MelonLogger.Msg("333");
            try
            {
                // Перезаписываем анимацию в контроллере
                controller.Internal_SetClipByName("Mita Idle", clip);
                MelonLogger.Msg($"Animation 'Mita Idle' added/overwritten in controller.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to set animation clip: {ex}");
                yield break;
            }

            MelonLogger.Msg("222");

            // Плавный переход к анимации
            float transitionDuration = 0.15f;
            try
            {
                mitaAnimator.CrossFade("Mita Idle", transitionDuration);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to play animation: {ex}");
                yield break;
            }
            MelonLogger.Msg("333 CrossFade after");
            // 2. Точное ожидание завершения анимации
            float animationLength = clip.length;
            float startTime = Time.time;

            while (Time.time - startTime < animationLength + transitionDuration)
            {
                // 3. Проверка текущего состояния аниматора
                AnimatorStateInfo stateInfo = mitaAnimator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName(clip.name) && stateInfo.normalizedTime >= 0.95f)
                    break;

                yield return null;
            }
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
            }


        }

        // 12. Улучшенный метод очистки
        static public void ClearQueue()
        {
            animationQueue.Clear();
            mitaAnimator.StopPlayback();

            if (idleAnimation != null)
                mitaAnimator.Play(idleAnimation.name, 0, 0);
        }
    }

}*/